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

namespace ChatClient
{
    public partial class MainWindow : Window
    {
        private ChannelFactory<IChatService> channelFactory;
        private IChatService chatService;

        private string currentUserId;

        public string CurrentUserId
        {
            get { return currentUserId; }
        }
        private string currentChannelName;

        private Thread pollingThread;
        private bool pollingFlag;

        private List<PrivateWindow> privateWindows = new List<PrivateWindow>();

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            channelFactory =
                new ChannelFactory<IChatService>("ChatServiceEndpoint");

            chatService = channelFactory.CreateChannel();
        }

        private void StartPollingThread()
        {
            pollingFlag = true;

            pollingThread = new Thread(PollServer);
            pollingThread.IsBackground = true;

            pollingThread.Start();
        }

        private void CheckPrivateNotifications()
        {
            try
            {
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return;
                }

                List<string> notifications =
                    chatService.GetPrivateNotifications(currentUserId);

                foreach (string senderId in notifications)
                {
                    Dispatcher.Invoke(() =>
                    {
                        foreach (PrivateWindow window in privateWindows)
                        {
                            if (window.OtherUserId == senderId)
                            {
                                window.Activate();
                                return;
                            }
                        }

                        PrivateWindow privateWindow = new PrivateWindow(currentUserId,senderId);

                        privateWindows.Add(privateWindow);

                        privateWindow.Closed +=
                            (sender, e) =>
                            {
                                privateWindows.Remove(privateWindow);
                            };

                        privateWindow.Show();
                    });
                }
            }
            catch
            {
                // Ignore temporary server/polling errors.
            }
        }

        private void PollServer()
        {
            while(pollingFlag)
            {
                if(string.IsNullOrEmpty(currentChannelName))
                {
                    var channels = chatService.GetChannels();

                    Dispatcher.Invoke(() =>
                    {

                        foreach (Channel channel in channels)
                        {
                            if(ChannelListBox.Items.Contains(channel.Name))
                            { 
                                
                            }
                            else{ChannelListBox.Items.Add(channel.Name);}
                            
                        }
                    });
                }
                else
                {
                    var messages = chatService.GetMessages(currentChannelName, currentUserId);
                    var users = chatService.GetUsers(currentChannelName, currentUserId);
                    var files = chatService.GetSharedFiles(currentChannelName);

                    Dispatcher.Invoke(() =>
                    {
                        // polling for the messages
                        MessageListBox.Items.Clear();
                        foreach (string message in messages)
                        {
                            MessageListBox.Items.Add(message);
                        }

                        // Polling for the users
                        foreach (string user in users)
                        {
                            bool userExists = false;

                            foreach (string currentUser in MemberListBox.Items)
                            {
                                if (currentUser == user)
                                {
                                    userExists = true;
                                }
                            }
                            if (userExists != true)
                            {
                                MemberListBox.Items.Add(user);
                            }
                        }

                        for (int i = MemberListBox.Items.Count - 1; i >= 0; i--)
                        {
                            string currentUser =
                                MemberListBox.Items[i].ToString();

                            if (!users.Contains(currentUser))
                            {
                                MemberListBox.Items.RemoveAt(i);
                            }
                        }

                        // Polling for files
                        foreach (FileMetaInfo file in files)
                        {
                            bool fileExists = false;
                            foreach(FileMetaInfo currentFile in FilesListBox.Items)
                            {
                                if(currentFile.FileId == file.FileId)
                                {
                                    fileExists = true;
                                }
                            
                            }
                            if (fileExists != true)
                            {
                                FilesListBox.Items.Add(file);
                            }
                        }
                    });

                }
                CheckPrivateNotifications();
                Thread.Sleep(1000);
            }
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
                StartPollingThread();
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

        private void JoinChannelButton_Click(object sender,RoutedEventArgs e)
        {
            if (ChannelListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a channel.","Join Channel");

                return;
            }

            string channelName =
                ChannelListBox.SelectedItem.ToString();

            chatService.JoinChannel(currentUserId,channelName);

            currentChannelName = channelName;

            ChannelView.Visibility =
                Visibility.Collapsed;

            ChatView.Visibility =
                Visibility.Visible;

            CurrentChannelTextBlock.Text =channelName;

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
            pollingFlag = false;

            if (!string.IsNullOrEmpty(currentChannelName))
            {
                chatService.LeaveChannel(currentUserId,currentChannelName);
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
                MessageBox.Show(reason,"Sign Out");
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
                ChannelErrorTextBlock.Text ="Channel name cannot be empty.";

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
                ChannelErrorTextBlock.Text ="A channel with that name already exists.";
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

                chatService.SendMessage(currentChannelName,currentUserId,message);

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

                chatService.SendPrivateMessage(currentUserId,recipientUserId,message);

                MessageTextBox.Clear();

                PrivateWindow existingWindow = null;

                foreach (PrivateWindow window in privateWindows)
                {
                    if (window.OtherUserId == recipientUserId)
                    {
                        existingWindow = window;
                        break;
                    }
                }

                if (existingWindow != null)
                {
                    existingWindow.Activate();
                }
                else
                {
                    PrivateWindow privateWindow =new PrivateWindow(currentUserId,recipientUserId);

                    privateWindows.Add(privateWindow);

                    privateWindow.Closed +=
                        (closedSender, closedEvent) =>
                        {
                            privateWindows.Remove(privateWindow);
                        };

                    privateWindow.Show();
                }
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

            bool success =chatService.LeaveChannel(currentUserId, currentChannelName);

            if (success)
            {
                currentChannelName = null;

                ChatView.Visibility =Visibility.Collapsed;

                ChannelView.Visibility =Visibility.Visible;

                MessageListBox.Items.Clear();
                MemberListBox.Items.Clear();
                FilesListBox.Items.Clear();

                LoadChannels();
            }
            else
            {
                MessageBox.Show("Unable to leave channel.","Leave Channel");
            }
        }

        private void LoadMessages()
        {
            MessageListBox.Items.Clear();

            List<string> messages =
                chatService.GetMessages(currentChannelName,currentUserId);

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

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog uploadBox = new OpenFileDialog();
            uploadBox.Filter = "Allowed Files|*.txt;*.png;*.jpg;*.jpeg;*.gif;*.bmp";

            if(uploadBox.ShowDialog() == true)
            {
                string filePath = uploadBox.FileName;
                string fileName = System.IO.Path.GetFileName(filePath);
                byte[] fileData = File.ReadAllBytes(filePath);

                chatService.ShareFile(currentChannelName, currentUserId, fileName, fileData);
                MessageBox.Show("File uploaded successfully.", "Upload File");
            }

            FilesListBox.Items.Clear();

            List<FileMetaInfo> files = chatService.GetSharedFiles(currentChannelName);
            
             foreach (FileMetaInfo file in files)
             {
                FilesListBox.Items.Add(file);
             }
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FileMetaInfo selectedFile = FilesListBox.SelectedItem as FileMetaInfo;
                if(selectedFile != null)
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
            catch(FileNotFoundException ex)
            {
                MessageBox.Show(ex.Message, "Download File");
            }
            catch(Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Download File");
            }
        }
    }
}