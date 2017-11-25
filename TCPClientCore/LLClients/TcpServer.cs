using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Newtonsoft.Json;
using System.Collections.Concurrent;

using TCPClientCore.SkynetClient;

namespace TCPClientCore.LLClients
{
    public sealed class TcpServer
    {
        private StreamSocketListener socketStreamListener;
        ConcurrentDictionary<string, IOutputStream> Clients = new ConcurrentDictionary<string, IOutputStream>();
        ConcurrentDictionary<string, string> NameMacCorrelation = new ConcurrentDictionary<string, string>();
        SkynetAPI _skynetAPI;

        public TcpServer(SkynetAPI skynetAPI)
        {
            _skynetAPI = skynetAPI;
            socketStreamListener = new StreamSocketListener();
            socketStreamListener.ConnectionReceived += SocketStreamListener_ConnectionReceived;
        }

        private async void SocketStreamListener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {

            var buffer = new Windows.Storage.Streams.Buffer(256);
            using (var stream = args.Socket.InputStream)
            {
                var read = await stream.ReadAsync(buffer, buffer.Capacity, InputStreamOptions.None);
                var reader = DataReader.FromBuffer(read);

                var message = JsonConvert.DeserializeObject<ActionMessage>(reader.ReadString(read.Length));
                await ProcessMessage(message, args.Socket);
            }

            GC.Collect();
        }

        public async void Start()
        {
            await socketStreamListener.BindEndpointAsync(null, "25000");
        }

        private Task ProcessMessage(ActionMessage message, StreamSocket socket)
        {
            switch (message.Action)
            {
                case ACTION.CONNECT:
                    break;
                case ACTION.TELL:
                    break;
                case ACTION.HELLO:
                    break;
                case ACTION.CONFIGURE:
                    return Config(message.Name, socket);
                default:
                    break;
            }

            return Task.CompletedTask;
        }

        private async Task Config(string macAddress, StreamSocket socket)
        {
            if(Clients.TryAdd(macAddress, socket.OutputStream))
            {
                var config = await _skynetAPI.GetConfig(macAddress);
                NameMacCorrelation.TryAdd(config.ClientName, macAddress);

                var json = JsonConvert.SerializeObject(config);
                var dataWriter = new DataWriter(socket.OutputStream);
                dataWriter.WriteString(json);
                var success = await dataWriter.FlushAsync();
            }
        }
    }
}
