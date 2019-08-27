using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Data;
using EmbedIO.WebSockets;
using Newtonsoft.Json;

namespace Orienteering_LR_Desktop.API
{
       
    public class ControlSocket : WebSocketModule
    {
        public class SetScreenClassesMsg
        {
            public string ScreenId;
            public List<int> RaceClassIds;
        }

        private SocketServer _server;

        public ControlSocket(string urlPath, SocketServer server) : base(urlPath, true)
        {
            _server = server;
        }

        protected override Task OnClientConnectedAsync(IWebSocketContext context)
        {
            return Task.WhenAll();//_server.OnClientConnected(context));
        }

        protected override Task OnClientDisconnectedAsync(IWebSocketContext context)
        {
            return Task.WhenAll();//_server.OnClientDisconnected(context));
        }

        protected override Task OnMessageReceivedAsync(IWebSocketContext context, byte[] buffer, IWebSocketReceiveResult result)
        {
            SetScreenClassesMsg command = JsonConvert.DeserializeObject<SetScreenClassesMsg>(System.Text.Encoding.Default.GetString(buffer));
            return Task.WhenAll();
        }
    }
}