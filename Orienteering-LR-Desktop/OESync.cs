using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.IO;

namespace Orienteering_LR_Desktop
{
    class OESync
    {
        public string OEEventPath;
        private int SyncTime;
        private bool Running;

        public OESync(string path)
        {
            OEEventPath = path;
            SyncTime = 30;
            Running = false;
        }

        public OESync(string path, int timer)
        {
            OEEventPath = path;
            SyncTime = timer;
            Running = false;
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
            // then write to our db record by record
        }

        public void StartSync()
        {
            // begin syncing on timer
        }

        public void StopSync()
        {
            // stop syncing
        }
    }
}
