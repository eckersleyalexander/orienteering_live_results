using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Orienteering_LR_Desktop.Database;
using SPORTident;
using SPORTident.Communication.UsbDevice;
using System.IO;
using Microsoft.WindowsAPICodePack;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using EmbedIO;
using EmbedIO.Files;
using Orienteering_LR_Desktop.API;
using EmbedIO.WebApi;
using System.Windows.Media;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Data;
using System.ComponentModel;

namespace Orienteering_LR_Desktop
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
    {
        public ObservableCollection<Runner> CompetitorsList = new ObservableCollection<Runner>();
        public ObservableCollection<Control> ControlsList = new ObservableCollection<Control>();
        public ObservableCollection<CourseDesktop> CoursesList = new ObservableCollection<CourseDesktop>();
        private readonly Reader _reader;
        private OESync oeSync;
        public WebServer server;
        public SocketServer socketServer;
        public Debugger debugger;

        public MainWindow()
        {
            InitializeComponent();
            debugger = new Debugger(DebuggerList);

            using (var db = new CompetitorContext())
            {
                db.GetService<IMigrator>().Migrate();
            }

            if (Properties.Settings.Default.OEPath != "")
            {
                OESync testSync = new OESync(Properties.Settings.Default.OEPath);
                if (oeSync != null)
                {
                    oeSync.StopSync();
                }
                testSync.StartSync();
                if (testSync.SyncSuccess)
                {
                    OEPathLabel.Content = Properties.Settings.Default.OEPath;
                    oeSync = testSync;
                    //GetInitData();
                }  else
                {
                    testSync.StopSync();
                }
            }
            String strHostName = string.Empty;
            strHostName = Dns.GetHostName();
            IPHostEntry ipEntry = Dns.GetHostEntry(strHostName);
            IPAddress[] AddrList = ipEntry.AddressList.Where(a => a.AddressFamily == AddressFamily.InterNetwork).ToArray();

            IPChoiceBox.ItemsSource = AddrList;
            CompetitorsTable.ItemsSource = CompetitorsList;
            CoursesTable.ItemsSource = CoursesList;
            ControlsTable.ItemsSource = ControlsList;
            CollectionViewSource.GetDefaultView(ControlsTable.ItemsSource).SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));

            // radio punch receiver
            _reader = new Reader
            {
                WriteBackupFile = false,
                WriteLogFile = false
            };
            _reader.OnlineStampRead += _reader_OnlineStampRead;
            _reader.OutputDevice = new ReaderDeviceInfo(ReaderDeviceType.None);
            _reader.OpenOutputDevice();
            ConnectRadio();

            // this should be in the setup process -> need to choose the oe directory
            // currently using pwd\test
        }

        private void ControlsTable_CellMouseUp(object sender, MouseButtonEventArgs e)
        {
            ControlsTable.CommitEdit();
        }

        private void StartWebServer(String WebAddr)
        {
            socketServer = new SocketServer("/socket");
            FileModule ldrbrdserver = new FileModule("/", new FileSystemProvider(Directory.GetCurrentDirectory() + "/vue_app/", false));
            FileModule ldrbrdmobserver = new FileModule("/mobile", new FileSystemProvider(Directory.GetCurrentDirectory() + "/vue_app_mobile/", false));

            server = new WebServer(o => o
                    .WithUrlPrefix("http://+:9696")
                    .WithMode(HttpListenerMode.EmbedIO)
                )
                .WithCors()
                .WithWebApi("/api", api => api.WithController<LeaderboardAPI>())
                .WithModule(socketServer)
                .WithModule(ldrbrdmobserver)
                .WithModule(ldrbrdserver);
                
            server.RunAsync();
            Process.Start("http://localhost:9696/");
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

            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                var mainWindow = (MainWindow)Application.Current.MainWindow;
                mainWindow.debugger.Write(chipId + "," + checkpointId + "," + punchTime);
            }));

            // push to front end
            //await socketServer.SendLeaderboardUpdates();
        }
       
        public void GetInitData()
        {
            var db = new Database.Query();
            CompetitorsList.Clear();
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
            CoursesList.Clear();
            List<int> controls = new List<int>();
            foreach (Database.CourseInfo c in Courses)
            {
                CourseDesktop cd = new CourseDesktop();
                cd.Name = c.Description;
                cd.Controls = string.Join(", ", c.CourseData);
                CoursesList.Add(cd);
                foreach (int controlID in c.CourseData)
                {
                    controls.Add(controlID);
                    if (!ControlsList.Any(x => x.Id == controlID) && controlID != OESync.FINISH_CHECKPOINT && controlID != OESync.START_CHECKPOINT)
                    {
                        ControlsList.Add(new Control()
                        {
                            Id = controlID,
                            RadioBool = false
                        });
                    }  
                }
            }

            List<Control> del = new List<Control>();
            foreach (Control ctr in ControlsList)
            {
                if (!controls.Contains(ctr.Id))
                {
                    del.Add(ctr);
                }
            }

            foreach (Control ctr in del)
            {
                ControlsList.Remove(ctr);
            }

            ControlsList.OrderBy(x => x.Id);
        }

        private void ConnectRadio()
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
                    debugger.Write("radio connected");
                }
                catch (Exception ex)
                {
                    if (_reader.InputDeviceIsOpen) _reader.CloseInputDevice();
                    debugger.Write(ex.Message);
                }
            }
        }

        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            int index = int.Parse(((Button)e.Source).Uid);

            GridCursor.Margin = new Thickness(12 + (165 * index), 45, 0, 0);

            switch (index)
            {
                case 0:
                    CompGrid.Visibility = Visibility.Visible;
                    ContGrid.Visibility = Visibility.Hidden;
                    CoursesGrid.Visibility = Visibility.Hidden;
                    SettingsGrid.Visibility = Visibility.Hidden;
                    break;
                case 1:
                    CompGrid.Visibility = Visibility.Hidden;
                    ContGrid.Visibility = Visibility.Visible;
                    CoursesGrid.Visibility = Visibility.Hidden;
                    SettingsGrid.Visibility = Visibility.Hidden;
                    break;
                case 2:
                    CompGrid.Visibility = Visibility.Hidden;
                    ContGrid.Visibility = Visibility.Hidden;
                    CoursesGrid.Visibility = Visibility.Visible;
                    SettingsGrid.Visibility = Visibility.Hidden;
                    break;
                case 3:
                    CompGrid.Visibility = Visibility.Hidden;
                    ContGrid.Visibility = Visibility.Hidden;
                    CoursesGrid.Visibility = Visibility.Hidden;
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
                if (oeSync != null)
                {
                    oeSync.StopSync();
                }
                testSync.StartSync();
                if (testSync.SyncSuccess)
                {
                    OEPathLabel.Content = dialog.FileName;
                    oeSync = testSync;
                    //GetInitData();
                    Properties.Settings.Default.OEPath = dialog.FileName;
                    Properties.Settings.Default.Save();
                }
                else
                {
                    testSync.StopSync();
                    if (oeSync != null)
                    {
                        oeSync.StartSync();
                    }
                    MessageBox.Show("No/Incomplete OE Data at specified location. Please try a different folder.");
                }
            }
        }

        private void StartWebServerClick(object sender, RoutedEventArgs e)
        {
            Button btn = (Button) sender;
            btn.Background = (Brush) new BrushConverter().ConvertFromString("Red");
            btn.Content = "Restart Web Server";
            StartWebServer("http://" + IPChoiceBox.SelectedValue + ":9696/");
        }

        private void Debug_Btn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            if (DebugGrid.Visibility == Visibility.Visible)
            {
                btn.Content = "Show Debugging";
                DebugGrid.Visibility = Visibility.Hidden;
            } else
            {
                btn.Content = "Hide Debugging";
                DebugGrid.Visibility = Visibility.Visible;
            }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Demobtn(object sender, RoutedEventArgs e)
        {
            using (var context = new Database.CompetitorContext())
            {
                context.Punches.RemoveRange(context.Punches);
                context.SaveChanges();
            }

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
        public String Name { get; set; }
        public String Controls { get; set; }
    }

    public class Debugger
    {
        ListBox lstbox;

        public Debugger(ListBox lstbox)
        {
            this.lstbox = lstbox;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] public void Write (String msg)
        {
            string str = "[" + DateTime.Now.ToString("hh:mm:ss tt") + "] " + new StackFrame(1).GetMethod().Name + "() says: " + msg;
            lstbox.Items.Add(str);
            using (System.IO.StreamWriter file = new System.IO.StreamWriter("Debug.txt", true))
            {
                file.WriteLine(str);
            }
        }

        public void DeleteLog() {
            if (File.Exists("Debug.txt"))
            {
                File.Delete("Debug.txt");
            }
        }

        public void ArchiveLog()
        {
            if (File.Exists("Debug.txt"))
            {
                File.Copy("Debug.txt", "Log:" + DateTime.Now.ToString("dd-MM-yyyy") + "/" + DateTime.Now.ToString("hh:mm:ss tt"));
                File.Delete("Debug.txt");
            }

        }   
    }

}
