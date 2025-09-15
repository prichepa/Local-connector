using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace LocalConnector
{
    public class TcpClientNHost
    {
        static Socket? Client = null;
        static Socket? ServerSocket;
        static string? SaveDirectory;

        public static bool isConnected => Client == null ? false : true;

        public static void Start(string? _saveDirectory, string ip)
        {
            SaveDirectory = _saveDirectory;

            Client = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp
                );

            try
            {
                Client.Connect(new IPEndPoint(IPAddress.Parse(ip), 8080));

                Task.Run(StartReceivingFiles);
            }
            catch (SocketException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public static void Start(string? _saveDirectory)
        {
            SaveDirectory = _saveDirectory;

            ServerSocket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp
                );

            ServerSocket.Bind(new IPEndPoint(IPAddress.Any, 8080));
            ServerSocket.Listen(100);

            Task.Run(WaitForClients);
        }

        public static void WaitForClients()
        {
            while (true)
            {
                Socket? clientSocket = ServerSocket?.Accept();
                Task.Run(() => ManageClient(clientSocket));
            }
        }

        public static void ManageClient(Socket? clientSocket)
        {
            if (Client == null)
            {
                Client = clientSocket;

                try
                {
                    Task.Run(StartReceivingFiles);
                }
                catch (SocketException ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                clientSocket?.Close();
            }
        }

        static void StartReceivingFiles()
        {
            Task.Run(() =>
            {
                while (Client?.Connected == true)
                {
                    try
                    {
                        if (SaveDirectory == null || !Directory.Exists(Path.GetDirectoryName(SaveDirectory)))
                        {
                            SaveDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ReceivedFiles");
                        }
                        if (!Directory.Exists(SaveDirectory))
                        {
                            Directory.CreateDirectory(SaveDirectory);
                        }

                        ReceiveFile();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        break;
                    }
                }
            });
        }

        public static void ReceiveFile()
        {
            int nameLength = BitConverter.ToInt32(ReceiveExact(Client, 4), 0);

            string fileName = Encoding.UTF8.GetString(ReceiveExact(Client, nameLength));

            int extLength = BitConverter.ToInt32(ReceiveExact(Client, 4), 0);
            string extension = Encoding.UTF8.GetString(ReceiveExact(Client, extLength));

            long fileSize = BitConverter.ToInt64(ReceiveExact(Client, 8), 0);

            string fullPath = Path.Combine(SaveDirectory, fileName);

            using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            {
                byte[] buffer = new byte[8192];
                long totalReceived = 0;

                while (totalReceived < fileSize)
                {
                    int toRead = (int)Math.Min(buffer.Length, fileSize - totalReceived);
                    int received = Client.Receive(buffer, 0, toRead, SocketFlags.None);

                    fs.Write(buffer, 0, received);
                    totalReceived += received;
                }
            }
        }
        static byte[] ReceiveExact(Socket client, int size)
        {
            byte[] data = new byte[size];
            int totalRead = 0;

            while (totalRead < size)
            {
                int read = client.Receive(data, totalRead, size - totalRead, SocketFlags.None);

                totalRead += read;
            }

            return data;
        }

        public static void SendFile(string? fileName)
        {
            Task.Run(() =>
            {
                if (string.IsNullOrEmpty(fileName)) return;

                var fileInfo = new FileInfo(fileName);

                try
                {
                    using var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);

                    byte[] nameBytes = Encoding.UTF8.GetBytes(fileInfo.Name);
                    Client?.Send(BitConverter.GetBytes(nameBytes.Length));
                    Client?.Send(nameBytes);

                    byte[] extBytes = Encoding.UTF8.GetBytes(fileInfo.Extension);
                    Client?.Send(BitConverter.GetBytes(extBytes.Length));
                    Client?.Send(extBytes);

                    Client?.Send(BitConverter.GetBytes(fs.Length));

                    byte[] buffer = new byte[8192];
                    int bytesRead;
                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        int totalSent = 0;
                        while (totalSent < bytesRead)
                        {
                            int sent = Client.Send(buffer, totalSent, bytesRead - totalSent, SocketFlags.None);
                            totalSent += sent;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            });
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1";
        }
    }
}