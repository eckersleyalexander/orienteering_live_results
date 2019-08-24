using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Windows.Media.Converters;

namespace Orienteering_LR_Desktop.API
{
    public class SocketClient
    {
        public string ClientId { get; set; }
        public string SocketId { get; set; }
        public bool Connected { get; set; }

        public SocketClient(string clientId, string socketId, bool connected)
        {
            ClientId = clientId;
            SocketId = socketId;
            Connected = connected;
        }
    }
}