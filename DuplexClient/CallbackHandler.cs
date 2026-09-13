using ChatContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DuplexClient
{
    public class CallbackHandler : IChatServiceCallback
    {
        private MainWindow mainWindow;
        public CallbackHandler(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        public void NewPrivateWindow(string otherUserId, PrivateWindow privateWindow)
        {
            lock (mainWindow.privateWindows)
            {
                mainWindow.privateWindows[otherUserId] = privateWindow;
            }
        }

        public void ReceivePrivateMessage(string senderId, string recipientId, string message)
        {
            mainWindow.Dispatcher.BeginInvoke(new Action(() =>
            {
                string currentUserId = recipientId;

                lock (mainWindow.privateWindows)
                {
                    if (mainWindow.privateWindows.ContainsKey(senderId))
                    {
                        mainWindow.privateWindows[senderId].PrivateMessageListBox.Items.Add($"{senderId}: {message}");
                    }
                    else
                    {
                        PrivateWindow newPrivateWindow = new PrivateWindow(mainWindow.chatService, currentUserId, senderId);
                        newPrivateWindow.Show();
                        mainWindow.privateWindows[senderId] = newPrivateWindow;

                        newPrivateWindow.Closed += (s, e) =>
                        {
                            lock (mainWindow.privateWindows)
                            {
                                mainWindow.privateWindows.Remove(senderId);
                            }
                        };
                    }
                }
                
            }));
        }

        public void ReceiveMessage(string channelName, string userId, string message)
        {
            mainWindow.Dispatcher.BeginInvoke(new Action(() =>
            {
                mainWindow.MessageListBox.Items.Add($"{userId}: {message}");
            }));
        }

        public void ReceiveFile(string channelName, List<FileMetaInfo> updatedFiles)
        {
            mainWindow.Dispatcher.BeginInvoke(new Action(() =>
            {
                mainWindow.FilesListBox.Items.Clear();
                foreach (var file in updatedFiles)
                {
                    mainWindow.FilesListBox.Items.Add(file);
                }
            }));
        }

        public void MemberlistChange(string channelName, List<string> members)
        {
            mainWindow.Dispatcher.BeginInvoke(new Action(() =>
            {
                mainWindow.MemberListBox.Items.Clear();
                foreach (string member in members)
                {
                     mainWindow.MemberListBox.Items.Add(member);
                }
            }));
        }

        public void ChannelListChange(string channelName)
        {
            mainWindow.Dispatcher.BeginInvoke(new Action(() =>
            {
                mainWindow.ChannelListBox.Items.Add(channelName);
            }));
        }
    }
}
