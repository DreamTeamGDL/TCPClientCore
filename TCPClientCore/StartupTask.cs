using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using Windows.Devices.Gpio;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using Windows.Networking;
using Windows.Networking.Sockets;

using TCPClientCore.Services;
using TCPClientCore.Services.Interfaces;
using System.IO;

namespace TCPClientCore
{
    public sealed class StartupTask : IBackgroundTask
    {
        BackgroundTaskDeferral _deferral;
        DatagramSocket socket;
        ISerializer _serializer;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();

            _serializer = new Serializer();

            socket = new DatagramSocket();
            socket.MessageReceived += MessageReceived;
            socket.BindServiceNameAsync("25500");

            /*
            var negotiateTask = Negotiate();
            negotiateTask.Wait();

            var connectTask = GetStream(negotiateTask.Result ?? throw new Exception());
            connectTask.Wait();

            var mainTask = Listen(connectTask.Result ?? throw new Exception());
            */
        }

        private void MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {

        }

        private async Task Configure(NetworkStream stream)
        {
            var message = new ActionMessage
            {
                Action = ACTION.CONFIGURE,
                Do = "",
                Name = ""
            };

            var json = _serializer.Serialize(message);
            await stream.WriteAsync(json, 0, json.Length);

            var buffer = new byte[254];
            var received = await stream.ReadAsync(buffer, 0, buffer.Length);
            var actionMessage = _serializer.Deserialize(buffer) as ActionMessage;

            var config = _serializer.Deserialize(new byte[4]);
        }

        private async Task Listen(NetworkStream stream)
        {
            var bytes = new byte[256];

            var message = new ActionMessage
            {
                Name = "Room 1",
                Do = "",
                Action = ACTION.CONNECT
            };

            var json = _serializer.Serialize(message);
            await stream.WriteAsync(json, 0, json.Length);

            int i = 0;
            while ((i = await stream.ReadAsync(bytes, 0, bytes.Length)) != 0)
            {
                var receivedMessage = _serializer.Deserialize(bytes) as ActionMessage;

                var buffer = _serializer.Serialize(new { Data = "Done" });

                await stream.WriteAsync(buffer, 0, buffer.Length);
            }
        }

        private async Task Negotiate()
        {
            var message = new ActionMessage
            {
                Action = ACTION.HELLO,
                Do = "",
                Name = ""
            };

            var udpSocket = new DatagramSocket();
            udpSocket.MessageReceived += UdpSocket_MessageReceived;

            var hostName = new HostName(IPAddress.Broadcast.ToString());

            var output = (await udpSocket.GetOutputStreamAsync(hostName, "25500")).AsStreamForWrite();
            var json = JsonConvert.SerializeObject(message);

            var writter = new StreamWriter(output);
            await writter.WriteLineAsync(json);
            await writter.FlushAsync();
        }

        private async Task ConnectToTcp(string ip, string port)
        {
            var message = new ActionMessage
            {
                Action = ACTION.CONFIGURE,
                Do = "",
                Name = ""
            };

            var socket = new StreamSocket();
            var serverHost = new HostName(ip);

            await socket.ConnectAsync(serverHost, port);

            var streamOut = socket.OutputStream.AsStreamForWrite();
            var streamWritter = new StreamWriter(streamOut);
            await streamWritter.WriteLineAsync(JsonConvert.SerializeObject(message));
            await streamWritter.FlushAsync();

            var streamIn = socket.InputStream.AsStreamForRead();
            var reader = new StreamReader(streamIn);
            var json = await reader.ReadLineAsync();

            message = JsonConvert.DeserializeObject<ActionMessage>(json);
        }

        private async void UdpSocket_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            var streamIn = args.GetDataStream().AsStreamForRead();
            var reader = new StreamReader(streamIn);
            var message = JsonConvert.DeserializeObject<ActionMessage>(await reader.ReadLineAsync()); 

            if(message.Action == ACTION.CONNECT)
            {
                var task = ConnectToTcp(message.Name, message.Do);
            }
        }

        private async Task<NetworkStream> GetStream(IPEndPoint endPoint)
        {
            var tcpClient = new TcpClient();
            try
            {
                await tcpClient.ConnectAsync(endPoint.Address, endPoint.Port);
                var controller = GpioController.GetDefault();

                if (tcpClient.Connected)
                {
                    var pin = controller.OpenPin(21);
                    pin.SetDriveMode(GpioPinDriveMode.Output);
                    pin.Write(GpioPinValue.High);

                    return tcpClient.GetStream();
                }
                else
                {
                    var pin = controller.OpenPin(120);
                    pin.SetDriveMode(GpioPinDriveMode.Output);
                    pin.Write(GpioPinValue.High);

                    return null;
                }
            }
            catch(SocketException ex)
            {
                throw ex;
            }
        }
    }

    enum ACTION
    {
        CONNECT,
        TELL,
        HELLO,
        CONFIGURE
    }

    class ActionMessage
    {
        public ACTION Action { get; set; }
        public string Name { get; set; }
        public string Do { get; set; }
    }
}
