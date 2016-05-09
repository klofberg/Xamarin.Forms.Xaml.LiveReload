using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace Xamarin.Forms.Xaml.LiveReload
{
    public enum MessageType
    {
        None,
        GetHostname,
        GetHostnameResponse,
        XamlUpdated
    }

    public class Message
    {
        public MessageType MessageType { get; set; }
        public byte[] Payload { get; set; }
    }
    
    public static class TcpClientExtensions
    {
        public static Message ReceiveMessage(this TcpClient client)
        {
            var buff = new byte[4096];
            var totalBytesRead = 0;
            var messageType = MessageType.None;
            var payloadSize = 0;
            
            // 1-8 header
            // byte 1-4 message type
            // byte 5-8 payload size
            // 9-? message
            // message string bytes
            var allReadBytes = new List<byte>();
            int bytesRead;
            while ((bytesRead = client.GetStream().Read(buff, 0, buff.Length)) > 0)
            {
                allReadBytes.AddRange(buff.Take(bytesRead));
                totalBytesRead += bytesRead;

                if (totalBytesRead >= 4 && messageType == MessageType.None)
                {
                    messageType = (MessageType) BitConverter.ToInt32(allReadBytes.Take(4).ToArray(), 0);
                }
                if (totalBytesRead >= 8 && payloadSize == 0)
                {
                    payloadSize = BitConverter.ToInt32(allReadBytes.Skip(4).Take(4).ToArray(), 0);
                }

                if (totalBytesRead - 8 >= payloadSize) break;
            }
            
            return new Message
            {
                MessageType = messageType,
                Payload = allReadBytes.Skip(8).ToArray()
            };
        }

        public static void SendMessage(this TcpClient client, Message message)
        {
            SendMessage(new[] { client }, message);
        }

        public static void SendMessage(this IEnumerable<TcpClient> clients, Message message)
        {
            var payload = message.Payload ?? new byte[0];
            var messageTypeBytes = BitConverter.GetBytes((int)message.MessageType);
            var payloadSizeBytes = BitConverter.GetBytes(payload.Length);
            var data = new List<byte>();
            data.AddRange(messageTypeBytes);
            data.AddRange(payloadSizeBytes);
            data.AddRange(payload);
            foreach (var socket in clients)
            {
                try
                {
                    socket.GetStream().Write(data.ToArray(), 0, data.Count);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode != SocketError.ConnectionReset) throw;
                    socket.Close();
                }
            }
        }
    }
}
