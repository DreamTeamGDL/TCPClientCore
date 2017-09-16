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

using TCPClientCore.Services;
using TCPClientCore.Services.Interfaces;

namespace TCPClientCore
{
    public sealed class StartupTask : IBackgroundTask
    {
        BackgroundTaskDeferral _deferral;
        ISerializer _serializer;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();

            _serializer = new Serializer();

            var negotiateTask = Negotiate();
            negotiateTask.Wait();

            var connectTask = GetStream(negotiateTask.Result ?? throw new Exception());
            connectTask.Wait();

            var mainTask = Listen(connectTask.Result ?? throw new Exception());
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

        private async Task<IPEndPoint> Negotiate()
        {
            var client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            await client.ConnectAsync(new IPEndPoint(IPAddress.Broadcast, 25500));

            var groupEP = new IPEndPoint(IPAddress.Any, 0);
            bool done = false;

            var message = new ActionMessage
            {
                Action = ACTION.HELLO,
                Do = "",
                Name = ""
            };

            var serializedMessage = _serializer.Serialize(message);
            
            var size = await client.SendToAsync(serializedMessage, SocketFlags.Broadcast, new IPEndPoint(IPAddress.Broadcast, 25500));

            IPEndPoint endPoint = null;

            try
            {
                while (!done)
                {
                    var bytes = await client.ReceiveAsync();
                    var obj = _serializer.Deserialize(bytes.Buffer) as ActionMessage;
                    done = true;
                    
                    if(IPAddress.TryParse(obj.Name, out var address))
                    {
                        endPoint = new IPEndPoint(address, Convert.ToInt32(obj.Do));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return endPoint;
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
