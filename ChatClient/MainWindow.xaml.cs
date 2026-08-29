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

            MessageBox.Show(
                "You selected " + channelName,
                "Join Channel");
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
    }
}