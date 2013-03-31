
using System.Drawing;
using System.Drawing.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;


namespace ImageServer
{
    class Program
    {
        static int countClient = 0;
        const int PORT = 5676;
        IPAddress IP_ADDRESS = new IPAddress(new byte[] { 127, 0, 0, 1 });
        const int BUFFER_SIZE = 1024;
        const int DATA_LENGTH_OFFSET = 4;

        private void ServerStart()
        {
            TcpListener listener = new TcpListener(IP_ADDRESS, PORT);
            listener.Start();
            Console.WriteLine("Server running, listening to port " + PORT + " at " + IP_ADDRESS);

            Task parentTask = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    Task.Factory.StartNew(() => Service(client), TaskCreationOptions.LongRunning);
                }
            }, TaskCreationOptions.LongRunning);

            parentTask.Wait();
            listener.Stop();

        }

        private void Service(TcpClient tcpClient)
        {
            try
            {
                countClient++;

                using (NetworkStream networkStream = tcpClient.GetStream())
                {
                    Console.WriteLine("Клиентов " + countClient + " подключено");

                    foreach (FileInfo f in GetFilesFromDir(@"..\..\Images"))
                    {
                        byte[] data = File.ReadAllBytes(f.FullName);
                        byte[] dataLength = BitConverter.GetBytes(data.Length);
                        byte[] package = new byte[DATA_LENGTH_OFFSET + data.Length];
                        dataLength.CopyTo(package, 0);
                        data.CopyTo(package, DATA_LENGTH_OFFSET);

                        int bytesSent = 0;
                        int bytesLeft = package.Length;

                        while (bytesLeft > 0)
                        {
                            int nextPacketSize = (bytesLeft > BUFFER_SIZE) ? BUFFER_SIZE : bytesLeft;
                            networkStream.Write(package, bytesSent, nextPacketSize);
                            bytesSent += nextPacketSize;
                            bytesLeft -= nextPacketSize;
                        }
                        Thread.Sleep(500);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Console.WriteLine("Клиентов " + (countClient - 1) + " подключено");
                countClient--;
            }
        }

        private FileInfo[] GetFilesFromDir(string path)
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            FileInfo[] files = dir.GetFiles();
            return files;
        }

        static void Main(string[] args)
        {
            bool onlyInstance = false;
            Mutex mutex = new Mutex(true, "ImageServer", out onlyInstance);
            if (!onlyInstance)
            {
                return;
            }
            Program server = new Program();
            server.ServerStart();
        }
    }
}

