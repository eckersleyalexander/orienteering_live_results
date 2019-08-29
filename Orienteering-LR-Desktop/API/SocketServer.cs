using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using EmbedIO.WebSockets;
using Newtonsoft.Json;
using Orienteering_LR_Desktop.Database;


namespace Orienteering_LR_Desktop.API
{
    public class SocketServer : WebSocketModule
    {
        public List<SocketClient> Clients;

        public SocketServer(string urlPath) : base(urlPath, true)
        {
            Clients = new List<SocketClient>();
        }

        private async Task UpdateSocketClient(string clientId, string socketId, string nameSpace, bool connected)
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

                    if (nameSpace != null)
                    {
                        client.ClientType = nameSpace;
                    }
                }
            }

            if (!connected)
            {
                Clients.RemoveAll(e => e.SocketId == socketId);
            }

            if (!found && connected)
            {
                Clients.Add(new SocketClient(clientId, socketId, nameSpace));
            }

            if (found)
            {
                await SendToControlPanels(GetClientsResponse(null));
            }
        }

        protected override Task OnClientConnectedAsync(IWebSocketContext context)
        {
            return UpdateSocketClient(null, context.Id, null, true);
        }

        protected override Task OnClientDisconnectedAsync(IWebSocketContext context)
        {
            return UpdateSocketClient(null, context.Id, null, false);
        }


        public async Task OnClientRegister(IWebSocketContext context, string uuid, string nameSpace)
        {
            Debug.WriteLine(uuid + " (" + nameSpace + ") registered");
            await UpdateSocketClient(uuid, context.Id, nameSpace, true);
        }

        public async Task SendUpdates()
        {
            await BroadcastAsync(GetLeaderboard.GetAllClassesJson());
        }

        public string MakeActionResponse(string nameSpace, string action, string uuid, string payload)
        {
            Dictionary<string, dynamic> actionResponse = new Dictionary<string, dynamic>
            {
                {"namespace", nameSpace},
                {"action", action},
                {"uuid", uuid},
                {"payload", payload}
            };
            return JsonConvert.SerializeObject(actionResponse);
        }

        public string MakeErrorResponse(string nameSpace, string message)
        {
            Dictionary<string, dynamic> errorResponse = new Dictionary<string, dynamic>
            {
                {"namespace", nameSpace},
                {"action", "error"},
                {"uuid", null},
                {
                    "payload", new Dictionary<string, string>
                    {
                        {"message", message}
                    }
                }
            };
            return JsonConvert.SerializeObject(errorResponse);
        }

        protected override Task OnMessageReceivedAsync(IWebSocketContext context, byte[] buffer,
            IWebSocketReceiveResult result)
        {
            return HandleMessage(context, buffer);
        }

        protected async Task HandleMessage(IWebSocketContext context, byte[] buffer)
        {
            string nameSpace, action, uuid;
            dynamic payload;
            try
            {
                string data = Encoding.GetString(buffer);
                var deserialised = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(data);

                nameSpace = deserialised["namespace"];
                action = deserialised["action"];
                uuid = deserialised["uuid"];
                deserialised.TryGetValue("payload", out payload);
            }
            catch (Exception e)
            {
                await SendAsync(context, MakeErrorResponse("control", e.Message));
                Debug.WriteLine(e);
                return;
            }

            Query query = new Query();

            switch (action)
            {
                case "register":
                    await OnClientRegister(context, uuid, nameSpace);
                    break;

                case "clients":
                    await SendAsync(context, GetClientsResponse(uuid));
                    break;

                case "classes":
                    await SendAsync(context,
                        MakeActionResponse("control", "classes", null,
                            JsonConvert.SerializeObject(query.GetClasses())));
                    break;

                case "updateLeaderboard":
                    await SendToClient(context, "leaderboard", uuid,
                        MakeActionResponse("leaderboard", "updateLeaderboard", uuid,
                            payload));
                    break;

                default:
                    await SendAsync(context, MakeErrorResponse("control", " unknown action: " + action));
                    break;
            }
        }

        public string GetClientsResponse(string uuid)
        {
            var clients = Clients
                .Where(c => c.ClientId != null)
                .Select(c => new Dictionary<string, string>() {{"id", c.ClientId}, {"type", c.ClientType}})
                .ToList();
            var serialised = JsonConvert.SerializeObject(clients);
            return MakeActionResponse("control", "clients", uuid, serialised);
        }

        public async Task SendToClient(IWebSocketContext context, string clientType, string uuid, string message)
        {
            var controlClients = Clients
                .Where(c => c.ClientType == clientType);
            foreach (var client in controlClients)
            {
                if (client.ClientId != uuid) continue;
                await BroadcastAsync(message, c => c.Id == client.SocketId);
                return;
            }
        }

        public async Task SendToLeaderboards(IWebSocketContext context, string message)
        {
            var leaderboardClients = Clients
                .Where(c => c.ClientType == "leaderboard").Select(c => c.SocketId);
            await BroadcastAsync(message, c => leaderboardClients.Contains(c.Id));
        }

        public async Task SendToControlPanels(string message)
        {
            var controlClients = Clients
                .Where(c => c.ClientType == "control").Select(c => c.SocketId);
            await BroadcastAsync(message, c => controlClients.Contains(c.Id));
        }
    }
}