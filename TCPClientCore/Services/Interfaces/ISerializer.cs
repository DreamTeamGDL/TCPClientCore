using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace TCPClientCore.Services.Interfaces
{
    public interface ISerializer
    {
        object Deserialize([ReadOnlyArray]byte[] buffer);

        byte[] Serialize(object obj);
    }
}
