using ChatContracts;
using ChatServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ChatClient
{
    public partial class PrivateWindow : Window
    {
        private IChatService chatService;

        private string currentUserId;
        private string recipientId;

        private Thread pollingThread;
        private bool pollingFlag;

        public string OtherUserId
        {
            get
            {
                return recipientId;
            }
        }

        public PrivateWindow(
            IChatService chatService,
            string currentUserId,
            string recipientId)
        {
            InitializeComponent();

            this.chatService =chatService;

            this.currentUserId =currentUserId;

            this.recipientId =recipientId;

            PrivateTextBlock.Text =$"Private chat with {recipientId}";

            LoadPrivateMessages();

            StartPolling();
        }

        private void StartPolling()
        {
            pollingFlag = true;

            pollingThread =
                new Thread(PollMessages);

            pollingThread.IsBackground = true;

            pollingThread.Start();
        }

        private void PollMessages()
        {
            while (pollingFlag)
            {
                try
                {
                    List<string> messages =chatService.GetPrivateMessages(currentUserId,recipientId);

                    Dispatcher.Invoke(() =>
                    {
                        PrivateMessageListBox.Items.Clear();

                        foreach (string message in messages)
                        {
                            PrivateMessageListBox.Items.Add(message);
                        }

                        if (PrivateMessageListBox.Items.Count > 0)
                        {
                            PrivateMessageListBox.ScrollIntoView(PrivateMessageListBox.Items[PrivateMessageListBox.Items.Count - 1]);
                        }
                    });
                }
                catch
                {
                    // The window is closing or the server
                    // is temporarily unavailable.
                }

                Thread.Sleep(1000);
            }
        }

        private void LoadPrivateMessages()
        {
            PrivateMessageListBox.Items.Clear();

            List<string> messages =
                chatService.GetPrivateMessages(currentUserId,recipientId);

            for (int i = 0;
                 i < messages.Count;i++)
            {
                PrivateMessageListBox.Items.Add(messages[i]);
            }
        }

        private void SendButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string message =PrivateMessageTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            try
            {
                chatService.SendPrivateMessage(currentUserId, recipientId, message);

                PrivateMessageTextBox.Clear();

                LoadPrivateMessages();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send message: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void CloseButton_Click(object sender,RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            pollingFlag = false;

            base.OnClosed(e);
        }

        private void SendButton_Click_1(object sender, RoutedEventArgs e)
        {

        }
    }
}