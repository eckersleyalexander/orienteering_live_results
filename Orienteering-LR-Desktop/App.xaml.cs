using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Constants;
using Unosquare.Labs.EmbedIO.Modules;
using RoutingStrategy = Unosquare.Labs.EmbedIO.Constants.RoutingStrategy;

namespace Orienteering_LR_Desktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            var server =  new WebServer("http://localhost:9696/", RoutingStrategy.Regex);

            server.RegisterModule(new WebApiModule());
            server.Module<WebApiModule>().RegisterController<ApiController>();
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
                // This is fake call to a Repository
                // var person = await PeopleRepository.GetById(id);
                return await this.JsonResponseAsync("{\"message\":\"Hello, World!\"}");
            }
            catch (Exception ex)
            {
                return await this.JsonExceptionResponseAsync(ex);
            }
        }
    
        // You can override the default headers and add custom headers to each API Response.
        public override void SetDefaultHeaders() => this.NoCache();
    }
}
