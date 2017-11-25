using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Newtonsoft.Json;
using Windows.Networking;
using Windows.Storage.Streams;
using System.Net;

using TCPClientCore.Services;

namespace TCPClientCore.LLClients
{
    public sealed class UdpServer
    {
        private DatagramSocket datagramSocket;
        private Serializer _serializer;

        private static ActionMessage ResponseMessage = new ActionMessage
        {
            Action = ACTION.CONNECT,
            Do = "25000",
            Name = Dns.GetHostName()
        };

        public UdpServer()
        {
            _serializer = new Serializer();
            datagramSocket = new DatagramSocket();
            datagramSocket.MessageReceived += DatagramSocket_MessageReceived;
        }

        private async void DatagramSocket_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            uint length = args.GetDataReader().UnconsumedBufferLength;
            var receivedMessage = args.GetDataReader().ReadString(length);

            var obj = JsonConvert.DeserializeObject<ActionMessage>(receivedMessage);
            if(obj.Action == ACTION.HELLO)
            {
                await RespondMessage(args.RemoteAddress, args.RemotePort);
            }
        }

        public async void Start()
        {
            await datagramSocket.BindServiceNameAsync("25500").AsTask();
        }

        private async Task RespondMessage(HostName remote, string port)
        {
            var socket = new DatagramSocket();
            using (var stream = await socket.GetOutputStreamAsync(remote, port))
            {
                using (var writer = new DataWriter(stream))
                {
                    var json = JsonConvert.SerializeObject(ResponseMessage);
                    var bytes = Encoding.UTF8.GetBytes(json);

                    writer.WriteBytes(bytes);

                    await writer.StoreAsync();
                }
            }
        }
    }
}
