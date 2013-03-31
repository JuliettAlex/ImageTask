using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ImageClientWPF
{
    public partial class MainWindow
    {
        const int BUFFER_SIZE = 1024;
        const int PORT = 5676;
        IPAddress IP_ADDRESS = new IPAddress(new byte[] { 127, 0, 0, 1 });

        ObservableCollection<BitmapImage> items = new ObservableCollection<BitmapImage>();

        private void ClientStart()
        {
            using (TcpClient client = new TcpClient())
            {
                client.Connect(IP_ADDRESS, PORT);

                using (NetworkStream networkStream = client.GetStream())
                {
                    while (true)
                    {
                        if (networkStream.DataAvailable)
                        {
                            byte[] length = new byte[4];
                            int bytesRead = networkStream.Read(length, 0, 4);
                            int dataLength = BitConverter.ToInt32(length, 0);
                            int allBytesRead = 0;

                            int bytesLeft = dataLength;
                            byte[] data = new byte[dataLength];

                            while (bytesLeft > 0)
                            {
                                int nextPacketSize = (bytesLeft > BUFFER_SIZE) ? BUFFER_SIZE : bytesLeft;
                                bytesRead = networkStream.Read(data, allBytesRead, nextPacketSize);
                                allBytesRead += bytesRead;
                                bytesLeft -= bytesRead;
                            }

                            Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.Background,
                                new Action(() =>
                                    {
                                        using (MemoryStream memoryStream = new MemoryStream(data))
                                        {
                                            memoryStream.Position = 0;
                                            BitmapImage bitmapImage = new BitmapImage();
                                            bitmapImage.BeginInit();
                                            bitmapImage.StreamSource = memoryStream;
                                            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                                            bitmapImage.DecodePixelHeight = 250;
                                            bitmapImage.EndInit();
                                            items.Add(bitmapImage);
                                        }
                                    }));
                        }
                    }
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            listbox1.ItemsSource = items;
            Thread thread = new Thread(ClientStart);
            thread.IsBackground = true;
            thread.Start();
        }
    }
}