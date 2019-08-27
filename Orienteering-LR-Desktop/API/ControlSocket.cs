using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Animation;
using EmbedIO.WebSockets;
using Newtonsoft.Json;
using Swan.Formatters;


namespace Orienteering_LR_Desktop.API
{
       
    public class ControlSocket : WebSocketModule
    {

        private SocketServer _server;

        public ControlSocket(string urlPath, SocketServer server) : base(urlPath, true)
        {
            _server = server;
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

                        await _server.OnClientRegister(context, uuid);
                        break;
                    
                    case "get clients":
                        var uuids = _server.Clients.Select(c => c.ClientId).ToList();
                        var serialised = JsonConvert.SerializeObject(uuids);
                        await SendAsync(context, serialised);
                        break;

                    default:
                        await SendAsync(context, MakeErrorResponse(" unknown action: " + action));
                        break;
                }
            }
            catch (Exception e)
            {
                await SendAsync(context, MakeErrorResponse(e.Message));
            }
        }

        private string MakeErrorResponse(string message)
        {
            Dictionary<string, dynamic> errorResponse = new Dictionary<string, dynamic>
            {
                {"action", "error"},
                {"uuid", null},
                {"payload", new Dictionary<string, string>
                {
                    {"message",message}
                }}
            };
            return JsonConvert.SerializeObject(errorResponse);
        }

    }
}