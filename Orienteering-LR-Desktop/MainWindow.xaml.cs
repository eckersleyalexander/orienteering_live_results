using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
                ID = 00001,
                firstName = "John",
                lastName = "Smith"
            });
            testGrid.ItemsSource = runners;
        }

    }
    public class Runner
    {
        public int ID { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
    }



}
