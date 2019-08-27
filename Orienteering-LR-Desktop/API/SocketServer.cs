using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Remoting.Lifetime;
using System.Threading.Tasks;
using System.Windows.Input;
using EmbedIO;
using EmbedIO.WebSockets;
using Microsoft.EntityFrameworkCore.Diagnostics;


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

        private async Task UpdateSocketClient(string clientId, string socketId, bool connected)
        {
            bool found = false;
            foreach (var client in Clients)
            {
                if (client.ClientId == clientId || client.SocketId == socketId)
                {
                    found = true;
                    client.SocketId = socketId;
                    if (clientId != null)
                    {
                        client.ClientId = clientId;
                    }

                }
            }

            if (!connected)
            {
                Clients.RemoveAll(e => e.SocketId == socketId);
            }

            if (!found && connected)
            {
                Clients.Add(new SocketClient(clientId, socketId));
            }
            
        }
        public async Task OnClientConnected(IWebSocketContext context)
        {
            await UpdateSocketClient(null,context.Id, true);
        }

        public async Task OnClientDisconnected(IWebSocketContext context)
        {
            await UpdateSocketClient(null,context.Id, false);
        }


        public async Task OnClientRegister(IWebSocketContext context, string uuid)
        {
            await UpdateSocketClient(uuid, context.Id, true);
        }
    }
}