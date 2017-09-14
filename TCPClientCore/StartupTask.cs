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

namespace TCPClientCore
{
    public sealed class StartupTask : IBackgroundTask
    {
        BackgroundTaskDeferral _deferral;
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();

            var mainTask = GetStream().ContinueWith(async (task) =>
            {
                var stream = task.Result;

                string done = "Done";
                var bytes = new byte[256];
                string data = null;

                var json = JsonConvert.SerializeObject(new AccionMessage
                {
                    Name = "Room 1",
                    Do = "",
                    Action = "Connect"
                });

                var firstMessage = Encoding.ASCII.GetBytes(json);
                await stream.WriteAsync(firstMessage, 0, firstMessage.Length);

                int i = 0;
                while ((i = await stream.ReadAsync(bytes, 0, bytes.Length)) != 0)
                {
                    data = Encoding.ASCII.GetString(bytes);
                    var buffer = Encoding.ASCII.GetBytes(done);

                    await stream.WriteAsync(buffer, 0, buffer.Length);
                }
            });
        }
        
        private async Task<NetworkStream> GetStream()
        {
            var tcpClient = new TcpClient();
            try
            {
                await tcpClient.ConnectAsync("192.168.1.71", 25000);
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

    class AccionMessage
    {
        public string Action { get; set; }
        public string Name { get; set; }
        public string Do { get; set; }
    }
}
