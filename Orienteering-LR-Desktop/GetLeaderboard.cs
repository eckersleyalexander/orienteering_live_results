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
            public Nullable<int> ChipNo;
            public string FirstName;
            public string LastName;
            public Nullable<int> Age;
            public Nullable<int> StartNo;
            public string Gender;
            public string Club;
            public string RaceClass;
            public string Status;
            public List<Nullable<int>> Times;

            public LeaderboardCompetitor(Database.CompetitorInfo competitorInfo)
            {
                Initialize(competitorInfo);
            }

            public LeaderboardCompetitor(Database.Competitor competitor, int stage)
            {
                Initialize(new Database.CompetitorInfo(competitor, stage));
            }

            private void Initialize(Database.CompetitorInfo competitorInfo)
            {
                CompetitorId = competitorInfo.CompetitorId;
                ChipNo = competitorInfo.ChipId;
                FirstName = competitorInfo.FirstName;
                LastName = competitorInfo.LastName;
                Age = competitorInfo.Age;
                StartNo = competitorInfo.StartNo;
                Gender = competitorInfo.Gender;
                Club = competitorInfo.Club?.Name;
                RaceClass = competitorInfo.RaceClass?.Abbreviation;
                Status = competitorInfo.Status;
                Times = competitorInfo.Times;
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
            List<LeaderboardCompetitor> leaderboard = new List<LeaderboardCompetitor>();
            Database.RaceClassInfo raceClass;
            int stage;

            using (var context = new Database.CompetitorContext())
            {
                stage = context.Stages.Single(a => a.Current).StageId;
                try
                {
                    raceClass = new Database.RaceClassInfo(context.RaceClasses.Single(a => a.RaceClassId == raceClassId), stage);
                    if (raceClass.Course == null)
                    {
                        throw new InvalidOperationException();
                    }
                }
                catch (InvalidOperationException)
                {
                    // return empty list if raceClass does not exist or if the associated course can't be retrieved
                    return leaderboard;
                }

                foreach (Database.Competitor c in context.Competitors.Where(a => a.RaceClassId == raceClassId))
                {
                    Database.CompetitorInfo info = new Database.CompetitorInfo();
                    info.PartialInitialize(c, context);
                    info.RaceClass = raceClass;
                    info.GetTimes(stage, context);

                    if (info.ChipId != null)
                    {
                        LeaderboardCompetitor comp = new LeaderboardCompetitor(info);
                        leaderboard.Add(comp);
                    }
                }
            }

            leaderboard.Sort();

            return leaderboard;
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
                foreach (Database.ClassCourse cc in context.ClassCourses.Where(a => a.CourseId == courseId))
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
