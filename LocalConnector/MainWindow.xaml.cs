using Microsoft.Win32;
using System.Windows;
using System.Text.Json;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Printing;
using System.Windows.Controls;
using System.Windows.Media;
using System;
using System.Diagnostics;
using System.Windows.Shapes;

namespace LocalConnector
{
    public partial class MainWindow : Window
    {
        string title;
        string? ChoosenFile;
        char UserType;
        bool IsClientInChangeIpPage = false;

        string CurrentPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ReceivedFiles");
        string CurrentIp = "127.0.0.1";

        public MainWindow()
        {
            InitializeComponent();

            string fileName = "config.json";

            Config config;

            if (!File.Exists(fileName))
            {
                config = new Config
                {
                    Path = CurrentPath,
                    IP = CurrentIp
                };
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(fileName, json);
            }
            else
            {
                string json = File.ReadAllText(fileName);
                config = JsonSerializer.Deserialize<Config>(json);

                if (Directory.Exists(config.Path))
                {
                    CurrentPath = config.Path;
                }
                else
                {
                    Directory.CreateDirectory(CurrentPath);
                }
                CurrentIp = config.IP;
            }

            ChangeFolderTB.Text = CurrentPath;

            ChooseOrSendBtn.Content = "Choose file";
            WantchFolderAfterConnection.Visibility = Visibility.Collapsed;
            ChangeIpBtn.IsEnabled = true;

            title = Title;
        }

        async void HostBtn_Click(object sender, RoutedEventArgs e)
        {
            TcpClientNHost.Start(CurrentPath);

            UserType = 'h';

            IpLabel.Visibility = Visibility.Visible;
            IpLabel.Content = $"IP: {TcpClientNHost.GetLocalIPAddress()}";

            ClientBtn.Visibility = Visibility.Collapsed;
            HostBtn.Visibility = Visibility.Collapsed;
            ChangeFolderBtn.Visibility = Visibility.Collapsed;
            ChangeFolderTB.Visibility = Visibility.Collapsed;

            Title = title + " - waiting for client";

            await Task.Run(() =>
            {
                while (!TcpClientNHost.isConnected)
                {
                    Thread.Sleep(100);
                }
            });

            Title = title + " - connected";

            ChooseOrSendBtn.Visibility = Visibility.Visible;
            DragNDropElement.Visibility = Visibility.Visible;
            WantchFolderAfterConnection.Visibility = Visibility.Visible;
        }

