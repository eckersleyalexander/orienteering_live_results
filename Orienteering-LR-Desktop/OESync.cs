using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Data;
using System.IO;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using System.Windows;

namespace Orienteering_LR_Desktop
{

    class OESync
    {
        private class ChipPunch
        {
            public int? checkpoint;
            public int? timestamp;
        }

        public Boolean SyncSuccess;
        public string OEEventPath;
        private int SyncTime; // in seconds
        private Timer SyncFunc;
        private static readonly string[] ExpectedTables = { "Teiln.dat", "Kat.dat", "Club.dat", "Chip1.dat", "Bahnen1.dat" };

        public const int START_CHECKPOINT = 12;
        public const int FINISH_CHECKPOINT = 13;

        public OESync(string path)
        {
            OEEventPath = path;
            SyncTime = 30;
            SyncFunc = null;
        }

        public OESync(string path, int timer)
        {
            OEEventPath = path;
            SyncTime = timer;
            SyncFunc = null;
        }

        private DataSet ReadOEDb()
        {
            string[] files = Directory.GetFiles(OEEventPath, "*.dat");
            DataSet db = new DataSet();

            foreach (string tableNamePath in files)
            {
                db.Tables.Add(DbisamRead.ReadTable(OEEventPath, Path.GetFileName(tableNamePath)));
            }

            return db;
        }

