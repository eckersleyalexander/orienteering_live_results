using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Data;
using System.IO;

namespace Orienteering_LR_Desktop
{
    class OESync
    {
        public string OEEventPath;
        private int SyncTime; // in seconds
        private bool Running;
        private Timer SyncFunc;
        

        public OESync(string path)
        {
            OEEventPath = path;
            SyncTime = 30;
            Running = false;
            SyncFunc = null;
        }

        public OESync(string path, int timer)
        {
            OEEventPath = path;
            SyncTime = timer;
            Running = false;
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

        public void OESyncDataNow()
        {
            DataSet OEdb = ReadOEDb();

            // for each of our tables: read the appropriate data from the DataSet
            // drop the current tables
            // then write all the records to our db

            if (!OEdb.Tables.Contains("Telin"))
            {
                return;
            }

            using (var context = new Database.CompetitorContext())
            {
                //context.Competitors.RemoveRange(context.Competitors);

            }

            // competitor

            // club

            // team

            // comptimes - times incomplete

            // raceclass

            
        }

        public void StartSync()
        {
            // begin syncing on timer
            SyncFunc = new Timer(e => OESyncDataNow(), null, TimeSpan.Zero, TimeSpan.FromSeconds(SyncTime));
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
    }
}
