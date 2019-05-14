using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
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
using Orienteering_LR_Desktop.Database;
using SPORTident;
using SPORTident.Communication.UsbDevice;
using Unosquare.Labs.EmbedIO.Modules;

namespace Orienteering_LR_Desktop
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
    {

        public List<Runner> runners = new List<Runner>();
        private readonly Reader _reader;

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
            using (var db = new CompetitorContext())
            {
                db.GetService<IMigrator>().Migrate();                
            }

            _reader = new Reader
            {
                WriteBackupFile = false,
                WriteLogFile = false
            };

            _reader.OnlineStampRead += _reader_OnlineStampRead;

            _reader.OutputDevice = new ReaderDeviceInfo(ReaderDeviceType.None);
            _reader.OpenOutputDevice();
            
            /*
            using (var db = new BloggingContext())
            {

                db.GetService<IMigrator>().Migrate();

                db.Blogs.Add(new Blog { Url = "http://blogs.msdn.com/adonet" });
               
                var count = db.SaveChanges();
                Debug.WriteLine("{0} records saved to database", count);

                Debug.WriteLine("");
                Debug.WriteLine("All blogs in database:");
                foreach (var blog in db.Blogs)
                {
                    Debug.WriteLine(" - {0}", blog.Url);
                }
            }
            */
        }

        private void _reader_OnlineStampRead(object sender, SportidentDataEventArgs e)
        {
            // Siid = chipId
            int chipId = (int)e.PunchData[0].SiidValue;
            // CodeNumber = checkpointId
            int checkpointId = (int)e.PunchData[0].CodeNumber;
            // TimeSi = punch time w/ correct date
            // PunchDateTime = punch time w/ date as 1/1/2000
            int punchTime = (int)((e.PunchData[0].PunchDateTime - new DateTime(2000, 1, 1)).TotalSeconds * 100.0);
            //MessageBox.Show("ChipId: " + chipId.ToString() + ", CheckpointId: " + chipId.ToString() + ", Punch: " + punchTime.ToString());

            // save to db
            var s = new Database.Store();
            s.CreatePunch(chipId, checkpointId, punchTime);

            // push to front end
            ((App)Application.Current).leaderboard.SendUpdates();
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

        private void ConnectRadio(object sender, RoutedEventArgs e)
        {
            List<DeviceInfo> devList = DeviceInfo.GetAvailableDeviceList(true, (int)DeviceType.Serial);
            if (devList.Count != 1)
            {
                MessageBox.Show("No devices detected (or more than 1)");
            }
            else
            {
                ReaderDeviceInfo device = new ReaderDeviceInfo(devList[0], ReaderDeviceType.SiDevice);
                try
                {
                    if (_reader.InputDeviceIsOpen) _reader.CloseInputDevice();
                    _reader.InputDevice = device;
                    _reader.OpenInputDevice();
                    MessageBox.Show("radio connected");
                }
                catch (Exception ex)
                {
                    if (_reader.InputDeviceIsOpen) _reader.CloseInputDevice();
                    MessageBox.Show(ex.Message);
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
