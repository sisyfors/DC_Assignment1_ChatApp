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
        public void ReceiveMessage(string channelName, string userId, string message)
        {
            mainWindow.Dispatcher.Invoke(new Action(() =>
            {
                mainWindow.MessageListBox.Items.Add($"{userId}: {message}");
            }));
        }

        public void ReceivePrivateMessage(string fromUserId, string message)
        {
            // Handle received private message
        }

        public void ReceiveFile(string channelName, List<FileMetaInfo> updatedFiles)
        {
            // UP TO HERE
            // Handle received file
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
