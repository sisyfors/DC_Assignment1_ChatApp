using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.ServiceModel;
using ChatContracts;
using Microsoft.Win32;

namespace DuplexClient
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

        public void UpdateMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                MessageListBox.Items.Add(message);
            });
        }

        public void UpdateMembers(List<string> userList)
        {
            Dispatcher.Invoke(() =>
            {
                MemberListBox.Items.Clear();
                foreach (string user in userList)
                {
                    MemberListBox.Items.Add(user);
                }
            });
        }

        public void UpdateChannels(List<Channel> channelsList)
        {
            Dispatcher.Invoke(() =>
            {
                ChannelListBox.Items.Clear();
                foreach (Channel channel in channelsList)
                {
                    ChannelListBox.Items.Add(channel.Name);
                }
            });
        }

        public void AddFile(FileMetaInfo file)
        {
            Dispatcher.Invoke(() =>
            {
                foreach (FileMetaInfo currFile in FilesListBox.Items)
                {
                    if (currFile.FileId == file.FileId)
                    {
                        return;
                    }
                }
                FilesListBox.Items.Add(file);
            });
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

            if (!string.IsNullOrEmpty(currentChannelName))
            {
                chatService.LeaveChannel(
                    currentUserId,
                    currentChannelName);
            }

            string reason;

            bool success =
                chatService.SignOut(currentUserId, out reason);

            if (success)
            {
                currentUserId = null;
                currentChannelName = null;

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

                // handle loading message using callback
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

                // handle loading private message using callback
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
                chatService.LeaveChannel(currentUserId, currentChannelName);

            if (success)
            {
                currentChannelName = null;

                ChatView.Visibility =
                    Visibility.Collapsed;

                ChannelView.Visibility =
                    Visibility.Visible;

                MessageListBox.Items.Clear();
                MemberListBox.Items.Clear();
                FilesListBox.Items.Clear();

                LoadChannels();
            }
            else
            {
                MessageBox.Show(
                    "Unable to leave channel.",
                    "Leave Channel");
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

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog uploadBox = new OpenFileDialog();
            uploadBox.Filter = "Allowed Files|*.txt;*.png;*.jpg;*.jpeg;*.gif;*.bmp";

            if (uploadBox.ShowDialog() == true)
            {
                string filePath = uploadBox.FileName;
                string fileName = System.IO.Path.GetFileName(filePath);
                byte[] fileData = File.ReadAllBytes(filePath);

                chatService.ShareFile(currentChannelName, currentUserId, fileName, fileData);
                MessageBox.Show("File uploaded successfully.", "Upload File");
            }

        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FileMetaInfo selectedFile = FilesListBox.SelectedItem as FileMetaInfo;
                if (selectedFile != null)
                {
                    byte[] fileData = chatService.DownloadFile(currentChannelName, selectedFile.FileId);

                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.FileName = selectedFile.Filename;

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        File.WriteAllBytes(saveFileDialog.FileName, fileData);
                        MessageBox.Show("File downloaded successfully.", "Download File");
                    }
                }
                else
                {
                    MessageBox.Show("Please select a file to download.", "Download File");
                }
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show(ex.Message, "Download File");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Download File");
            }
        }
    }
}