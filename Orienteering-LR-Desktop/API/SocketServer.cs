using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.WebSockets;


namespace Orienteering_LR_Desktop.API
{
    public class SocketServer
    {
        public List<SocketClient> Clients;
        public LeaderboardSocket LeaderboardSocket;
        public ControlSocket ControlSocket;

        public SocketServer(WebServer server)
        {
            Clients = new List<SocketClient>();
            LeaderboardSocket = new LeaderboardSocket("/leaderboard",this);
            ControlSocket = new ControlSocket("/control", this);

            server.WithModule(LeaderboardSocket)
                .WithModule(ControlSocket);

        }

        private void UpdateSocketClient(string clientId, bool connected)
        {
            bool found = false;
            foreach (var client in Clients)
            {
                if (client.ClientId == clientId)
                {
                    client.Connected = true;
                    found = true;
                }
            }

            if (!found)
            {
                Clients.Add(new SocketClient(clientId, "",  true));
            }
        }
        public async Task OnClientConnected(IWebSocketContext context)
        {
            Debug.WriteLine("client connected");
            UpdateSocketClient(context.Id, true);
        }

        public async Task OnClientDisconnected(IWebSocketContext context)
        {
            Debug.WriteLine("client disconnected");
            UpdateSocketClient(context.Id, false);
        }
    }
}