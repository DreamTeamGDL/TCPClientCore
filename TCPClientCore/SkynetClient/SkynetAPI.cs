using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using TCPClientCore.Models.Configs;
using Windows.Foundation;

namespace TCPClientCore.SkynetClient
{
    public sealed class SkynetAPI
    {
        private HttpClient _client;
        private string ZoneID { get; set; }

        public SkynetAPI()
        {
            _client = new HttpClient()
            {
                BaseAddress = new Uri("http://skynetgdl.azurewebsites.net/")
            };
        }

        public IAsyncOperation<ClientConfig> GetConfig(string macAddress)
        {
            return Task.FromResult(new ClientConfig
            {
                ClientName = "Rasp",
                PinMap =
                {
                    { "Pin 3", 14 }
                }
            }).AsAsyncOperation();
        }

        public IAsyncOperation<MainConfig> GetMainConfig(string macAddress) => throw new NotImplementedException();
    }
}
