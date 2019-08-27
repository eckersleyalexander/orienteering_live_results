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
        public class CommandMsg
        {
            public string Command;
            public string Body;
        }

        public class SetScreenCommand
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
            try
            {
                CommandMsg command = JsonConvert.DeserializeObject<CommandMsg>(System.Text.Encoding.Default.GetString(buffer));
                string reply;

                switch (command.Command)
                {
                    case "SetScreen":
                        SetScreenCommand body = JsonConvert.DeserializeObject<SetScreenCommand>(command.Body);
                        reply = "Screen " + body.ScreenId + " set to classes: ";
                        List<string> added = new List<string>();
                        foreach (Database.RaceClass raceClass in new Database.Query().GetClasses())
                        {
                            if (body.RaceClassIds.Contains(raceClass.RaceClassId))
                            {
                                added.Add(raceClass.Name);
                            }
                        }
                        reply += string.Join(",", added);
                        break;
                    case "GetClasses":
                        reply = JsonConvert.SerializeObject(new Database.Query().GetAllClassInfo());
                        break;
                    case "GetScreens":
                        reply = "GetScreens";
                        break;
                    default:
                        reply = "Unrecognized Command";
                        break;
                }
                SendAsync(context, reply);
            }
            catch (JsonReaderException)
            {
                SendAsync(context, "Invalid Message Format");
            }
            return Task.WhenAll();
        }
    }
}