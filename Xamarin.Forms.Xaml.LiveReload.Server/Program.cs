using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.Forms.Xaml.LiveReload.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            var port = 52222;
            
            Task.Run(() =>
            {
                
                while (true)
                {
                    var udp = new UdpClient {EnableBroadcast = true};
                    udp.Send(new byte[0], 0, "255.255.255.255", port);
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            });
            
            var clients = new List<TcpClient>();

            var tcpListener = new TcpListener(port);
            tcpListener.Start();

            Task.Run(() =>
            {
                while (true)
                {
                    var client = tcpListener.AcceptTcpClient();
                    Console.WriteLine($"Client connected from {client.Client.RemoteEndPoint}");
                    clients.Add(client);
                    Task.Run(() =>
                    {
                        while (true)
                        {
                            var message = client.ReceiveMessage();
                            switch (message.MessageType)
                            {
                                case MessageType.GetHostname:
                                    client.SendMessage(new Message
                                    {
                                        MessageType = MessageType.GetHostnameResponse,
                                        Payload = Encoding.UTF8.GetBytes(Dns.GetHostName())
                                    });
                                    break;
                            }
                        }
                    });
                }
            });

            var fw = new FileSystemWatcher(Directory.GetCurrentDirectory())
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite
            };
            fw.Changed += (sender, eventArgs) =>
            {
                var extension = Path.GetExtension(eventArgs.FullPath);
                if (extension != ".xaml~*" && extension != ".xaml") return;
                var tildeIndex = eventArgs.FullPath.IndexOf('~');

                var path = tildeIndex > 0
                    ? eventArgs.FullPath.Substring(0, eventArgs.FullPath.IndexOf('~'))
                    : eventArgs.FullPath;

                Console.WriteLine(path);
                var xaml = "";
                using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var textReader = new StreamReader(fileStream))
                {
                    xaml = textReader.ReadToEnd();
                }
                
                clients.RemoveAll(x => !x.Connected);
                clients.SendMessage(new Message
                {
                    MessageType = MessageType.XamlUpdated,
                    Payload = Encoding.UTF8.GetBytes(xaml)
                });

            };
            Console.WriteLine($"Watching for file changes in {Directory.GetCurrentDirectory()}");
            
            Console.ReadLine();
        }
    }
}
