using ChatContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Net;

namespace ChatServer
{
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single,
       ConcurrencyMode = ConcurrencyMode.Multiple,
       UseSynchronizationContext = false)]
    public class ChatService : IDuplexChatService
    {
        private static readonly HashSet<string> signedInUsers =
            new HashSet<string>();

        private static readonly HashSet<PrivateChannel> privateChannels =
            new HashSet<PrivateChannel>();

        private static Dictionary<string, IChatServiceCallback> userCallbacks =
            new Dictionary<string, IChatServiceCallback>();



        private static readonly string[] allowedExtensions = {".png", ".jpg", ".jpeg", ".gif", ".bmp", ".txt" };
        private static Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();
        private static Dictionary<string, List<FileMetaInfo>> channelFiles = new Dictionary<string, List<FileMetaInfo>>();

        private static Dictionary<string, List<string>> privateNotifications = new Dictionary<string, List<string>>();

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool SignIn(string userId, out string reason)
        {
            reason = "";

            if (string.IsNullOrWhiteSpace(userId))
            {
                reason = "User ID cannot be empty.";
                return false;
            }

            userId = userId.Trim();


            if (signedInUsers.Contains(userId))
            {
                reason = "User ID '" + userId + "' is already signed in.";
                return false;
            }

            signedInUsers.Add(userId);

            return true;
        }


        [MethodImpl(MethodImplOptions.Synchronized)]
        public void RegisterCallback(string userId)
        {
            IChatServiceCallback callback =
                OperationContext.Current.GetCallbackChannel<IChatServiceCallback>();

            userCallbacks[userId] = callback;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool SignOut(string userId, out string reason)
        {
            reason = "";


            if (!signedInUsers.Contains(userId))
            {
                 reason = "User is not currently signed in.";
                 return false;
            }

            ClientDisconnected(userId);


            return true;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool CreateChannel(string channelName)
        {

                for (int i = 0; i < channels.Count; i++)
                {
                    if (channels[i].Name == channelName)
                    {
                        return false;
                    }
                }

                Channel newChannel = new Channel();
                newChannel.Name = channelName;
                channels.Add(newChannel);

                var callbacks = userCallbacks.ToList();

                List<string> disconnectedUsers = new List<string>();

                foreach (var pair in callbacks)
                {
                    try
                    {
                        pair.Value.ChannelListChange(channelName);
                    }
                    catch (Exception)
                    {
                        disconnectedUsers.Add(pair.Key);
                    }
                }
            
            foreach (string user in disconnectedUsers)
            {
                ClientDisconnected(user);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void JoinChannel(string userId, string channelName)
        {
            Channel targetChannel = null;

            for (int i = 0; i < channels.Count; i++)
            {
                if (channels[i].Name == channelName)
                {
                    targetChannel = channels[i];
                    break;
                }
            }

            if (targetChannel == null)
            {
                return;
            }


            for (int i = 0; i < channels.Count; i++)
            {
                for (int j = 0; j < channels[i].Users.Count; j++)
                {
                    if (channels[i].Users[j] == userId)
                    {
                            channels[i].Users.RemoveAt(j);
                            channels[i].JoinIndexes.RemoveAt(j);

                            break;
                    }
                }
            }

            if (!targetChannel.Users.Contains(userId))
            {
                targetChannel.Users.Add(userId);
                targetChannel.JoinIndexes.Add(targetChannel.Messages.Count);
            }
            

            var updatedUsers = GetUsers(channelName, userId);
            var snapshot = updatedUsers.ToList();
            List<string> disconnectedUsers = new List<string>();

            foreach (var user in snapshot)
            {

                if (userCallbacks.TryGetValue(user, out var callback))
                {
                    try
                    {
                        callback.MemberlistChange(channelName, updatedUsers);
                    }
                    catch (Exception)
                    {
                        disconnectedUsers.Add(user);
                    }
                }
                
            }
            foreach (string user in disconnectedUsers)
            {
                ClientDisconnected(user);
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool LeaveChannel(string userId, string channelName)
        {
            bool removed = false;

            Console.WriteLine($"LeaveChannel called: userId='{userId}', channelName='{channelName}'");
            for (int i = 0; i < channels.Count; i++)
            {
                    Console.WriteLine($"Checking channel '{channels[i].Name}', contains user: {channels[i].Users.Contains(userId)}");
                    if (channels[i].Name == channelName)
                    {
                        int userIndex = channels[i].Users.IndexOf(userId);
                    if (userIndex != -1)
                    {
                         channels[i].Users.RemoveAt(userIndex);
                         channels[i].JoinIndexes.RemoveAt(userIndex);
                         removed = true;
                    }

                    break;
                 }
            }
            
            
            if(removed)
            {
                List<string> updatedMembers = GetUsers(channelName, userId);
                List<string> disconnectedUsers = new List<string>();


                foreach (string member in updatedMembers)
                {
                    if (userCallbacks.TryGetValue(member, out var callback))
                    {
                        try
                        {
                            callback.MemberlistChange(channelName, updatedMembers);
                        }
                        catch (Exception)
                        {
                            disconnectedUsers.Add(member);
                        }
                    }
                }
                foreach (string user in disconnectedUsers)
                {
                    ClientDisconnected(user);
                }

            }

            return removed;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SendMessage(string channelName, string userId, string message)
        {
            Channel target = null;


            for (int i = 0; i < channels.Count; i++)
            {
                if (channels[i].Name == channelName)
                {
                    target = channels[i];
                    string chatMessage = userId + ": " + message;
                    channels[i].Messages.Add(chatMessage);
                    break;
                }
            }
            

            if (target != null)
            {

                var snapshot = target.Users.ToList();

                List<string> disconnectedUsers = new List<string>();

                foreach (var user in snapshot)
                {
                    if (userCallbacks.TryGetValue(user, out var callback))
                    {
                        try
                        {
                            callback.ReceiveMessage(channelName, userId, message);
                        }
                        catch (Exception)
                        {
                            disconnectedUsers.Add(user);
                        }
                    }

                }

                foreach (string user in disconnectedUsers)
                {
                    ClientDisconnected(user);
                }

            }

            return;
            

        }

        private static readonly List<Channel> channels = new List<Channel>
        {
                new Channel { Name = "General" },
                new Channel { Name = "Room1" },
                new Channel { Name = "Room2" }
        };

        [MethodImpl(MethodImplOptions.Synchronized)]
        public List<String> GetUsers(string channelName, string userId)
        {

            for (int i = 0; i < channels.Count; i++)
            {
                if (channels[i].Name == channelName)
                {
                    return channels[i].Users.ToList();
                }
            }

            return new List<string>();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public List<string> GetMessages(string channelName, string userId)
        {
            for (int i = 0; i < channels.Count; i++)
            {
                if (channels[i].Name == channelName)
                {
                    for (int j = 0; j < channels[i].Users.Count; j++)
                    {
                        if (channels[i].Users[j] == userId)
                        {
                            int startIndex = channels[i].JoinIndexes[j];

                            List<string> messages = new List<string>();

                            for (int k = startIndex; k < channels[i].Messages.Count; k++)
                            {
                                messages.Add(
                                    channels[i].Messages[k]);
                            }

                            return messages;
                        }
                    }
                }
            }

            return new List<string>();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public List<Channel> GetChannels()
        {

            return channels.ToList();
            
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SendPrivateMessage(string fromUserId, string toUserId, string message)
        {
            PrivateChannel targetChannel = null;

            foreach (var privateChannel in privateChannels)
            {
                if ((privateChannel.Sender == fromUserId &&
                     privateChannel.Recipient == toUserId) ||
                    (privateChannel.Sender == toUserId &&
                     privateChannel.Recipient == fromUserId))
                {
                    targetChannel = privateChannel;
                    break;
                }
            }

            if (targetChannel == null)
            {
                targetChannel = new PrivateChannel
                {
                    Sender = fromUserId,
                    Recipient = toUserId,
                    Messages = new List<string>()
                };

                privateChannels.Add(targetChannel);
            }

            string chatMessage = fromUserId + ": " + message;
            targetChannel.Messages.Add(chatMessage);

            if (userCallbacks.TryGetValue(toUserId, out var callback))
            {
                try
                {
                    callback.ReceivePrivateMessage(fromUserId, toUserId, message);
                }
                catch (Exception)
                {
                    ClientDisconnected(toUserId);
                }
            }

        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public List<string> GetPrivateMessages(string userId, string otherUserId)
        {

            foreach (var privateChannel in privateChannels)
            {
                if ((privateChannel.Sender == userId && privateChannel.Recipient == otherUserId) ||
                    (privateChannel.Sender == otherUserId && privateChannel.Recipient == userId))
                {
                    return privateChannel.Messages.ToList();
                }
            }
            
            return new List<string>();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void ShareFile(string channelName, string fromUserId, string fileName, byte[] fileData)
        {
            string extension = Path.GetExtension(fileName).ToLower();

            if(!allowedExtensions.Contains(extension))
            {
                throw new ExtensionException($"File extension '{extension}' is not allowed.");
            }
            
            if (fileData.Length > 2 * 1024 * 1024) // 2 MB limit
            {
                throw new FileSizeException("File size exceeds the maximum allowed size of 2 MB.");
            }

            string fileId = Guid.NewGuid().ToString();


            files[fileId] = fileData;

            if (!channelFiles.ContainsKey(channelName))
            {
                channelFiles[channelName] = new List<FileMetaInfo>();
            }

            channelFiles[channelName].Add(new FileMetaInfo
            {
                FileId = fileId,
                Filename = fileName,
                Sender = fromUserId
            });
            

            var updatedFiles = GetSharedFiles(channelName);
            var members = GetUsers(channelName, fromUserId);

            var snapshot = members.ToList();
            List<string> disconnectedUsers = new List<string>();

            foreach (var member in snapshot)
            {
                if (userCallbacks.TryGetValue(member, out var callback))
                {
                    try
                    {
                        callback.ReceiveFile(channelName, updatedFiles);
                    }
                    catch (Exception)
                    {
                        disconnectedUsers.Add(member);
                    }
                }
            }
            foreach (string user in disconnectedUsers)
            {
                ClientDisconnected(user);
            }

        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public List<FileMetaInfo> GetSharedFiles(string channelName)
        {

            if (channelFiles.ContainsKey(channelName))
            {
                return channelFiles[channelName].ToList();
            }
            
            return new List<FileMetaInfo>();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public byte[] DownloadFile(string channelName, string fileId)
        {

            if (channelFiles.ContainsKey(channelName))
            {
                for (int i = 0; i < channelFiles[channelName].Count; i++)
                {
                    if (channelFiles[channelName][i].FileId == fileId && files.ContainsKey(fileId))
                    {
                        return files[fileId];
                    }
                }
            }
            
            
            throw new FileNotFoundException("File not found.");
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void ClientDisconnected(string userId)
        {
            if (!signedInUsers.Contains(userId))
            {
                return;
            }

            signedInUsers.Remove(userId);
            userCallbacks.Remove(userId);

            for (int i = 0; i < channels.Count; i++)
            {
                int userIndex = channels[i].Users.IndexOf(userId);

                if (userIndex != -1)
                {
                    channels[i].Users.RemoveAt(userIndex);

                    if (userIndex < channels[i].JoinIndexes.Count)
                    {
                        channels[i].JoinIndexes.RemoveAt(userIndex);
                    }

                    List<string> usersActive = channels[i].Users.ToList();

                    foreach (string user in usersActive)
                    {
                        if (userCallbacks.TryGetValue(user, out var callback))
                        {
                            try
                            {
                                callback.MemberlistChange(channels[i].Name, usersActive);
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }

                    break;
                }
            }
        }
    }
}