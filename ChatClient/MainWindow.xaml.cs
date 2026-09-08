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
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.ServiceModel;
using ChatContracts;

namespace ChatClient
{
    public partial class MainWindow : Window
    {
        private ChannelFactory<IChatService> channelFactory;
        private IChatService chatService;

        private string currentUserId;
        private string currentChannelName;

        public MainWindow()
        {
            InitializeComponent();

            channelFactory =
                new ChannelFactory<IChatService>("ChatServiceEndpoint");

            chatService = channelFactory.CreateChannel();
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            string userId = UserIdTextBox.Text.Trim();

            string reason;

            bool success =
                chatService.SignIn(userId, out reason);

            if (success)
            {
                currentUserId = userId;

                ErrorTextBlock.Text = "";

                ShowChannelList();
            }
            else
            {
                ErrorTextBlock.Text = reason;

                UserIdTextBox.Focus();
                UserIdTextBox.SelectAll();
            }
        }

        private void ShowChannelList()
        {
            SignInView.Visibility = Visibility.Collapsed;
            ChannelView.Visibility = Visibility.Visible;

            LoadChannels();
        }

        private void LoadChannels()
        {
            ChannelListBox.Items.Clear();

            var channels = chatService.GetChannels();

            foreach (Channel channel in channels)
            {
                ChannelListBox.Items.Add(channel.Name);
            }
        }

        private void JoinChannelButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (ChannelListBox.SelectedItem == null)
            {
                MessageBox.Show(
                    "Please select a channel.",
                    "Join Channel");

                return;
            }

            string channelName =
                ChannelListBox.SelectedItem.ToString();

            chatService.JoinChannel(
                currentUserId,
                channelName);

            currentChannelName = channelName;

            ChannelView.Visibility =
                Visibility.Collapsed;

            ChatView.Visibility =
                Visibility.Visible;

            CurrentChannelTextBlock.Text =
                channelName;

            MessageListBox.Items.Clear();

            LoadMembers();
        }

        private void SignOutButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentUserId))
            {
                return;
            }

            string reason;

            bool success =
                chatService.SignOut(currentUserId, out reason);

            if (success)
            {
                currentUserId = null;

                ChannelView.Visibility = Visibility.Collapsed;
                SignInView.Visibility = Visibility.Visible;

                UserIdTextBox.Clear();
            }
            else
            {
                MessageBox.Show(
                    reason,
                    "Sign Out");
            }
        }

        private void CreateChannelButton_Click(
          object sender,
          RoutedEventArgs e)
        {
            string channelName =
                NewChannelTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(channelName))
            {
                ChannelErrorTextBlock.Text =
                    "Channel name cannot be empty.";

                return;
            }

            bool success =
                chatService.CreateChannel(channelName);

            if (success)
            {
                ChannelErrorTextBlock.Text = "";

                NewChannelTextBox.Clear();

                LoadChannels();
            }
            else
            {
                ChannelErrorTextBlock.Text =
                    "A channel with that name already exists.";
            }
        }

        private void SendMessageButton_Click(
          object sender,
          RoutedEventArgs e)
        {
            if (MemberListBox.SelectedItem == null || MemberListBox.SelectedItem.ToString() == currentUserId)
            {
                string message =
                    MessageTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(message))
                {
                    return;
                }

                chatService.SendMessage(
                    currentChannelName,
                    currentUserId,
                    message);

                MessageTextBox.Clear();

                LoadMessages();
            }
            else
            {
                string message =
                    MessageTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(message))
                {
                    return;
                }

                string recipientUserId =
                    MemberListBox.SelectedItem.ToString();

                chatService.SendPrivateMessage(
                    currentUserId,
                    recipientUserId,
                    message);

                MessageTextBox.Clear();

                PrivateWindow privateWindow =
                    new PrivateWindow(currentUserId, recipientUserId);

                privateWindow.Show();

                LoadPrivateMessages(recipientUserId);
            }
        }

        private void LeaveChannelButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentChannelName))
            {
                return;
            }

            bool success =
                chatService.LeaveChannel(
                    currentChannelName,
                    currentUserId);

            if (success)
            {
                currentChannelName = null;

                ChatView.Visibility =
                    Visibility.Collapsed;

                ChannelView.Visibility =
                    Visibility.Visible;

                MessageListBox.Items.Clear();
                MemberListBox.Items.Clear();

                LoadChannels();
            }
            else
            {
                MessageBox.Show(
                    "Unable to leave channel.",
                    "Leave Channel");
            }
        }

        private void LoadMessages()
        {
            MessageListBox.Items.Clear();

            List<string> messages =
                chatService.GetMessages(
                    currentChannelName,
                    currentUserId);

            for (int i = 0; i < messages.Count; i++)
            {
                MessageListBox.Items.Add(messages[i]);
            }
        }

        private void LoadPrivateMessages(string otherUserId)
        {
            MessageListBox.Items.Clear();
            List<string> messages =
                chatService.GetPrivateMessages(
                    currentUserId,
                    otherUserId);
            for (int i = 0; i < messages.Count; i++)
            {
                MessageListBox.Items.Add(messages[i]);
            }
        }

        private void LoadMembers()
        {
            MemberListBox.Items.Clear();

            var users =
                chatService.GetUsers(
                    currentChannelName,
                    currentUserId);

            foreach (string user in users)
            {
                MemberListBox.Items.Add(user);
            }
        }
    }
}