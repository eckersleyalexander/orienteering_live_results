using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore.Internal;

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
                RaceClass = query.GetRaceClassInfo((int) competitor.RaceClassId, stage);
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
            TeamId = null; // TODO - with relay support
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

            CompTime compTime =
                context.CompTimes.SingleOrDefault(a => a.CompetitorId == CompetitorId && a.Stage == stage);
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
                                    Punch punch = context.Punches.FirstOrDefault(a => a.ChipId == ChipId && a.Stage == stage && a.CheckpointId == checkpoint);
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
                                        start = (int) Times[0];
                                    }
                                    else if (RaceClass.StartTime != null)
                                    {
                                        start = (int) RaceClass.StartTime;
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
                int LastTime = (int) Times.Last(a => a != null);
                int LastTimeo = (int) o.Times.Last(a => a != null);
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
                    cc = context.ClassCourses.Include(a => a.Course).Single(b =>
                        b.RaceClassId == raceClass.RaceClassId && b.Stage == stage && b.CompetitionPos == 1);
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
                CourseData = course.CourseData != null
                    ? JsonConvert.DeserializeObject<List<int>>(course.CourseData)
                    : null;
            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                CourseData = null;
            }

            try
            {
                DistanceData = course.DistanceData != null
                    ? JsonConvert.DeserializeObject<List<int?>>(course.DistanceData)
                    : null;
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
                foreach (ClassCourse cc in context.ClassCourses.Include(a => a.RaceClass)
                    .Where(b => b.CourseId == course.CourseId && b.Stage == stage))
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
        public CompData GetCompData()
        {
            CompData data = new CompData("<Comp name>", "<comp-date>",
                new CompData.Marquee(0, "TechLauncher Showcase - Orienteering Live Results", 60)
            );
            using (var context = new CompetitorContext())
            {
                foreach (var raceClass in context.RaceClasses)
                {
                    var compTimes = context.Competitors
                        .Where(competitor => competitor.RaceClassId == raceClass.RaceClassId)
                        .Join(context.Clubs,
                            competitor => competitor.ClubId,
                            club => club.ClubId,
                            (competitor, club) => new
                            {
                                competitor, club
                            }
                        )
                        .Join(context.CompTimes,
                            compclub => compclub.competitor.CompetitorId,
                            compTime => compTime.CompetitorId,
                            (compclub, compTime) => new
                            {
                                id = compclub.competitor.CompetitorId,
                                competitor = compclub.competitor.FirstName + " " + compclub.competitor.LastName,
                                club = compclub.competitor.Club.Name,
                                status = 1,//compTime.Status,
                                times = compTime.Times,
                                starttime = compTime.StartTime,
                                punches = context.Punches.Where(punch => punch.ChipId == compTime.ChipId).ToList()
                            }).ToList();

                    var classCourse =
                        context.ClassCourses.FirstOrDefault(cc => cc.RaceClassId == raceClass.RaceClassId);
                    var course = context.Courses.FirstOrDefault(c => c.CourseId == classCourse.CourseId);
                    if (compTimes.Count > 0)
                    {
                        data.AddToCompResults(raceClass, course, compTimes);
                    }
                }
            }

            return data;
        }

        public Boolean StageExists(int stageId)
        {
            using (var context = new CompetitorContext())
            {
                return context.Stages.Any(q => q.StageId == stageId);
            }
        }

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
                return context.CompTimes.SingleOrDefault(b => b.CompetitorId == competitorId && b.Stage == stage)
                    ?.Competitor;
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
                    return new CompetitorInfo(
                        context.CompTimes.Single(b => b.CompetitorId == competitorId && b.Stage == stage).Competitor,
                        stage, this, context);
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
                return context.CompTimes.SingleOrDefault(b => b.CompetitorId == competitorId && b.Stage == stage)
                    ?.ChipId;
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
                    CompetitorPunches compPunches = new CompetitorPunches()
                    {
                        FirstName = competitor.FirstName,
                        LastName = competitor.LastName
                    };
                    List<Punch> punches = context.Punches.Where(p =>
                        p.ChipId == context.CompTimes.Single(a =>
                            a.CompetitorId == competitor.CompetitorId && a.Stage == CurrentStage()).ChipId &&
                        p.Stage == CurrentStage()).ToList();
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

    public class CompData
    {
        public string cmpName;
        public string cmpDate;
        public Marquee marquee;
        public List<CompResults> cmpResults;

        public CompData(string cmpName, string cmpDate, Marquee marquee)
        {
            this.cmpName = cmpName;
            this.cmpDate = cmpDate;
            this.marquee = marquee;
            this.cmpResults = new List<CompResults>();
        }

        private dynamic processTimes(List<string> times, List<int> courseData, List<int> radioControls)
        {
            List<string> filteredTimes = new List<string>();
            for (int i = 1; i < times.Count - 1; i++)
            {
                if (radioControls.Contains(courseData[i]))
                {
                    filteredTimes.Add((times[i]));
                }
            }


            string startTime = times.ElementAt(0) ?? "null";
            string finishTime = times.ElementAt(times.Count - 1) ?? "null";


            // process comp times (find ranks etc)
            return new
            {
                startTime = startTime,
                finishTime = finishTime,
                radioTimes = filteredTimes
            };
        }

        public void AddToCompResults(RaceClass raceClass, Course course, IEnumerable<dynamic> compTimes)
        {
            List<int> courseData = new List<int>();
            List<int> radioControls = new List<int>();
            Application.Current.Dispatcher.Invoke((Action) (() =>
            {
                var mainWindow = (MainWindow) Application.Current.MainWindow;
                courseData = JsonConvert.DeserializeObject<List<int>>(course.CourseData);
                foreach (int r in courseData)
                {
                    radioControls.Add(r);
                }
            }));


            // radio info
            List<CompResults.RadioInfo> radioInfos = new List<CompResults.RadioInfo>();
            for (int i = 1; i < radioControls.Count -1; i++)
            {
                CompResults.RadioInfo radioInfo =
                    new CompResults.RadioInfo(radioControls[i], i*1000, (int) (100 * (i + 1.0) / radioControls.Count));
                radioInfos.Add(radioInfo);
            }

            CompResults compResults = new CompResults(raceClass.RaceClassId, raceClass.Abbreviation,
                course.Description, course.Distance.ToString(), radioInfos, course.CourseData);

            // class results
            compResults.AddClassResults(compTimes, radioControls);

            // save comp results
            this.cmpResults.Add(compResults);
        }


        public class Marquee
        {
            public int show; // 1 or 0
            public string text;
            public int duration; //seconds

            public Marquee(int show, string text, int duration)
            {
                this.show = show;
                this.text = text;
                this.duration = duration;
            }
        }

        public class CompResults
        {
            public int clsId;
            public string clsName;
            public string course;
            public string length;
            public int radioCount;
            public List<RadioInfo> radioInfo;
            public List<ClassResults> clsResults;
            private List<int?> CourseData;

            public CompResults(int clsId, string clsName, string course, string length, List<RadioInfo> radioInfos, string courseData)
            {
                this.clsId = clsId;
                this.clsName = clsName;
                this.course = course;
                this.length = length;
                this.radioCount = radioInfos.Count;
                this.clsResults = new List<ClassResults>();
                this.radioInfo = radioInfos;
                this.CourseData = JsonConvert.DeserializeObject<List<int?>>(courseData);
            }

            private string _formatTime(int seconds)
            {
                var span = TimeSpan.FromSeconds(seconds);
                return $"{(int)Math.Floor(span.TotalMinutes)}:{span.Seconds:D2}";
            }


            public void AddClassResults(IEnumerable<dynamic> compTimes, List<int> radioControls)
            {
                if (radioControls.Count == 0)
                {
                    foreach (var compTime in compTimes)
                    {
                        this.clsResults.Add(new ClassResults(compTime.id.ToString(), compTime.competitor,
                            compTime.club,
                            compTime.status.ToString(), "0" /* or global start time */,
                            null, "", "", new List<ClassResults.Radio>()));
                    }

                    return;
                }

                List<ClassResults> clsResults = new List<ClassResults>();
                foreach (var compTime in compTimes)
                {
                    List<int?> Times = JsonConvert.DeserializeObject<List<int?>>(compTime.times);
                    List<Punch> punches = (List<Punch>)compTime.punches;
                    List<ClassResults.Radio> radioTimes = new List<ClassResults.Radio>();

                    if (Times[Times.Count - 1] != null)
                    {
                        // competitor has finished - use sync'd OE times

                    }
                    else
                    {
                        // competitor does not have OE data - attempt to use radio punches
                        Times[0] = 0;
                        Times[Times.Count - 1] = punches.Find(p => p.CheckpointId == OESync.FINISH_CHECKPOINT)?.Timestamp;
                        if (Times[Times.Count - 1] != null)
                        {
                            Times[Times.Count - 1] -= compTime.starttime;
                        }
                        for (int i = 1; i < Times.Count - 1; i++)
                        {
                            // try and find punches for whole course
                            Times[i] = punches.Find(p => p.CheckpointId == CourseData[i])?.Timestamp;
                        }
                    }

                    for (int i = 1; i < Times.Count - 1; i++)
                    {
                        if (CourseData[i] != null && Times[i] != null && radioControls.Contains((int)CourseData[i]))
                        {
                            radioTimes.Add(new ClassResults.Radio((int)CourseData[i], (((int)Times[i] - compTime.starttime)/ 1000).ToString(), "", ""));
                        }
                        else
                        {
                            radioTimes.Add(new ClassResults.Radio((int)CourseData[i], "0", "", ""));
                        }
                    }

                    clsResults.Add(new ClassResults(compTime.id.ToString(), compTime.competitor,
                        compTime.club,
                        compTime.status.ToString(), Times[0]?.ToString(),
                        (Times[Times.Count - 1] == null ? 0 : (int)(Times[Times.Count - 1]/1000)).ToString(), "", "", radioTimes));
                }

                this.clsResults = clsResults;
                
                // add ranks to radio times 
                HashSet<int> radioInfoUsed = new HashSet<int>();
                foreach (var radioInfo in this.radioInfo)
                {
                    // filter clsResults for competitors with a punch on this control
                    List<ClassResults> sortedPunches = clsResults
                        .Where(cr => cr.radios.Exists(r => r.code == radioInfo.code))
                        .OrderBy(cr => cr.radios.Find(r => r.code == radioInfo.code).time).ToList();
                    if (sortedPunches.Count == 0)
                    {
                        continue;
                    }

                    int rank = 1;
                    int firstTime = Int32.Parse(sortedPunches.First().radios.Find(r => r.code == radioInfo.code).time);
                    foreach (var punch in sortedPunches)
                    {
                        ClassResults.Radio thePunch = punch.radios.Find(r => r.code == radioInfo.code);
                        thePunch.rank = rank.ToString();
                        string diff = "+" + this._formatTime(0);//int.Parse(thePunch.time) - firstTime);
                        thePunch.diff = rank == 1 ? "" : diff;
                        rank += 1;
                        radioInfoUsed.Add(thePunch.code);
                        thePunch.time = _formatTime(int.Parse(thePunch.time));
                    }
                }

                // filter radio info and set radioCount based on above
                this.radioCount = clsResults.Max(cr => cr.radios.Count);
//                this.radioInfo = this.radioInfo.Where(ri => radioInfoUsed.Contains(ri.code)).ToList();


                // add ranks based on finish time
                List<ClassResults> sortedResults = clsResults.Where(cr => !string.IsNullOrEmpty(cr.finishTime))
                    .OrderBy(cr => Int32.Parse(cr.finishTime)).ToList();
                if (sortedResults.Count == 0)
                {
                    return;
                }

                int finRank = 1;
                int firstFinTime = int.Parse(sortedResults.First().finishTime);
                foreach (var classResult in sortedResults)
                {
                    classResult.finishRank = finRank.ToString();
                    string diff = "+" + this._formatTime(int.Parse(classResult.finishTime) - firstFinTime);
                    classResult.finishDiff = finRank == 1 ? "" : diff;
                    finRank += 1;
                    classResult.finishTime = _formatTime(int.Parse(classResult.finishTime));
                }
            }


            public class RadioInfo
            {
                public int code;
                public int distance;
                public int percentage;

                public RadioInfo(int code, int distance, int percentage)
                {
                    this.code = code;
                    this.distance = distance;
                    this.percentage = percentage;
                }
            }

            public class ClassResults
            {
                public string id;
                public string competitor; // name
                public string club;
                public string status;
                public string startTime;
                public string finishTime;
                public string finishRank;
                public string finishDiff;
                public List<Radio> radios;

                public ClassResults(string id, string competitor, string club, string status, string startTime,
                    string finishTime, string finishRank, string finishDiff, List<Radio> radioTimes)
                {
                    this.id = id;
                    this.competitor = competitor;
                    this.club = club;
                    this.status = status;
                    this.startTime = startTime;
                    this.finishTime = finishTime;
                    this.finishRank = finishRank;
                    this.finishDiff = finishDiff;
                    this.radios = radioTimes;
                }

                public class Radio
                {
                    public int code;
                    public string time;
                    public string rank;
                    public string diff;

                    public Radio(int code, string time, string rank, string diff)
                    {
                        this.code = code;
                        this.time = time;
                        this.rank = rank;
                        this.diff = diff;
                    }
                }
            }
        }
    }
}