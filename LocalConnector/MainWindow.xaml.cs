using Microsoft.Win32;
using System.Windows;
using System.Text.Json;
using System.IO;
using Newtonsoft.Json.Linq;

namespace LocalConnector
{
    public partial class MainWindow : Window
    {
        string title;
        string? ChoosenFile;
        char UserType;
        bool IsClientInChangeIpPage = false; 

        string CurrentPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ReceivedFiles");
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

                CurrentPath = config.Path;
                CurrentIp = config.IP;
            }

            if (!Directory.Exists(CurrentPath))
            {
                Directory.CreateDirectory(CurrentPath);
            }

            ChangeFolderTB.Text = CurrentPath;

            ChooseOrSendBtn.Content = "Choose file";

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
                string selectedPath = Path.GetDirectoryName(dialog.FileName);

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

        private void ChangeIpBtn_Click(object sender, RoutedEventArgs e)
        {
            string input = ChangeIpTBox.Text;

            if (!string.IsNullOrEmpty(input) && System.Net.IPAddress.TryParse(input, out _))
            {
                var config = JObject.Parse(File.ReadAllText("config.json"));
                config["IP"] = input;
                File.WriteAllText("config.json", config.ToString());

                CurrentIp = input;
            }
            else if (!string.IsNullOrEmpty(input))
            {
                MessageBox.Show("Invalid IP address format!", "Error");
                ChangeIpTBox.Text = CurrentIp;
            }
        }
    }

    public class Config
    {
        public string? Path { get; set; }
        public string? IP { get; set; }
    }
}