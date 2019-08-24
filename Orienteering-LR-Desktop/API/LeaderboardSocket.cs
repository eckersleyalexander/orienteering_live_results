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
            var q = new Database.Query();
            var CompPunches = q.GetCompetitorPunches();
            var things = new List<BoardDemoClass>();
            foreach (var c in CompPunches)
            {
                var temp = new BoardDemoClass {FirstName = c.FirstName, LastName = c.LastName};

                var timez = new List<int>();
                if (c.Punches.Count > 0)
                {
                    c.Punches.Sort((a, b) => a.Timestamp - b.Timestamp);
                    var startTime = c.Punches[0].Timestamp;
                    foreach (var p in c.Punches)
                    {
                        p.Timestamp -= startTime;
                        timez.Add(p.Timestamp);
                    }
                }
                temp.Times = timez;
                things.Add(temp);
            }
            things.Sort();
            var jsoned = JsonConvert.SerializeObject(things);
            BroadcastAsync(jsoned);
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