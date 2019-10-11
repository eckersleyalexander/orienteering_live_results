using System;
using System.Threading.Tasks;
using System.Windows;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Newtonsoft.Json;
using Orienteering_LR_Desktop.Database;
using Swan;

namespace Orienteering_LR_Desktop.API
{
    public class LeaderboardAPI: WebApiController
    {
        [Route(HttpVerbs.Get, "/leaderboard")]
        public async Task<string> GetLeaderboard(int id)
        {
            Query q = new Query();
            var compData = q.GetCompData();

            return JsonConvert.SerializeObject(compData);
        }
    }
}