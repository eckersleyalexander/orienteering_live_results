using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmbedIO.WebSockets;
using Newtonsoft.Json;


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
             return HandleMessage(context, buffer);
         }
         
         protected async Task HandleMessage(IWebSocketContext context, byte[] buffer)
         {
             try
             {
                 string data = Encoding.GetString(buffer);
                 var deserialised = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(data);

                 string action = deserialised["action"];
                 string uuid = deserialised["uuid"];

                 switch (action)
                 {
                     case "register":
                         string endpoint = context.RequestUri.LocalPath.Trim('/');
                         await _server.OnClientRegister(context, uuid, endpoint);
                         break;
                   
                    
                     default:
                         await SendAsync(context, _server.MakeErrorResponse(" unknown action: " + action));
                         break;
                 }
             }
             catch (Exception e)
             {
                 await SendAsync(context, _server.MakeErrorResponse(e.Message));
             }
         }
         
         public async Task SendToLeaderboards(IWebSocketContext context, string message)
         {
             var leaderboardClients = _server.Clients
                 .Where(c => c.ClientType == "leaderboard").Select(c => c.SocketId);
             await BroadcastAsync(message, c => leaderboardClients.Contains(c.Id));
         }
         
         public async Task SendToClient(IWebSocketContext context, string uuid, string message)
         {
             var leaderboardClients = _server.Clients
                 .Where(c => c.ClientType == "leaderboard");
             foreach (var client in leaderboardClients)
             {
                 if (client.ClientId != uuid) continue;
                 await BroadcastAsync(message, c => c.Id == client.SocketId);
                 return;
             }
         }
     }
}