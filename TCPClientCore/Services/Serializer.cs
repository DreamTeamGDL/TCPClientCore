using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TCPClientCore.Services.Interfaces;

namespace TCPClientCore.Services
{
    sealed class Serializer : ISerializer
    {
        public object Deserialize(byte[] buffer)
        {
            var str = Encoding.UTF8.GetString(buffer);
            return JsonConvert.DeserializeObject<object>(str);
        }
        public byte[] Serialize(object obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            return Encoding.UTF8.GetBytes(json);
        }
    }
}
