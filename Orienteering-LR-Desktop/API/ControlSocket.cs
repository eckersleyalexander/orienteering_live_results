using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmbedIO.WebSockets;
using Newtonsoft.Json;


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
                        string endpoint = context.RequestUri.LocalPath.Trim('/');
                        await _server.OnClientRegister(context, uuid, endpoint);
                        break;
                    
                    case "clients":
                        await SendAsync(context, GetClientsResponse(uuid));
                        break;
                    
                    case "broadcast":
                        await BroadcastAsync("{\"action\":\"broadcast\"}", c => c != context);
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

        public string GetClientsResponse(string uuid)
        {
            var clients = _server.Clients
                .Where(c => c.ClientId != null)
                .Select(c => new Dictionary<string, string>(){{"id",c.ClientId},{"type", c.ClientType}})
                .ToList();
            var serialised = JsonConvert.SerializeObject(clients);
            return _server.MakeActionResponse("clients", uuid, serialised);
        }

        public async Task SendToControlPanels(IWebSocketContext context, string message)
        {
            var controlClients = _server.Clients
                .Where(c => c.ClientType == "control").Select(c => c.SocketId);
            await BroadcastAsync(message, c => controlClients.Contains(c.Id));
        }

        

    }
}