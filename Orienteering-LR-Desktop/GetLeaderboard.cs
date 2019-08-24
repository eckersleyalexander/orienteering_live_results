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
        public class ClassLeaderboard
        {
            public string Type = "Class";
            public Database.RaceClassInfo Class;
            public int Stage;
            public List<LeaderboardCompetitor> Competitors;
        }

        public class CourseLeaderboard
        {
            public string Type = "Course";
            public Database.CourseInfo Course;
            public List<Database.RaceClassInfo> Classes;
            public int Stage;
            public List<LeaderboardCompetitor> Competitors;
        }

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

        public static ClassLeaderboard ByClass(int raceClassId)
        {
            using (var context = new Database.CompetitorContext())
            {
                int stage = context.Stages.Single(a => a.Current).StageId;
                try
                {
                    Database.RaceClassInfo raceClass = new Database.RaceClassInfo(context.RaceClasses.Single(a => a.RaceClassId == raceClassId), stage);
                    if (raceClass.Course == null)
                    {
                        throw new InvalidOperationException();
                    }
                    else
                    {
                        return ByClass(raceClass, stage, context);
                    }
                }
                catch (InvalidOperationException)
                {
                    // return empty leaderboard if raceClass does not exist or if the associated course can't be retrieved
                    return new ClassLeaderboard()
                    {
                        Competitors = new List<LeaderboardCompetitor>()
                    };
                }
            }
        }

        public static ClassLeaderboard ByClass(Database.RaceClassInfo raceClass, int stage, Database.CompetitorContext context)
        {
            ClassLeaderboard leaderboard = new ClassLeaderboard
            {
                Competitors = new List<LeaderboardCompetitor>(),
                Class = raceClass,
                Stage = stage
            };

            foreach (Database.Competitor c in context.Competitors.Where(a => a.RaceClassId == raceClass.RaceClassId))
            {
                Database.CompetitorInfo info = new Database.CompetitorInfo();
                info.PartialInitialize(c, context);
                info.RaceClass = raceClass;
                info.GetTimes(stage, context);

                if (info.ChipId != null)
                {
                    leaderboard.Competitors.Add(new LeaderboardCompetitor(info));
                }
            }

            leaderboard.Competitors.Sort();

            return leaderboard;
        }

        public static string ByCourseJson(int courseId)
        {
            return JsonConvert.SerializeObject(ByCourse(courseId));
        }

        // gets results for all classes that are running a particular course
        public static CourseLeaderboard ByCourse(int courseId)
        {
            using (var context = new Database.CompetitorContext())
            {
                Database.Course course = context.Courses.SingleOrDefault(a => a.CourseId == courseId);

                CourseLeaderboard leaderboard = new CourseLeaderboard
                {
                    Stage = context.Stages.Single(a => a.Current).StageId,
                    Course = course != null ? new Database.CourseInfo(course) : null,
                    Classes = new List<Database.RaceClassInfo>(),
                    Competitors = new List<LeaderboardCompetitor>()
                };

                foreach (Database.ClassCourse cc in context.ClassCourses.Include(b => b.RaceClass).Where(a => a.CourseId == courseId))
                {
                    Database.RaceClassInfo raceClass = new Database.RaceClassInfo(cc.RaceClass, leaderboard.Stage);
                    leaderboard.Classes.Add(raceClass);
                    leaderboard.Competitors.AddRange(ByClass(raceClass, leaderboard.Stage, context).Competitors);
                }

                leaderboard.Competitors.Sort();

                return leaderboard;
            }
        }
    }
}
