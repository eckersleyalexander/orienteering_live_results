using ConsoleApp.NewDb;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            testGrid.CellEditEnding += Datagrid_CellEditEnding;

            runners.Add(new Runner()
            {
                id = "1",
                firstName = "John",
                lastName = "Smith"
            });
            testGrid.ItemsSource = runners;
            
            using (var db = new BloggingContext())
            {
                db.Blogs.Add(new Blog { Url = "http://blogs.msdn.com/adonet" });
                var count = db.SaveChanges();
                Console.WriteLine("{0} records saved to database", count);

                Console.WriteLine();
                Console.WriteLine("All blogs in database:");
                foreach (var blog in db.Blogs)
                {
                    Console.WriteLine(" - {0}", blog.Url);
                }
            }
        }

        void Datagrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var column = e.Column as DataGridBoundColumn;
                if (column != null)
                {
                    var bindingPath = (column.Binding as Binding).Path.Path;
                    var row = e.Row;
                    var el = e.EditingElement as TextBox;
                    int rowIndex = row.GetIndex();
                    Runner runnerRow = runners[rowIndex];
                    Debug.WriteLine("Row: " + rowIndex + ", column changed: " + bindingPath + ", new value: " + el.Text + ", ID = " + runnerRow.id);
                }
            }
        }

    }
    public class Runner
    {
        public string id { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
    }



}
