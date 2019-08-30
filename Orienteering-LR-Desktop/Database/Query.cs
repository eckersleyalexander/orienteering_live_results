using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Orienteering_LR_Desktop.Database
{
    public class CompetitorInfo : IComparable<CompetitorInfo>
    {
        public int CompetitorId;
        public string FirstName;
        public string LastName;
        public Nullable<int> Age;
        public Nullable<int> StartNo;
        public string Gender;

        public Club Club;
        private RaceClassInfo RaceClass;

        public RaceClassInfo GetRaceClass()
        {
            return RaceClass;
        }

        public void SetRaceClass(RaceClassInfo rc)
        {
            RaceClass = rc;
        }

        public int? ChipId;
        public int? TeamId;

        public string Status;

        public List<int?> Times;

        public CompetitorInfo()
        {

        }

        public CompetitorInfo(Competitor competitor, int stage)
        {
            Query query = new Query();
            using (var context = new CompetitorContext())
            {
                Initialize(competitor, stage, query, context);
            }
        }

        public CompetitorInfo(Competitor competitor, int stage, Query query, CompetitorContext context)
        {
            Initialize(competitor, stage, query, context);
        }

        private void Initialize(Competitor competitor, int stage, Query query, CompetitorContext context)
        {
            PartialInitialize(competitor, context);

            if (competitor.RaceClass != null)
            {
                RaceClass = new RaceClassInfo(competitor.RaceClass, stage);
            }
            else if (competitor.RaceClassId != null)
            {
                RaceClass = query.GetRaceClassInfo((int)competitor.RaceClassId, stage);
            }
            else
            {
                RaceClass = null;
            }

            GetTimes(stage, context);
        }

        public void PartialInitialize(Competitor competitor)
        {
            using (var context = new CompetitorContext())
            {
                PartialInitialize(competitor, context);
            }
        }

        //  does not fill RaceClass, ChipId, Status or Times
        public void PartialInitialize(Competitor competitor, CompetitorContext context)
        {
            CompetitorId = competitor.CompetitorId;
            FirstName = competitor.FirstName;
            LastName = competitor.LastName;
            Age = competitor.Age;
            StartNo = competitor.StartNo;
            Gender = competitor.Gender;
            Club = competitor.Club ?? context.Clubs.SingleOrDefault(b => b.ClubId == competitor.ClubId);
            TeamId = null;  // TODO - with relay support
        }

        public void GetTimes(int stage)
        {
            using (var context = new CompetitorContext())
            {
                GetTimes(stage, context);
            }
        }

        // fills ChipId, Status, and Times
        // relies on other fields being filled properly already
        public void GetTimes(int stage, CompetitorContext context)
        {
            Status = "Error";
            Times = new List<int?>();

            CompTime compTime = context.CompTimes.SingleOrDefault(a => a.CompetitorId == CompetitorId && a.Stage == stage);
            if (compTime == null)
            {
                ChipId = null;
                Status = "No Chip";
            }
            else
            {
                ChipId = compTime.ChipId;
                switch (compTime.Status)
                {
                    case 0:
                        Times = JsonConvert.DeserializeObject<List<int?>>(compTime.Times);
                        // if no data sync'd from OE yet (i.e. not yet finished)
                        if (Times.Count(s => s != null) == 0)
                        {
                            if (RaceClass == null)
                            {
                                Status = "No Class";
                            }
                            else if (RaceClass.Course == null)
                            {
                                Status = "No Course";
                            }
                            else if (RaceClass.Course.CourseData == null)
                            {
                                Status = "Empty Course";
                            }
                            else
                            {
                                // scan Punches for times instead
                                foreach (int checkpoint in RaceClass.Course.CourseData)
                                {
                                    Punch punch = context.Punches.SingleOrDefault(a => a.ChipId == ChipId && a.Stage == stage && a.CheckpointId == checkpoint);
                                    Times.Add(punch != null ? (Nullable<int>)punch.Timestamp : null);
                                }

                                // if no times
                                if (Times.Count(s => s != null) == 0)
                                {
                                    // not yet started
                                    Status = "Ready";
                                }
                                else
                                {
                                    // if there is a finish time
                                    if (Times[Times.Count - 1] != null)
                                    {
                                        Status = "Provisional";
                                    }
                                    // there are times, but no finish yet
                                    else
                                    {
                                        Status = "Started";
                                    }

                                    // zero the times
                                    int start = 0;
                                    if (Times[0] != null)
                                    {
                                        start = (int)Times[0];
                                    }
                                    else if (RaceClass.StartTime != null)
                                    {
                                        start = (int)RaceClass.StartTime;
                                    }
                                    else
                                    {
                                        Status = "No Start";
                                        for (int i = 0; i < Times.Count; i++)
                                        {
                                            Times[i] = null;
                                        }
                                    }

                                    if (start != 0)
                                    {
                                        // subtract start time from each timestamp
                                        for (int i = 0; i < Times.Count; i++)
                                        {
                                            Times[i] -= start;
                                        }
                                    }
                                }
                            }
                        }
                        else if (Times[Times.Count - 1] != null)
                        {
                            Status = "Finished";
                        }
                        break;
                    case 1:
                        Status = "DNS";
                        break;
                    case 2:
                        Status = "DNF";
                        break;
                    case 3:
                        Status = "MP";
                        break;
                    case 5:
                        Status = "OT";
                        break;
                    default:
                        break;
                }
            }
        }

        public int CompareTo(CompetitorInfo o)
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

    public class RaceClassInfo
    {
        public int RaceClassId;
        public string Abbreviation;
        public string Name;
        public Nullable<int> AgeFrom;
        public Nullable<int> AgeTo;
        public string Gender;
        public int RaceClassTypeValue;

        public CourseInfo Course;
        public int? StartTime;

        // only partially initializes the class (missing Course and StartTime)
        public RaceClassInfo(RaceClass raceClass)
        {
            PartialInitialize(raceClass);
        }

        public RaceClassInfo(RaceClass raceClass, int stage)
        {
            PartialInitialize(raceClass);

            // if there is no matching course for this class/stage then this will be null
            using (var context = new CompetitorContext())
            {
                ClassCourse cc = null;
                try
                {
                    cc = context.ClassCourses.Include(a => a.Course).Single(b => b.RaceClassId == raceClass.RaceClassId && b.Stage == stage && b.CompetitionPos == 1);
                }
                catch (InvalidOperationException)
                {
                    // if cant retrieve the course leave it as null
                }
                StartTime = cc?.StartTime;
                Course = cc?.Course != null ? new CourseInfo(cc.Course) : null;
            }
        }

        // does not fill the Course field or StartTime fields
        private void PartialInitialize(RaceClass raceClass)
        {
            RaceClassId = raceClass.RaceClassId;
            Abbreviation = raceClass.Abbreviation;
            Name = raceClass.Name;
            AgeFrom = raceClass.AgeFrom;
            AgeTo = raceClass.AgeTo;
            Gender = raceClass.Gender;
            RaceClassTypeValue = raceClass._RaceClassTypeValue;
        }
    }

    public class CourseInfo
    {
        public int CourseId;
        public Nullable<float> DistanceTotalKm; // in km
        public Nullable<float> ClimbTotalM; // in m
        public string Description;
        public List<int> CourseData;
        public List<int?> DistanceData;

        public CourseInfo(Course course)
        {
            CourseId = course.CourseId;
            DistanceTotalKm = course.Distance;
            ClimbTotalM = course.Climb;
            Description = course.Description;

            try
            {
                CourseData = course.CourseData != null ? JsonConvert.DeserializeObject<List<int>>(course.CourseData) : null;
            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                CourseData = null;
            }

            try
            {
                DistanceData = course.DistanceData != null ? JsonConvert.DeserializeObject<List<int?>>(course.DistanceData) : null;
            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                DistanceData = null;
            }
        }
    }

    // adds a list of the classes that are running this course for a particular stage
    public class CourseInfoExtended : CourseInfo
    {
        public List<RaceClassInfo> Classes;

        public CourseInfoExtended(Course course, int stage) : base(course)
        {
            Classes = new List<RaceClassInfo>();
            using (var context = new CompetitorContext())
            {
                foreach (ClassCourse cc in context.ClassCourses.Include(a => a.RaceClass).Where(b => b.CourseId == course.CourseId && b.Stage == stage))
                {
                    RaceClassInfo rc = new RaceClassInfo(cc.RaceClass);
                    rc.StartTime = cc.StartTime;
                    rc.Course = this;
                    Classes.Add(rc);
                }
            }
        }
    }

    public class Query
    {
        public RaceClassInfo GetRaceClassInfo(int raceClassId)
        {
            return GetRaceClassInfo(raceClassId, CurrentStage());
        }

        public RaceClassInfo GetRaceClassInfo(int raceClassId, int stage)
        {
            using (var context = new CompetitorContext())
            {
                try
                {
                    return new RaceClassInfo(context.RaceClasses.Single(a => a.RaceClassId == raceClassId), stage);
                }
                catch (InvalidOperationException)
                {
                    // if this raceclassid doesnt exist
                    return null;
                }
            }
        }

        public Competitor GetCompetitor(int competitorId)
        {
            return GetCompetitor(competitorId, CurrentStage());
        }

        public Competitor GetCompetitor(int competitorId, int stage)
        {
            using (var context = new CompetitorContext())
            {
                return context.CompTimes.SingleOrDefault(b => b.CompetitorId == competitorId && b.Stage == stage)?.Competitor;
            }
        }

        public CompetitorInfo GetCompetitorInfo(int competitorId)
        {
            return GetCompetitorInfo(competitorId, CurrentStage());
        }

        public CompetitorInfo GetCompetitorInfo(int competitorId, int stage)
        {
            using (var context = new CompetitorContext())
            {
                try
                {
                    return new CompetitorInfo(context.CompTimes.Single(b => b.CompetitorId == competitorId && b.Stage == stage).Competitor, stage, this, context);
                }
                catch (InvalidOperationException)
                {
                    // if this compid + stage combo doesnt exist
                    return null;
                }
            }
        }

        public int? FindCompetitorIdByChipId(int chipId)
        {
            return FindCompetitorIdByChipId(chipId, CurrentStage());
        }

        public int? FindCompetitorIdByChipId(int chipId, int stage)
        {
            using (var context = new CompetitorContext())
            {
                return context.CompTimes.SingleOrDefault(b => b.ChipId == chipId && b.Stage == stage)?.CompetitorId;
            }
        }

        public int? GetChipId(int competitorId)
        {
            return GetChipId(competitorId, CurrentStage());
        }

        public int? GetChipId(int competitorId, int stage)
        {
            using (var context = new CompetitorContext())
            {
                return context.CompTimes.SingleOrDefault(b => b.CompetitorId == competitorId && b.Stage == stage)?.ChipId;
            }
        }

        public List<RaceClass> GetClasses()
        {
            using (var context = new CompetitorContext())
            {
                var classes = context.RaceClasses.ToList();
                return classes;
            }
        }

        public List<RaceClassInfo> GetAllClassInfo()
        {
            return GetAllClassInfo(CurrentStage());
        }

        public List<RaceClassInfo> GetAllClassInfo(int stage)
        {
            using (var context = new CompetitorContext())
            {
                List<RaceClassInfo> classes = new List<RaceClassInfo>();
                foreach (RaceClass raceClass in context.RaceClasses)
                {
                    classes.Add(new RaceClassInfo(raceClass, stage));
                }
                return classes;
            }
        }

        public List<Course> GetCourses()
        {
            using (var context = new CompetitorContext())
            {
                var courses = context.Courses.ToList();
                return courses;
            }
        }

        public List<CourseInfo> GetAllCourseInfo()
        {
            using (var context = new CompetitorContext())
            {
                List<CourseInfo> courses = new List<CourseInfo>();
                foreach (Course course in context.Courses)
                {
                    courses.Add(new CourseInfo(course));
                }

                return courses;
            }
        }

        public List<CourseInfoExtended> GetAllCourseInfoExtended()
        {
            return GetAllCourseInfoExtended(CurrentStage());
        }

        public List<CourseInfoExtended> GetAllCourseInfoExtended(int stage)
        {
            using (var context = new CompetitorContext())
            {
                List<CourseInfoExtended> courses = new List<CourseInfoExtended>();
                foreach (Course course in context.Courses)
                {
                    courses.Add(new CourseInfoExtended(course, stage));
                }

                return courses;
            }
        }
        
        public List<Club> GetClubs()
        {
            using (var context = new CompetitorContext())
            {
                var clubs = context.Clubs.ToList();
                return clubs;
            }
        }
        
        public List<Competitor> GetCompetitors()
        {
            using (var context = new CompetitorContext())
            {
                var competitors = context.Competitors.ToList();
                return competitors;
            }
        }

        public List<CompetitorInfo> GetAllCompetitorInfo()
        {
            return GetAllCompetitorInfo(CurrentStage());
        }

        public List<CompetitorInfo> GetAllCompetitorInfo(int stage)
        {
            using (var context = new CompetitorContext())
            {
                List<CompetitorInfo> competitors = new List<CompetitorInfo>();
                foreach (Competitor comp in context.Competitors)
                {
                    competitors.Add(new CompetitorInfo(comp, stage, this, context));
                }

                return competitors;
            }
        }

//        public List<CompetitorPunches> GetLeaderBoardClass(int classId)
//        {
//            using (var context = new CompetitorContext())
//            {
//                var competitors = context.Competitors.Where(c => c.RaceClassId == classId).ToList();
//                List<CompetitorPunches> leaderBoard = new List<CompetitorPunches>();
//                foreach (var competitor in competitors)
//                {
//                    CompetitorPunches compPunches = (CompetitorPunches) competitor;
//                    List<Punch> punches = context.Punches.Where(p => p.ChipId == competitor.ChipId).ToList();
//                    compPunches.Punches = punches;
//                    leaderBoard.Add(compPunches);
//                }
//                return leaderBoard;
//            }
//        }

        public List<CompetitorPunches> GetCompetitorPunches()
        {
            using (var context = new CompetitorContext())
            {
                var competitors = context.Competitors.ToList();
                List<CompetitorPunches> things = new List<CompetitorPunches>();
                foreach (var competitor in competitors)
                {
                    CompetitorPunches compPunches = new CompetitorPunches(){
                        FirstName = competitor.FirstName,
                        LastName = competitor.LastName
                    };
                    List<Punch> punches = context.Punches.Where(p => p.ChipId == context.CompTimes.Single(a => a.CompetitorId == competitor.CompetitorId && a.Stage == CurrentStage()).ChipId && p.Stage == CurrentStage()).ToList();
                    compPunches.Punches = punches;
                    things.Add(compPunches);
                }
                return things;
            }
        }

        public int CurrentStage()
        {
            using (var context = new CompetitorContext())
            {
                return context.Stages.Single(s => s.Current).StageId;
            }
        }
    }
    
    public class CompetitorPunches : Competitor
    {
        public List<Punch> Punches { get; set; }
    }
}