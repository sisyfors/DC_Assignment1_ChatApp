using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ServiceModel;
using ChatContracts;
using ChatServer;

namespace DuplexClient
{
    /// <summary>
    /// Interaction logic for PrivateWindow.xaml
    /// </summary>
    /// 

    public partial class PrivateWindow : Window
    {
        private IChatService chatService;

        private string currentUserId;
        private string recipientId;

        public PrivateWindow(IChatService chatService, string currentUserId, string recipientId)
        {
            InitializeComponent();

            this.chatService = chatService;

            this.currentUserId = currentUserId;
            this.recipientId = recipientId;

            PrivateTextBlock.Text = $"Private chat with {recipientId}";


            LoadPrivateMessages();
        }

        public void AddMessage(string message)
        {
            PrivateMessageListBox.Items.Add(message);
        }

        private void LoadPrivateMessages()
        {
            PrivateMessageListBox.Items.Clear();
            List<string> messages =
                chatService.GetPrivateMessages(
                    currentUserId,
                    recipientId);
            for (int i = 0; i < messages.Count; i++)
            {
                PrivateMessageListBox.Items.Add(messages[i]);
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string message =
                PrivateMessageTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            chatService.SendPrivateMessage(
                currentUserId,
                recipientId,
                message);

            PrivateMessageListBox.Items.Add($"{currentUserId}: {message}");

            PrivateMessageTextBox.Clear();

        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