        async void ClientBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!IsClientInChangeIpPage)
            {
                HostBtn.Visibility = Visibility.Collapsed;
                ChangeFolderBtn.Visibility = Visibility.Collapsed;
                ChangeFolderTB.Visibility = Visibility.Collapsed;

                ChangeIpBtn.Visibility = Visibility.Visible;
                ChangeIpTBox.Visibility = Visibility.Visible;
                IsClientInChangeIpPage = true;

                ChangeIpTBox.Text = CurrentIp;
                ChangeIpBtn.Background = Brushes.White;
            }
            else
            {
                TcpClientNHost.Start(CurrentPath, CurrentIp);

                UserType = 'c';

                ClientBtn.Visibility = Visibility.Collapsed;
                ChangeIpBtn.Visibility = Visibility.Collapsed;
                ChangeIpTBox.Visibility = Visibility.Collapsed;

                Title = title + " - connecting";

                await Task.Run(() =>
                {
                    while (!TcpClientNHost.isConnected)
                    {
                        Thread.Sleep(100);
                    }
                });

                Title = title + " - connected";

                ChooseOrSendBtn.Visibility = Visibility.Visible;
                DragNDropElement.Visibility = Visibility.Visible;
                WantchFolderAfterConnection.Visibility = Visibility.Visible;
            }
        }

        void ChooseOrSendBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ChoosenFile == null)
            {
                var fd = new OpenFileDialog();
                if (fd.ShowDialog() == true)
                {
                    ChoosenFile = fd.FileName;
                    ChooseOrSendBtn.Content = "Send file";
                    DragNDropElement.IsEnabled = false;
                    DragNDropStatTB.Visibility = Visibility.Collapsed;

                    CancelBtn.Visibility = Visibility.Visible;
                }
            }
            else
            {
                if (UserType == 'h')
                {
                    TcpClientNHost.SendFile(ChoosenFile);
                }
                else
                {
                    TcpClientNHost.SendFile(ChoosenFile);
                }

                ChoosenFile = null;
                ChooseOrSendBtn.Content = "Choose file";
                DragNDropElement.IsEnabled = true;
                DragNDropStatTB.Visibility = Visibility.Visible;

                CancelBtn.Visibility = Visibility.Collapsed;
            }
        }

        void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            ChoosenFile = null;
            ChooseOrSendBtn.Content = "Choose file";
            DragNDropElement.IsEnabled = true;
            DragNDropStatTB.Visibility = Visibility.Visible;

            CancelBtn.Visibility = Visibility.Collapsed;
        }

        void Border_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                ChoosenFile = files[0];

                ChooseOrSendBtn.Content = "Send file";
                DragNDropElement.IsEnabled = false;
                DragNDropStatTB.Visibility = Visibility.Collapsed;

                CancelBtn.Visibility = Visibility.Visible;
            }
        }

        void ChangeFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Select this folder",
                Title = "Select folder"
            };

            if (dialog.ShowDialog() == true)
            {
                string selectedPath = System.IO.Path.GetDirectoryName(dialog.FileName);

                if (string.IsNullOrEmpty(selectedPath))
                    selectedPath = dialog.FileName;

                var config = File.Exists("config.json") ?
                    JObject.Parse(File.ReadAllText("config.json")) : new JObject();
                config["Path"] = selectedPath;
                File.WriteAllText("config.json", config.ToString());

                CurrentPath = selectedPath;
                ChangeFolderTB.Text = selectedPath;
            }
        }
        private void ChangeIpTBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsIpValid(ChangeIpTBox.Text))
            {
                ChangeIpBtn.IsEnabled = false;
            }
            else
            {
                ChangeIpBtn.IsEnabled = true;
            }
            if (CurrentIp != ChangeIpTBox.Text)
            {
                ChangeIpBtn.Background = Brushes.LightGreen;
                ClientBtn.Visibility = Visibility.Collapsed;
            }
            else
            {
                ClientBtn.Visibility = Visibility.Visible;
                ChangeIpBtn.Background = Brushes.White;
            }
        }
        private void ChangeIpBtn_Click(object sender, RoutedEventArgs e)
        {
            string input = ChangeIpTBox.Text;
            var config = JObject.Parse(File.ReadAllText("config.json"));
            config["IP"] = input;
            File.WriteAllText("config.json", config.ToString());
            ChangeIpBtn.Background = Brushes.White;
            ClientBtn.Visibility = Visibility.Visible;

            CurrentIp = input;
        }
        private void WantchFolderAfterConnection_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", CurrentPath);
        }
        public int CheckDots(string ip)
        {
            int counter = 0;
            for(int i = 0; i< ip.Length; i++)
            {
                if (ip[i] == '.')
                {
                    counter++;
                }
            }
            return counter;
        }
        public bool IsIpValid(string ip)
        {
            if (ip == "" || CheckDots(ip) < 3)
            {
                return false;
            }
            else
            {
                string ip_sample = ip;
                int secondIndex = 0;
                for (int i = 1; i <= 4; i++)
                {
                    string buffer = "";
                    while (secondIndex != ip_sample.Length && ip_sample[secondIndex] != '.')
                    {
                        buffer += ip_sample[secondIndex];
                        secondIndex++;
                    }
                    secondIndex++;

                    if (buffer == "" || (Convert.ToInt32(buffer) >= 255 || Convert.ToInt32(buffer) <= 0))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

    }

    public class Config
    {
        public string? Path { get; set; }
        public string? IP { get; set; }
    }
}