using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orienteering_LR_Desktop
{
    class GetLeaderboard
    {
        public class LeaderboardCompetitor : IComparable<LeaderboardCompetitor>
        {
            public int CompetitorId;
            public int ChipNo;
            public string FirstName;
            public string LastName;
            public Nullable<int> Age;
            public Nullable<int> StartNo;
            public string Gender;
            public string Club;
            public string RaceClass;
            public string RaceClassFull;
            public List<Nullable<int>> Times;
            public string Status;

            public LeaderboardCompetitor(Database.CompTime compTime, Database.RaceClass raceClass)
            {
                CompetitorId = compTime.CompetitorId;
                ChipNo = compTime.ChipId;
                FirstName = compTime.Competitor.FirstName;
                LastName = compTime.Competitor.LastName;
                Age = compTime.Competitor.Age;
                StartNo = compTime.Competitor.StartNo;
                Gender = compTime.Competitor.Gender;
                Club = compTime.Competitor.Club == null ? null : compTime.Competitor.Club.Name;
                RaceClass = raceClass.Abbreviation;
                RaceClassFull = raceClass.Name;
                Times = JsonConvert.DeserializeObject<List<Nullable<int>>>(compTime.Times);
                Status = "";
            }

            public int CompareTo(LeaderboardCompetitor o)
            {
                if (o == null)
                {
                    return -1;
                }
                // if this is empty
                if (Times.Count(s => s != null) == 0)
                {
                    // if o is also empty
                    if (o.Times.Count(s => s != null) == 0)
                    {
                        // if this is "Ready"
                        if (Status == "Ready")
                        {
                            // o comes first
                            return 1;
                        }
                        // this is not "Ready"
                        else
                        {
                            // this comes first
                            return -1;
                        }
                    }
                    // this is empty but o is not
                    else
                    {
                        // o comes first
                        return 1;
                    }
                }
                // if this is not empty but o is
                else if (o.Times.Count(s => s != null) == 0)
                {
                    // this comes first
                    return -1;
                }
                // neither are empty
                else
                {
                    // the times at the last scanned checkpoints for this and o
                    int LastTime = (int)Times.Last(a => a != null);
                    int LastTimeo = (int)o.Times.Last(a => a != null);
                    // if they are both at the same checkpoint
                    if (Times.LastIndexOf(LastTime) == o.Times.LastIndexOf(LastTimeo))
                    {
                        // order by lowest time
                        return LastTime - LastTimeo;
                    }
                    // otherwise
                    else
                    {
                        // order by farthest last checkpoint
                        return o.Times.LastIndexOf(LastTimeo) - Times.LastIndexOf(LastTime);
                    }
                }
            }
        }

        public static string ByClassJson(int raceClassId)
        {
            return JsonConvert.SerializeObject(ByClass(raceClassId));
        }

        public static List<LeaderboardCompetitor> ByClass(int raceClassId)
        {
            List<LeaderboardCompetitor> competitors = new List<LeaderboardCompetitor>();

            Database.CompetitorContext context = new Database.CompetitorContext();
            int currentStage = context.Stages.Single(a => a.Current).StageId;
            Database.RaceClass raceClass;
            Database.ClassCourse classCourse;
            try
            {
                raceClass = context.RaceClasses.Single(a => a.RaceClassId == raceClassId);
                classCourse = context.ClassCourses.Include(a => a.Course).Single(a => a.RaceClassId == raceClassId && a.Stage == currentStage);
            }
            catch (InvalidOperationException)
            {
                // return empty list if raceClass does not exist or if it does not have an associated course
                return competitors;
            }

            List<int> currentCourse = JsonConvert.DeserializeObject<List<int>>(classCourse.Course.CourseData);


            foreach (Database.CompTime compTime in context.CompTimes.Include(a => a.Competitor).ThenInclude(a => a.Club).Where(a => a.Stage == currentStage && a.Competitor.RaceClassId == raceClassId))
            {
                LeaderboardCompetitor compEntry = new LeaderboardCompetitor(compTime, raceClass);

                // if times is empty (i.e. nothing sync'd from OE) then search for times in punch
                if (compEntry.Times.Count == 0)
                {
                    // get times from Punch
                    foreach (int checkpoint in currentCourse)
                    {
                        // if a checkpoint is missing, time will be null
                        Database.Punch punch = context.Punches.SingleOrDefault(a => a.ChipId == compTime.ChipId && a.Stage == currentStage && a.CheckpointId == checkpoint);
                        compEntry.Times.Add(punch == null ? null : (Nullable<int>)punch.Timestamp);
                    }

                    // if no times
                    if (compEntry.Times.Count(s => s != null) == 0)
                    {
                        // not yet started
                        compEntry.Status = "Ready";
                    }
                    else
                    {
                        // if there is a finish time
                        if (compEntry.Times[compEntry.Times.Count - 1] != null)
                        {
                            compEntry.Status = "Provisional";
                        }
                        // there are times, but no finish yet
                        else
                        {
                            compEntry.Status = "Started";
                        }

                        // zero the times to the start
                        int startTime = 0;
                        if (compEntry.Times[0] == null)
                        {
                            if (classCourse.StartTime != null)
                            {
                                startTime = (int)classCourse.StartTime;
                            }
                            else
                            {
                                // could not find a start time
                                compEntry.Status = "Error";
                            }
                        }
                        else
                        {
                            startTime = (int)compEntry.Times[0];
                        }

                        // subtract start time from each timestamp
                        for (int i = 0; i < compEntry.Times.Count; i++)
                        {
                            if (compEntry.Times[i] != null)
                            {
                                // null all times if no start is found
                                compEntry.Times[i] = compEntry.Status == "Error" ? null : compEntry.Times[i] - startTime;
                            }
                        }
                    }


                }
                else if (compEntry.Times.Count == 1)
                {
                    // TODO check OE status codes
                    // DNF
                    // DNS
                    // DQ
                }
                else
                {
                    compEntry.Status = "Finished";
                }

                competitors.Add(compEntry);
            }

            competitors.Sort();

            return competitors;
        }

        public static string ByCourseJson(int courseId)
        {
            return JsonConvert.SerializeObject(ByCourse(courseId));
        }

        // gets results for all classes that are running a particular course
        public static List<LeaderboardCompetitor> ByCourse(int courseId)
        {
            using (var context = new Database.CompetitorContext())
            {
                List<Nullable<int>> classes = new List<Nullable<int>>();
                foreach (Database.ClassCourse cc in context.ClassCourses.Where(a => a.CourseId == courseId).ToList())
                {
                    classes.Add(cc.RaceClassId);
                }

                List<LeaderboardCompetitor> leaderboard = new List<LeaderboardCompetitor>();

                foreach (int raceClass in classes)
                {
                    leaderboard.AddRange(ByClass(raceClass));
                }

                leaderboard.Sort();

                return leaderboard;
            }
        }
    }
}
