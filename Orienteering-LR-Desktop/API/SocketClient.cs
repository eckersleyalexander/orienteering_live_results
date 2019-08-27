using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Windows.Media.Converters;

namespace Orienteering_LR_Desktop.API
{
    public class SocketClient
    {
        public string ClientId { get; set; }
        public string SocketId { get; set; }
        public string ClientType { get; set; }

        public SocketClient(string clientId, string socketId, string clientType)
        {
            ClientId = clientId;
            SocketId = socketId;
            ClientType = clientType;
        }
    }
}