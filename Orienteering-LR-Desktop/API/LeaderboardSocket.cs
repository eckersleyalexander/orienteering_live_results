using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using EmbedIO.WebSockets;
using Newtonsoft.Json;
using Remotion.Linq.Utilities;


namespace Orienteering_LR_Desktop.API
{
     public class LeaderboardSocket : WebSocketModule
     {
         private SocketServer _server;

         public LeaderboardSocket(string urlPath, SocketServer server) : base(urlPath, true)
         {
             _server = server;
         }

         public async Task SendUpdates()
        {
            await BroadcastAsync(GetLeaderboard.GetAllClassesJson());
        }

        protected override Task OnClientConnectedAsync(IWebSocketContext context)
        {
            return Task.WhenAll(_server.OnClientConnected(context));
        }

        protected override Task OnClientDisconnectedAsync(IWebSocketContext context)
        {
            return Task.WhenAll(_server.OnClientDisconnected(context));
        }


         protected override Task OnMessageReceivedAsync(IWebSocketContext context, byte[] buffer, IWebSocketReceiveResult result)
         {
             return SendUpdates();
         }
     }
}