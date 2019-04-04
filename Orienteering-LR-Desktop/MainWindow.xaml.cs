using System.Collections.Generic;
using System.Windows;

namespace Orienteering_LR_Desktop
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
    {
        public List<Runner> runners = new List<Runner>();

        public MainWindow()
        {
            InitializeComponent();
            runners.Add(new Runner()
            {
                id = "1",
                firstName = "John",
                lastName = "Smith"
            });
            testGrid.ItemsSource = runners;
        }

    }
    public class Runner
    {
        public string id { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
    }



}