        public bool OESyncDataNow()
        {
            DataSet OEdb = ReadOEDb();
            SyncSuccess = false;

            // for each of our tables: read the appropriate data from the DataSet
            // drop the current tables
            // then write all the records to our db

            foreach (string tableName in ExpectedTables)
            {
                if(OEdb.Tables[tableName] == null)
                {
                    // throw error or otherwise notify main window?
                    return false;
                }
            }

            using (var context = new Database.CompetitorContext())
            {
                // stages
                context.Stages.RemoveRange(context.Stages);
                context.Stages.Add(new Database.Stage
                {
                    StageId = 1,
                    Current = true
                });

                // drop current tables
                context.Courses.RemoveRange(context.Courses);
                context.RaceClasses.RemoveRange(context.RaceClasses);
                context.Clubs.RemoveRange(context.Clubs);

                //courses
                foreach (DataRow row in OEdb.Tables["Bahnen1.dat"].Rows)
                {
                    // collect checkpoints for this course
                    List<int> checkpoints = new List<int>();
                    
                    // start checkpoint
                    checkpoints.Add(START_CHECKPOINT);

                    // course checkpoints
                    for (int i = 1; i <= 64; i++)
                    {
                        Nullable<int> c = row.Field<Nullable<int>>("CodeNr" + i);
                        if (c == null)
                        {
                            break;
                        }
                        else
                        {
                            checkpoints.Add((int)c);
                        }
                    }

                    // finish checkpoint
                    checkpoints.Add(FINISH_CHECKPOINT);

                    Database.Course newCourse = new Database.Course
                    {
                        CourseId = row.Field<int>("BahnNr"),
                        Distance = (Nullable<float>)row.Field<Nullable<double>>("BahnKm"),
                        Climb = (Nullable<float>)row.Field<Nullable<int>>("BahnHm"),
                        Description = row.Field<string>("Bez"),
                        CourseData = JsonConvert.SerializeObject(checkpoints),
                        DistanceData = null
                    };

                    List<int> distData = new List<int>();
                    for (int i = 0; i < checkpoints.Count; i++)
                    {
                        distData.Add(i);
                    }

                    // try to preserve distance data if it already exists
                    try
                    {
                        newCourse.DistanceData = context.Courses.Single(a => a.CourseId == newCourse.CourseId).DistanceData;
                    }
                    catch (InvalidOperationException)
                    {
                        newCourse.DistanceData = null;
                    }

                    // ensure distance data agrees with checkpoints
                    if (newCourse.DistanceData == null || JsonConvert.DeserializeObject<List<float>>(newCourse.DistanceData).Count != checkpoints.Count)
                    {
                        newCourse.DistanceData = JsonConvert.SerializeObject(distData);
                    }

                    context.Courses.Add(newCourse);
                }

                // add clubs
                foreach (DataRow row in OEdb.Tables["Club.dat"].Rows)
                {
                    Database.Club newClub = new Database.Club
                    {
                        ClubId = row.Field<int>("ClubNr"),
                        Name = row.Field<string>("Ort")
                    };
                    context.Clubs.Add(newClub);
                }

                // add classes
                foreach (DataRow row in OEdb.Tables["Kat.dat"].Rows)
                {
                    Database.RaceClass newClass = new Database.RaceClass
                    {
                        RaceClassId = row.Field<int>("KatNr"),
                        Abbreviation = row.Field<string>("KatKurz"),
                        Name = row.Field<string>("KatLang"),
                        AgeFrom = row.Field<Nullable<int>>("AlterVon"),
                        AgeTo = row.Field<Nullable<int>>("AlterBis"),
                        Gender = row.Field<Nullable<bool>>("Maennlich") == null ? null : (row.Field<bool>("Maennlich") ? "Male" : "Female"),
                        _RaceClassTypeValue = row.Field<Nullable<int>>("KatTyp2") == null ? 0 : row.Field<int>("KatTyp2")
                    };
                    context.RaceClasses.Add(newClass);
                }

                context.SaveChanges();

                // classcourse
                context.ClassCourses.RemoveRange(context.ClassCourses);
                foreach (DataRow row in OEdb.Tables["Kat.dat"].Rows)
                {
                    Database.ClassCourse cc = new Database.ClassCourse
                    {
                        CompetitionPos = 1,
                        Stage = 1,
                        RaceClassId = row.Field<int>("KatNr"),
                        CourseId = row.Field<Nullable<int>>("BahnNr1"),
                        StartTime = row.Field<Nullable<int>>("StartZ11")
                    };

                    if (cc.StartTime < 0 || cc.StartTime > 360000000)
                    {
                        cc.StartTime = null;
                    }

                    // get it into milliseconds
                    cc.StartTime *= 10;
                    
                    try
                    {
                        cc.RaceClass = context.RaceClasses.Single(a => a.RaceClassId == cc.RaceClassId);
                    }
                    catch (InvalidOperationException)
                    {
                        cc.RaceClass = null;
                    }

                    if (cc.CourseId != null)
                    {
                        try
                        {
                            cc.Course = context.Courses.Single(a => a.CourseId == cc.CourseId);
                        }
                        catch (InvalidOperationException)
                        {
                            cc.Course = null;
                        }
                    }

                    context.ClassCourses.Add(cc);
                }

                context.SaveChanges();

                // add competitors and times
                context.Competitors.RemoveRange(context.Competitors);
                context.CompTimes.RemoveRange(context.CompTimes);
                foreach (DataRow row in OEdb.Tables["Teiln.dat"].Rows)
                {
                    Database.Competitor comp = new Database.Competitor();

                    comp.CompetitorId = row.Field<int>("IdNr");
                    comp.FirstName = row.Field<string>("Vorname");
                    comp.LastName = row.Field<string>("Name");
                    comp.Age = row.Field<Nullable<int>>("Jgg");
                    comp.StartNo = row.Field<Nullable<int>>("StartNr");

                    if (row.Field<Nullable<bool>>("Maennlich") == true)
                    {
                        comp.Gender = "Male";
                    }
                    else if (row.Field<Nullable<bool>>("Maennlich") == false)
                    {
                        comp.Gender = "Female";
                    }
                    else
                    {
                        comp.Gender = null;
                    }

                    comp.ClubId = row.Field<Nullable<int>>("ClubNr");
                    if (comp.ClubId == null)
                    {
                        comp.Club = null;
                    }
                    else
                    {
                        try
                        {
                            comp.Club = context.Clubs.Single(a => a.ClubId == comp.ClubId);
                        }
                        catch (InvalidOperationException)
                        {
                            comp.Club = null;
                        }
                    }

                    comp.RaceClassId = row.Field<Nullable<int>>("KatNr");
                    if (comp.RaceClassId == null)
                    {
                        comp.RaceClass = null;
                    }
                    else
                    {
                        try
                        {
                            comp.RaceClass = context.RaceClasses.Single(a => a.RaceClassId == comp.RaceClassId);
                        }
                        catch (InvalidOperationException)
                        {
                            comp.RaceClass = null;
                        }
                    }

                    context.Competitors.Add(comp);

                    // comptimes
                    if (row.Field<Nullable<int>>("ChipNr1") != null)
                    {
                        Database.CompTime compTime = new Database.CompTime
                        {
                            ChipId = row.Field<int>("ChipNr1"),
                            CompetitorId = comp.CompetitorId,
                            Competitor = comp,
                            Stage = 1,
                            Status = row.Field<int?>("NCKen1"),
                            StartTime = row.Field<int?>("Start1")
                        };

                        compTime.StartTime = compTime.StartTime > 0 && compTime.StartTime < 360000000 ? compTime.StartTime : null;
                        compTime.StartTime *= 10;

                        List<int?> times = new List<int?>();

                        // get list of checkpoints that this person should be running
                        Database.ClassCourse cc = context.ClassCourses.Include(b => b.Course).SingleOrDefault(a => a.RaceClassId == comp.RaceClassId && a.Stage == compTime.Stage);
                        List<int> checkpoints = JsonConvert.DeserializeObject<List<int>>(cc?.Course?.CourseData ?? "[]");

                        if (checkpoints.Count > 0)
                        {
                            if (compTime.Status == 0)
                            {
                                // get punch entries from OE
                                List<ChipPunch> chipTimes = new List<ChipPunch>();
                                // search for this person
                                DataRow[] chip = OEdb.Tables["Chip1.dat"].Select("ChipNr = " + compTime.ChipId + " AND IdNr = " + comp.CompetitorId);

                                // if we found a matching entry
                                if (chip.Length == 1)
                                {
                                    // start punch
                                    int? checkpoint = chip[0].Field<int?>("ChipStartP");
                                    int? timestamp = chip[0].Field<int?>("ChipStart");
                                    chipTimes.Add(new ChipPunch()
                                    {
                                        checkpoint = checkpoint,
                                        timestamp = timestamp > 0 && timestamp < 360000000 ? timestamp : null
                                    });

                                    // checkpoint punches
                                    for (int i = 1; i <= 64; i++)
                                    {
                                        checkpoint = chip[0].Field<int?>("ChipStempelP" + i);
                                        timestamp = chip[0].Field<int?>("ChipStempel" + i);
                                        if (checkpoint != null)
                                        {
                                            chipTimes.Add(new ChipPunch()
                                            {
                                                checkpoint = checkpoint,
                                                timestamp = timestamp > 0 && timestamp < 360000000 ? timestamp : null
                                            });
                                        }
                                    }

                                    // finish punch
                                    checkpoint = chip[0].Field<int?>("ChipZielP");
                                    timestamp = chip[0].Field<int?>("ChipZiel");
                                    chipTimes.Add(new ChipPunch()
                                    {
                                        checkpoint = checkpoint,
                                        timestamp = timestamp > 0 && timestamp < 360000000 ? timestamp : null
                                    });

                                    // if list matches punches then easy
                                    if (MatchingCheckpoints(checkpoints, chipTimes))
                                    {
                                        // just copy over times
                                        foreach (ChipPunch punch in chipTimes)
                                        {
                                            times.Add(punch.timestamp);
                                        }
                                    }
                                    else
                                    {
                                        // otherwise more flexibly look for matching punches
                                        // always take start punch without checking checkpoint
                                        if (chipTimes.Count > 0)
                                        {
                                            times.Add(chipTimes[0].timestamp);
                                        }
                                        else
                                        {
                                            times.Add(null);
                                        }

                                        // search for matching punch for each course checkpoint (not start or end)
                                        for (int i = 1; i < checkpoints.Count - 1; i++)
                                        {
                                            // look for first punch that matches
                                            ChipPunch match = chipTimes.FirstOrDefault(a => a.checkpoint == checkpoints[i]);
                                            if (match != null)
                                            {
                                                // if it exists then add it and remove it from list of punches
                                                // remove this way the same punch doesnt get used for multiple checkpoints (for a butterfly course)
                                                times.Add(match.timestamp);
                                                chipTimes.Remove(match);
                                            }
                                            else
                                            {
                                                times.Add(null);
                                            }
                                        }

                                        // always take finish punch without checking
                                        if (chipTimes.Count > 0)
                                        {
                                            times.Add(chipTimes[chipTimes.Count - 1].timestamp);
                                        }
                                        else
                                        {
                                            times.Add(null);
                                        }
                                    }

                                    // if there is no start time, then check if this was a non-punch start
                                    if (times[0] == null && compTime.StartTime != null)
                                    {
                                        times[0] = compTime.StartTime / 10;
                                    }

                                    // if there is a start time
                                    int? min = times[0];
                                    if (min != null)
                                    {
                                        // zero the split times
                                        // and convert from hundreths of seconds to milliseconds
                                        for (int i = 0; i < times.Count; i++)
                                        {
                                            times[i] -= min;
                                            if (times[i] < 0)
                                            {
                                                // compensate for the times rolling-over at the 12h mark
                                                times[i] += 12 * 60 * 60 * 100;
                                            }
                                            times[i] *= 10;
                                        }

                                        // if the final time exists but it didn't get pulled over by the chip scan then add it
                                        if (times[times.Count - 1] == null)
                                        {
                                            int finalTime = row.Field<int?>("Zeit1") ?? 0;
                                            if (finalTime > 0 && finalTime < 360000000)
                                            {
                                                times[times.Count - 1] = finalTime * 10;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // if no start time then discard the data
                                        times.Clear();
                                    }
                                }
                            }

                            if (times.Count == 0)
                            {
                                times.AddRange(Enumerable.Repeat<int?>(null, checkpoints.Count));
                                //times[0] = compTime.StartTime;
                            }
                        }

                        compTime.Times = JsonConvert.SerializeObject(times);
                        context.CompTimes.Add(compTime);
                    }
                }

                context.SaveChanges();

                // tests
                //foreach (Database.RaceClass c in context.RaceClasses.ToList())
                //{
                    //List<GetLeaderboard.LeaderboardCompetitor> leaderboard = GetLeaderboard.ByClass(c.RaceClassId);
                    //string leaderboardJson = GetLeaderboard.ByClassJson(c.RaceClassId);
                //}
            }
            
            SyncSuccess = true;
            return true;
        }

        // check if a list of chip punches correctly matches an ordered list of checkpoints
        private bool MatchingCheckpoints(List<int> checkpoints, List<ChipPunch> punches)
        {
            if (checkpoints.Count != punches.Count)
            {
                return false;
            }
            else
            {
                // ignoring start and finish
                for (int i = 1; i < checkpoints.Count - 1; i++)
                {
                    if (punches[i].checkpoint != checkpoints[i])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public void StartSync()
        {
            // begin syncing on timer
            StopSync();
            if (OESyncDataNow())
            {
                SyncFunc = new Timer(e => OESyncDataNow(), null, TimeSpan.FromSeconds(SyncTime), TimeSpan.FromSeconds(SyncTime));
            }

            //GetInitData()
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                var mainWindow = (MainWindow)Application.Current.MainWindow;
                mainWindow.GetInitData();
            }));
        }

        public void StopSync()
        {
            // stop syncing
            if(SyncFunc != null)
            {
                SyncFunc.Dispose();
                SyncFunc = null;
            }
        }

        public bool Syncing()
        {
            return SyncFunc != null;
        }
    }
}
