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
        private Dictionary<string, PrivateWindow> privateWindows = new Dictionary<string, PrivateWindow>();
        public CallbackHandler(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        public void NewPrivateWindow(string otherUserId, PrivateWindow privateWindow)
        {
            privateWindows[otherUserId] = privateWindow;
        }

        public void ReceivePrivateMessage(string senderId, string recipientId, string message)
        {
            mainWindow.Dispatcher.Invoke(new Action(() =>
            {
                if (privateWindows.ContainsKey(senderId))
                {
                    privateWindows[senderId].PrivateMessageListBox.Items.Add($"{senderId}: {message}");
                }
                else
                {
                    PrivateWindow newPrivateWindow = new PrivateWindow(senderId, recipientId);
                    newPrivateWindow.Show();
                    newPrivateWindow.PrivateMessageListBox.Items.Add($"{senderId}: {message}");
                    privateWindows[senderId] = newPrivateWindow;
                }
            }));
        }

        public void ReceiveMessage(string channelName, string userId, string message)
        {
            mainWindow.Dispatcher.Invoke(new Action(() =>
            {
                mainWindow.MessageListBox.Items.Add($"{userId}: {message}");
            }));
        }

        public void ReceiveFile(string channelName, List<FileMetaInfo> updatedFiles)
        {
            mainWindow.Dispatcher.Invoke(new Action(() =>
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
            mainWindow.Dispatcher.Invoke(new Action(() =>
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
            mainWindow.Dispatcher.Invoke(new Action(() =>
            {
                mainWindow.ChannelListBox.Items.Add(channelName);
            }));
        }
    }
}
