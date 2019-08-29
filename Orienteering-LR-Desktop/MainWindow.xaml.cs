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
using System.IO;
using Microsoft.WindowsAPICodePack;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.ObjectModel;

namespace Orienteering_LR_Desktop
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
    {
        public ObservableCollection<Runner> CompetitorsList = new ObservableCollection<Runner>();
        public List<Control> ControlsList = new List<Control>();
        public ObservableCollection<CourseDesktop> CoursesList = new ObservableCollection<CourseDesktop>();
        private readonly Reader _reader;
        private OESync oeSync;

        public MainWindow()
        {
            InitializeComponent();

            if (File.Exists("LRDB.db"))
            {
                File.Delete("LRDB.db");
            }
            using (var db = new CompetitorContext())
            {
                db.GetService<IMigrator>().Migrate();
            }

            if (Properties.Settings.Default.OEPath != "")
            {
                OESync testSync = new OESync(Properties.Settings.Default.OEPath);
                testSync.StartSync();
                if (testSync.SyncSuccess)
                {
                    OEPathLabel.Content = Properties.Settings.Default.OEPath;
                    oeSync = testSync;
                    testSync.StopSync();
                    GetInitData();
                } 
            }
            
            CompetitorsTable.ItemsSource = CompetitorsList;
            ControlsTable.ItemsSource = ControlsList;

            // radio punch receiver
            _reader = new Reader
            {
                WriteBackupFile = false,
                WriteLogFile = false
            };
            _reader.OnlineStampRead += _reader_OnlineStampRead;
            _reader.OutputDevice = new ReaderDeviceInfo(ReaderDeviceType.None);
            _reader.OpenOutputDevice();

            // this should be in the setup process -> need to choose the oe directory
            // currently using pwd\test

        }

        private async void _reader_OnlineStampRead(object sender, SportidentDataEventArgs e)
        {
            // Siid = chipId
            int chipId = (int)e.PunchData[0].SiidValue;
            // CodeNumber = checkpointId
            int checkpointId = (int)e.PunchData[0].CodeNumber;
            // TimeSi = punch time w/ correct date
            // PunchDateTime = punch time w/ date as 1/1/2000
            int punchTime = (int)((e.PunchData[0].PunchDateTime - new DateTime(2000, 1, 1)).TotalSeconds * 1000.0);
            //MessageBox.Show("ChipId: " + chipId.ToString() + ", CheckpointId: " + chipId.ToString() + ", Punch: " + punchTime.ToString());

            // save to db
            var s = new Database.Store();
            s.CreatePunch(chipId, checkpointId, punchTime);

            // push to front end
            await ((App)Application.Current).socketServer.LeaderboardSocket.SendUpdates();
        }
       
        private void GetInitData()
        {
            var db = new Database.Query();
            List<Database.CompetitorInfo> Competitors = db.GetAllCompetitorInfo(1);
            List<Database.CourseInfo> Courses = db.GetAllCourseInfo();
            
            foreach (Database.CompetitorInfo c in Competitors)
            {
                CompetitorsList.Add(new Runner()
                {
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    ChipId = c.ChipId,
                    Status = c.Status
                });
            }

            foreach (Database.CourseInfo c in Courses)
            {
                foreach (int controlID in c.CourseData)
                {
                    if (!ControlsList.Any(x => x.Id == controlID)) {
                        ControlsList.Add(new Control()
                        {
                            Id = controlID,
                            RadioBool = false
                        });
                    }  
                }
            }

            ControlsList.Sort((x, y) => x.Id.CompareTo(y.Id));

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

        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            int index = int.Parse(((Button)e.Source).Uid);

            GridCursor.Margin = new Thickness(10 + (150 * index), 45, 0, 0);

            switch (index)
            {
                case 0:
                    CompGrid.Visibility = Visibility.Visible;
                    ContGrid.Visibility = Visibility.Hidden;
                    ClassGrid.Visibility = Visibility.Hidden;
                    SettingsGrid.Visibility = Visibility.Hidden;
                    break;
                case 1:
                    CompGrid.Visibility = Visibility.Hidden;
                    ContGrid.Visibility = Visibility.Visible;
                    ClassGrid.Visibility = Visibility.Hidden;
                    SettingsGrid.Visibility = Visibility.Hidden;
                    break;
                case 2:
                    CompGrid.Visibility = Visibility.Hidden;
                    ContGrid.Visibility = Visibility.Hidden;
                    ClassGrid.Visibility = Visibility.Visible;
                    SettingsGrid.Visibility = Visibility.Hidden;
                    break;
                case 3:
                    CompGrid.Visibility = Visibility.Hidden;
                    ContGrid.Visibility = Visibility.Hidden;
                    ClassGrid.Visibility = Visibility.Hidden;
                    SettingsGrid.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void SetOEPathButton(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = Properties.Settings.Default.OEPath == "" ? "C:\\" : Properties.Settings.Default.OEPath;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                OESync testSync = new OESync(dialog.FileName);
                testSync.StartSync();
                if (testSync.SyncSuccess)
                {
                    OEPathLabel.Content = dialog.FileName;
                    oeSync = testSync;
                    testSync.StopSync();
                    GetInitData();
                    Properties.Settings.Default.OEPath = dialog.FileName;
                    Properties.Settings.Default.Save();
                }
                else
                {
                    MessageBox.Show("No/Incomplete OE Data at specified location. Please try a different folder.");
                }
            }
        }

        private void DemoButtonClick(object sender, RoutedEventArgs e)
        {
            if (DemoGrid.Visibility == Visibility.Hidden)
            {
                oeSync.StopSync();
                CompetitorsList = new ObservableCollection<Runner>();
                ControlsList = new List<Control>();
                if (File.Exists("LRDB.db"))
                {
                    File.Delete("LRDB.db");
                }
                using (var db = new CompetitorContext())
                {
                    db.GetService<IMigrator>().Migrate();
                }
                var dbstore = new Database.Store();
                DemoGrid.Visibility = Visibility.Visible;
                CompetitorsList.Add(new Runner() {
                    ChipId = 101,
                    Status = "Not Started",
                    FirstName = "John",
                    LastName = "Smith"
            
                });
                CompetitorsList.Add(new Runner()
                {
                    ChipId = 102,
                    Status = "Not Started",
                    FirstName = "Bob",
                    LastName = "Brown"

                });
                CompetitorsTable.ItemsSource = CompetitorsList;
                ControlsList.Add(new Control()
                {
                    Id = 1,
                    RadioBool = false
                });
                ControlsList.Add(new Control()
                {
                    Id = 2,
                    RadioBool = false
                });
                ControlsList.Add(new Control()
                {
                    Id = 3,
                    RadioBool = false
                });
                ControlsTable.ItemsSource = ControlsList;

                dbstore.CreateClub(new Club()
                {
                    ClubId = 1,
                    Name = "Club One"
                });
                dbstore.CreateRaceClass(new RaceClass()
                {
                    RaceClassId = 1,
                    Name = "Test Class"
                });
                foreach (Runner r in CompetitorsList)
                {
                    dbstore.CreateCompetitor(new Competitor()
                    {
                        CompetitorId = r.ChipId ?? default(int),
                        FirstName = r.FirstName,
                        LastName = r.LastName,
                        Gender = "Male",
                        ClubId = 1,
                        RaceClassId = 1
                    });
                }
            }
        }

        private void DemoPunchClick(object sender, RoutedEventArgs e)
        {
            int index = int.Parse(((Button)e.Source).Uid);
            var s = new Database.Store();
            int now = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            switch (index)
            {
                case 0:
                    s.CreatePunch(101, 0, now);
                    CompetitorsList[0].Status = "Started";  
                    break;
                case 1:
                    s.CreatePunch(101, 1, now);
                    break;
                case 2:
                    s.CreatePunch(101, 2, now);
                    CompetitorsList[0].Status = "Finished";
                    break;
                case 3:
                    s.CreatePunch(102, 0, now);
                    CompetitorsList[1].Status = "Started";
                    break;
                case 4:
                    s.CreatePunch(102, 1, now);
                    break;
                case 5:
                    s.CreatePunch(102, 2, now);
                    CompetitorsList[1].Status = "Finished";
                    break;
            }
            CompetitorsTable.ItemsSource = new ObservableCollection<Runner>();
            CompetitorsTable.ItemsSource = CompetitorsList;
            ((App)Application.Current).socketServer.LeaderboardSocket.SendUpdates();
        }
    }

    public class Runner
    {
        public int? ChipId { get; set; }
        public string Status { get; set;  }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class Control
    {
        public int Id { get; set; }
        public Boolean RadioBool { get; set; }
    }

    public class CourseDesktop
    {
        public List<Control> Controls { get; set; }
        public String Name { get; set; }
    }


}
