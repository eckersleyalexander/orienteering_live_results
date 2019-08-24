using System;
using System.Threading.Tasks;
using System.Windows;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Swan;

namespace Orienteering_LR_Desktop.API
{
    public class LeaderboardAPI: WebApiController
    {
        [Route(HttpVerbs.Get, "/api/hello/")]
        public async Task<string> SayHello(int id)
        {
            try
            {
                var text = 0;
                var text2 = "";
                var text3 = "";
                // 
                Application.Current.Dispatcher.Invoke((Action)(() =>
                {
                    text = ((MainWindow)Application.Current.MainWindow).Runners[0].Id;
                    text2 = ((MainWindow)Application.Current.MainWindow).Runners[0].FirstName;
                    text3 = ((MainWindow)Application.Current.MainWindow).Runners[0].LastName;
                   
                }));
                // This is fake call to a Repository
                // var person = await PeopleRepository.GetById(id);
                return "{\"ID\": \"" + text + "\", \"firstName\": \"" + text2 + "\", \"lastName\": \"" + text3 + "\"}";
            }
            catch (Exception ex)
            {
                return ex.ToJson();
            }
        }
    }
}