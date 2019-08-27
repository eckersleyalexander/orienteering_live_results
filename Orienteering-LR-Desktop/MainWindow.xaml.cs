﻿using Microsoft.EntityFrameworkCore.Infrastructure;
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

namespace Orienteering_LR_Desktop
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
    {
        public List<Runner> CompetitorsList = new List<Runner>();
        public List<Control> ControlsList = new List<Control>();
        public List<CourseDesktop> CoursesList = new List<CourseDesktop>();
        public List<Runner> Runners = new List<Runner>();

        private readonly Reader _reader;
        private readonly OESync oeSync;

        public MainWindow()
        {
            InitializeComponent();
            if (File.Exists("testdb.db"))
            {
                File.Delete("testdb.db");
            }
            using (var db = new CompetitorContext())
            {
                db.GetService<IMigrator>().Migrate();
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
            oeSync = new OESync(Directory.GetCurrentDirectory() + "\\test");
            oeSync.StartSync();

            GetInitData();
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
            ((App)Application.Current).socketServer.LeaderboardSocket.SendUpdates();
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
                    Runner runnerRow = Runners[rowIndex];
                    Debug.WriteLine("Row: " + rowIndex + ", column changed: " + bindingPath + ", new value: " + el.Text + ", ID = " + runnerRow.ChipId);
                }
            }
        }
       
        private void GetInitData()
        {
            var db = new Database.Query();
            List<Database.CompetitorInfo> Competitors = db.GetAllCompetitorInfo(1);
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

            ControlsList.Add(new Control()
            {
                Id = 1000001,
                RadioBool = false
            });
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

        private void CommandBinding_CanExecute_1(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CommandBinding_Executed_1(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private void CommandBinding_Executed_2(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MaximizeWindow(this);
        }

        private void CommandBinding_Executed_3(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int index = int.Parse(((Button)e.Source).Uid);

            GridCursor.Margin = new Thickness(10 + (150 * index), 45, 0, 0);

            switch (index)
            {
                case 0:
                    CompGrid.Visibility = Visibility.Visible;
                    ContGrid.Visibility = Visibility.Hidden;
                    ClassGrid.Visibility = Visibility.Hidden;
                    break;
                case 1:
                    CompGrid.Visibility = Visibility.Hidden;
                    ContGrid.Visibility = Visibility.Visible;
                    ClassGrid.Visibility = Visibility.Hidden;
                    break;
                case 2:
                    CompGrid.Visibility = Visibility.Hidden;
                    ContGrid.Visibility = Visibility.Hidden;
                    ClassGrid.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
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
