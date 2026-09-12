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
        internal IChatService chatService;

        private IChatServiceCallback foobCallback;

        private string currentUserId;
        private string currentChannelName;

        private Dictionary<string, PrivateWindow> privateWindows = new Dictionary<string, PrivateWindow>();


        public MainWindow()
        {
            InitializeComponent();

            DuplexChannelFactory<IChatService> foobFactory;
            NetTcpBinding netTcpBinding = new NetTcpBinding();
            string URL = "net.tcp://localhost:8100/ChatService";
            foobCallback = new CallbackHandler(this);
            foobFactory = new DuplexChannelFactory<IChatService>
                (foobCallback, netTcpBinding, URL);
            chatService = foobFactory.CreateChannel();
        }

        private void NewPrivateWindow(string otherUserId, PrivateWindow privateWindow)
        {
            lock (privateWindows)
            {
                privateWindows[otherUserId] = privateWindow;
            }
        }

        public void OnPrivateMessageReceived(string fromUserId, string message)
        {
            Dispatcher.Invoke(() =>
            {
                if (privateWindows.TryGetValue(fromUserId, out var privateWindow))
                {
                    privateWindow.AddMessage($"{fromUserId}: {message}");
                }
                else
                {
                    PrivateWindow newPrivateWindow = new PrivateWindow(chatService, currentUserId, fromUserId);
                    NewPrivateWindow(fromUserId, newPrivateWindow);
                    newPrivateWindow.AddMessage($"{fromUserId}: {message}");
                    newPrivateWindow.Show();
                }   
            });
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

        private async void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            string userId = UserIdTextBox.Text.Trim();
            string reason = null;
            bool success = false;

            try
            {
                success = await Task.Run(() =>
                {
                    string localReason;
                    bool localSuccess = chatService.SignIn(userId, out localReason);

                    reason = localReason;

                    return localSuccess;
                });
            }
            catch (Exception ex)
            {
                ErrorTextBlock.Text = $"Error: {ex.Message}";
                return;
            }

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

        private async void ShowChannelList()
        {
            SignInView.Visibility = Visibility.Collapsed;
            ChannelView.Visibility = Visibility.Visible;

            await LoadChannels();
        }

        private async Task LoadChannels()
        {

            List<Channel> channels = null;

            try
            {
                channels = await Task.Run(() => chatService.GetChannels());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to load channels: {ex.Message}", "Load Channels");
                return;
            }

            ChannelListBox.Items.Clear();
            foreach (Channel channel in channels)
            {
                ChannelListBox.Items.Add(channel.Name);
            }
        }

        private async void JoinChannelButton_Click(
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

            try
            {
                await Task.Run(() => chatService.JoinChannel(currentUserId, channelName));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to join channel: {ex.Message}", "Join Channel");
                return;
            }

            currentChannelName = channelName;

            ChannelView.Visibility =
                Visibility.Collapsed;

            ChatView.Visibility =
                Visibility.Visible;

            CurrentChannelTextBlock.Text =
                channelName;

            MessageListBox.Items.Clear();

            await LoadMembers();
        }

        private async void SignOutButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentUserId))
            {
                return;
            }

            if (!string.IsNullOrEmpty(currentChannelName))
            {
                try
                {
                    await Task.Run(() => chatService.LeaveChannel(currentUserId, currentChannelName));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unable to leave channel: {ex.Message}", "Sign Out");
                    return;
                }
            }

            string reason = null;
            bool success = false;

            try
            {
                success = await Task.Run(() =>
                {
                    string localReason;
                    bool localSuccess = chatService.SignOut(currentUserId, out localReason);
                    reason = localReason;
                    return localSuccess;
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to sign out: {ex.Message}", "Sign Out");
                return;
            }

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

        private async void CreateChannelButton_Click(
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

            bool success = false;

            try
            {
                success = await Task.Run(() => chatService.CreateChannel(channelName));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to create channel: {ex.Message}", "Create Channel");
                return;
            }

            if (success)
            {
                ChannelErrorTextBlock.Text = "";

                NewChannelTextBox.Clear();
            }
            else
            {
                ChannelErrorTextBlock.Text =
                    "A channel with that name already exists.";
            }
        }

        private async void SendMessageButton_Click(
          object sender,
          RoutedEventArgs e)
        {
            string message = MessageTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (MemberListBox.SelectedItem == null || MemberListBox.SelectedItem.ToString() == currentUserId)
            {
                MessageTextBox.Clear();

                try
                {
                    await Task.Run(() => chatService.SendMessage(currentChannelName, currentUserId, message));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unable to send message: {ex.Message}", "Send Message");
                    return;
                }
            }
            else
            {
                string recipientUserId = MemberListBox.SelectedItem.ToString();
                MessageTextBox.Clear();

                try
                {
                    await Task.Run(() => chatService.SendPrivateMessage(currentUserId, recipientUserId, message));
                    PrivateWindow privateWindow = new PrivateWindow(chatService, currentUserId, recipientUserId);
                    privateWindow.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unable to send private message: {ex.Message}", "Send Private Message");
                    return;
                }
            }
        }

        private async void LeaveChannelButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentChannelName))
            {
                return;
            }

            bool success = false;

            try
            {
                success = await Task.Run(() => chatService.LeaveChannel(currentUserId, currentChannelName));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to leave channel: {ex.Message}", "Leave Channel");
                return;
            }

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

                await LoadChannels();
            }
            else
            {
                MessageBox.Show(
                    "Unable to leave channel.",
                    "Leave Channel");
            }
        }

        private async Task LoadMembers()
        {
            List<string> members = null;

            try
            {
                members = await Task.Run(() => chatService.GetUsers(currentChannelName, currentUserId));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to load members: {ex.Message}", "Load Members");
                return;
            }

            MemberListBox.Items.Clear();

            
            foreach (string user in members)
            {
                MemberListBox.Items.Add(user);
            }
        }

        private async void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog uploadBox = new OpenFileDialog();
            uploadBox.Filter = "Allowed Files|*.txt;*.png;*.jpg;*.jpeg;*.gif;*.bmp";

            if (uploadBox.ShowDialog() == true)
            {
                string filePath = uploadBox.FileName;
                string fileName = System.IO.Path.GetFileName(filePath);

                try
                {
                    byte[] fileData = await Task.Run(() => File.ReadAllBytes(filePath));
                    await Task.Run(() => chatService.ShareFile(currentChannelName, currentUserId, fileName, fileData));
                    MessageBox.Show("File uploaded successfully.", "Upload File");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while uploading the file: {ex.Message}", "Upload File");


                }
            }
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            FileMetaInfo selectedFile = FilesListBox.SelectedItem as FileMetaInfo;
            if (selectedFile == null)
            {
                MessageBox.Show("Please select a file to download.", "Download File");
                return;
            }

            try
            {
                byte[] fileData = await Task.Run(() => chatService.DownloadFile(currentChannelName, selectedFile.FileId));
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.FileName = selectedFile.Filename;
                if (saveFileDialog.ShowDialog() == true)
                {
                    await Task.Run(() => File.WriteAllBytes(saveFileDialog.FileName, fileData));
                    MessageBox.Show("File downloaded successfully.", "Download File");
                }
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show(ex.Message, "Download File");
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Download File");
            } 
        }
    }
}