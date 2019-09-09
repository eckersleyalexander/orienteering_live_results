using System;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.Generic;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Orienteering_LR_Desktop.API;
using Swan;


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
