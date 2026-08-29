using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatContracts;

namespace ChatServer
{
    public class ChatService : IChatService
    {
        private static readonly HashSet<string> signedInUsers =
            new HashSet<string>();

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

        public bool SignOut(string userId, out string reason)
        {
            reason = "";

            if (!signedInUsers.Contains(userId))
            {
                reason = "User is not currently signed in.";
                return false;
            }

            signedInUsers.Remove(userId);

            return true;
        }

        private static readonly List<Channel> channels = new List<Channel>
        {
                new Channel { Name = "General" },
                new Channel { Name = "Room1" },
                new Channel { Name = "Room2" }
        };

        public List<Channel> GetChannels()
        {
            return channels;
        }
    }
}