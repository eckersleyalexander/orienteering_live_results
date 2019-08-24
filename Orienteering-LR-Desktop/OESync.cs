using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Data;
using System.IO;
using Newtonsoft.Json;

namespace Orienteering_LR_Desktop
{
    class OESync
    {
        public string OEEventPath;
        private int SyncTime; // in seconds
        private Timer SyncFunc;
        private static readonly string[] ExpectedTables = { "Teiln.dat", "Kat.dat", "Club.dat", "Chip1.dat", "Bahnen1.dat" };

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
                    if (row.Field<Nullable<int>>("StartCodeNr") != null)
                    {
                        checkpoints.Add(row.Field<int>("StartCodeNr"));
                    }

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
                    if (row.Field<Nullable<int>>("ZielStr") != null)
                    {
                        checkpoints.Add(row.Field<int>("ZielStr"));
                    }

                    Database.Course newCourse = new Database.Course
                    {
                        CourseId = row.Field<int>("BahnNr"),
                        Distance = (Nullable<float>)row.Field<Nullable<double>>("BahnKm"),
                        Climb = (Nullable<float>)row.Field<Nullable<int>>("BahnHm"),
                        Description = row.Field<string>("Bez"),
                        CourseData = JsonConvert.SerializeObject(checkpoints),
                        DistanceData = null
                    };

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
                    if (newCourse.DistanceData != null && JsonConvert.DeserializeObject<List<float>>(newCourse.DistanceData).Count != checkpoints.Count - 1)
                    {
                        newCourse.DistanceData = null;
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
                        Stage = "1",
                        RaceClassId = row.Field<int>("KatNr"),
                        CourseId = row.Field<Nullable<int>>("BahnNr1")
                    };
                    
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

                // add competitors
                context.Competitors.RemoveRange(context.Competitors);
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

                    comp.ChipId = row.Field<Nullable<int>>("ChipNr1");

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
                }

                context.SaveChanges();

                // comptimes

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
