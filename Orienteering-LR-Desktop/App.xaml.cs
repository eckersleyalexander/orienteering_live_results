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
			//server.RegisterModule(new StaticFilesModule(Directory.GetCurrentDirectory() + "/vue_app"));
			//server.Module<StaticFilesModule>().UseRamCache = true;
			//server.Module<StaticFilesModule>().DefaultExtension = ".html";
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
                foreach (Database.Punch p in c.Punches)
                {
                    timez.Add(p.Timestamp);
                }
                temp.Times = timez;
                things.Add(temp);
            }
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

        }

        
    }
    public class BoardDemoClass
    {
        public String FirstName { get; set; }
        public String LastName { get; set; }
        public List<int> Times { get; set; }
    }
}
