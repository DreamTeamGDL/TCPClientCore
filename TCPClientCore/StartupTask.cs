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
using System.IO;

using TCPClientCore.Services;
using TCPClientCore.Services.Interfaces;
using TCPClientCore.LLClients;
using TCPClientCore.SkynetClient;

namespace TCPClientCore
{
    public sealed class StartupTask : IBackgroundTask
    {
        BackgroundTaskDeferral _deferral;
        ISerializer _serializer;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();

            var client = new SkynetAPI();

            var udpServer = new UdpServer();
            var udpClientTask = Task.Run(() => udpServer.Start());
            
            /*
            var tcpServer = new TcpServer(client);
            var tcpClientTask = Task.Run(() => tcpServer.Start());
            */

            _serializer = new Serializer();
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
