using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPClientCore.Models.Configs
{
    public sealed class ClientConfig
    {
        public string ClientName { get; set; }
        public IDictionary<string, int> PinMap { get; set; } = new Dictionary<string, int>();
    }
}
