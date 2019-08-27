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
            public Database.RaceClassInfo Class;
            public List<Database.CompetitorInfo> Competitors;
        }

        public class CourseLeaderboard
        {
            public Database.CourseInfo Course;
            public List<Database.RaceClassInfo> Classes;
            public List<Database.CompetitorInfo> Competitors;
        }

        public static string GetAllClassesJson()
        {
            return JsonConvert.SerializeObject(GetAllClasses());
        }

        public static List<ClassLeaderboard> GetAllClasses()
        {
            List<ClassLeaderboard> leaderboards = new List<ClassLeaderboard>();
            using (var context = new Database.CompetitorContext())
            {
                int stage = context.Stages.Single(a => a.Current).StageId;
                foreach (Database.RaceClassInfo raceClass in new Database.Query().GetAllClassInfo(stage))
                {
                    leaderboards.Add(ByClass(raceClass, stage, context));
                }
            }

            return leaderboards;
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
                        Competitors = new List<Database.CompetitorInfo>()
                    };
                }
            }
        }

        public static ClassLeaderboard ByClass(Database.RaceClassInfo raceClass, int stage, Database.CompetitorContext context)
        {
            ClassLeaderboard leaderboard = new ClassLeaderboard
            {
                Competitors = new List<Database.CompetitorInfo>(),
                Class = raceClass
            };

            foreach (Database.Competitor c in context.Competitors.Where(a => a.RaceClassId == raceClass.RaceClassId))
            {
                Database.CompetitorInfo info = new Database.CompetitorInfo();
                info.PartialInitialize(c, context);
                info.SetRaceClass(raceClass);
                info.GetTimes(stage, context);

                if (info.ChipId != null)
                {
                    leaderboard.Competitors.Add(info);
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
                int stage = context.Stages.Single(a => a.Current).StageId;

                CourseLeaderboard leaderboard = new CourseLeaderboard
                {
                    Course = course != null ? new Database.CourseInfo(course) : null,
                    Classes = new List<Database.RaceClassInfo>(),
                    Competitors = new List<Database.CompetitorInfo>()
                };

                foreach (Database.ClassCourse cc in context.ClassCourses.Include(b => b.RaceClass).Where(a => a.CourseId == courseId))
                {
                    Database.RaceClassInfo raceClass = new Database.RaceClassInfo(cc.RaceClass, stage);
                    leaderboard.Classes.Add(raceClass);
                    leaderboard.Competitors.AddRange(ByClass(raceClass, stage, context).Competitors);
                }

                leaderboard.Competitors.Sort();

                return leaderboard;
            }
        }
    }
}
