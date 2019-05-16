using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Constants;
using Unosquare.Labs.EmbedIO.Modules;
using RoutingStrategy = Unosquare.Labs.EmbedIO.Constants.RoutingStrategy;
using System.Collections.Generic;
using Orienteering_LR_Desktop.Database;
using Newtonsoft.Json;
using System.Globalization;

namespace Orienteering_LR_Desktop
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
    {
        public WebServer server;
        public LeaderboardSocket leaderboard;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            server =  new WebServer("http://localhost:9696/", RoutingStrategy.Regex);
            leaderboard = new LeaderboardSocket();

            server.RegisterModule(new WebApiModule());
            server.Module<WebApiModule>().RegisterController<ApiController>();
			server.RegisterModule(new StaticFilesModule(Directory.GetCurrentDirectory() + "/vue_app"));
			server.Module<StaticFilesModule>().UseRamCache = true;
			server.Module<StaticFilesModule>().DefaultExtension = ".html";
            server.RegisterModule(new WebSocketsModule());
            server.Module<WebSocketsModule>().RegisterWebSocketsServer<LeaderboardSocket>("/leaderboard", leaderboard);
            server.RunAsync();
        }
	}

    public class ApiController: WebApiController 
    {
        public ApiController(IHttpContext context) : base(context)
        {
        }
        [WebApiHandler(HttpVerbs.Get, "/api/hello/")]
        public async Task<bool> SayHello(int id)
        {
            try
            {
                var text = "";
                var text2 = "";
                var text3 = "";
                // 
                Application.Current.Dispatcher.Invoke((Action)(() =>
               {
                   text = ((MainWindow)Application.Current.MainWindow).runners[0].id;
                   text2 = ((MainWindow)Application.Current.MainWindow).runners[0].firstName;
                   text3 = ((MainWindow)Application.Current.MainWindow).runners[0].lastName;
                   
               }));
                // This is fake call to a Repository
                // var person = await PeopleRepository.GetById(id);
                return await this.JsonResponseAsync("{\"ID\": \"" + text + "\", \"firstName\": \"" + text2 + "\", \"lastName\": \"" + text3 + "\"}");
            }
            catch (Exception ex)
            {
                return await this.JsonExceptionResponseAsync(ex);
            }
        }
    
        // You can override the default headers and add custom headers to each API Response.
        public override void SetDefaultHeaders() => this.NoCache();
    }

    public class LeaderboardSocket : WebSocketsServer
    {
        public LeaderboardSocket()
            : base(true)
        {
            // placeholder
        }

        public override string ServerName => "Leaderboard Socket";
        //public List<String> clients;

        protected override void OnClientConnected(
            IWebSocketContext context,
            System.Net.IPEndPoint localEndPoint,
            System.Net.IPEndPoint remoteEndPoint)
        {
            Console.WriteLine("connected");
            SendUpdates();


            // Test string I was using
            //   Broadcast("[{\"FirstName\": \"Bob\",\"LastName\": \"Johnson\",\"Times\": [0, 17387, 24325]},{\"FirstName\": \"Bill\",\"LastName\": \"Ginseng\",\"Times\": [0, 15210, 25210]},{\"FirstName\": \"Janice\",\"LastName\": \"Thyme\",\"Times\": [0, 12101, 27435]},{\"FirstName\": \"Gary\",\"LastName\": \"Jam\",\"Times\": []}]");

        }

        public void SendUpdates()
        {
            var q = new Database.Query();
            List<Database.CompetitorPunches> CompPunches = q.GetCompetitorPunches();
            List<BoardDemoClass> things = new List<BoardDemoClass>();
            foreach (Database.CompetitorPunches c in CompPunches)
            {
                BoardDemoClass temp = new BoardDemoClass();
                temp.FirstName = c.FirstName;
                temp.LastName = c.LastName;

                List<int> timez = new List<int>();
                if (c.Punches.Count > 0)
                {
                    c.Punches.Sort((a, b) => a.Timestamp - b.Timestamp);
                    int startTime = c.Punches[0].Timestamp;
                    foreach (Database.Punch p in c.Punches)
                    {
                        p.Timestamp -= startTime;
                        timez.Add(p.Timestamp);
                    }
                }
                temp.Times = timez;
                things.Add(temp);
            }
            things.Sort();
            String jsoned = JsonConvert.SerializeObject(things);
            Broadcast(jsoned);
        }
       
        protected override void OnClientDisconnected(IWebSocketContext context)
        {
            Console.WriteLine("disconnected");
        }

        protected override void OnFrameReceived(IWebSocketContext context, byte[] rxBuffer, IWebSocketReceiveResult rxResult)
        {
            // placeholder
        }

        protected override void OnMessageReceived(IWebSocketContext context, byte[] rxBuffer, IWebSocketReceiveResult rxResult)
        {
            SendUpdates();
        }


    }
    public class BoardDemoClass : IComparable<BoardDemoClass>
    {
        public String FirstName { get; set; }
        public String LastName { get; set; }
        public List<int> Times { get; set; }

        public int CompareTo(BoardDemoClass obj)
        {
            if (Times.Count == obj.Times.Count && Times.Count > 0)
            {
                return Times[Times.Count - 1] - obj.Times[obj.Times.Count - 1];
            }
            else
            {
                return obj.Times.Count - Times.Count;
            }
        }
    }
}
