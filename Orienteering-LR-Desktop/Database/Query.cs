using System.Collections.Generic;
using System.Linq;

namespace Orienteering_LR_Desktop.Database
{
    public class Query
    {
        public List<RaceClass> GetClasses()
        {
            using (var context = new CompetitorContext())
            {
                var classes = context.RaceClasses.ToList();
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
                    List<Punch> punches = context.Punches.Where(p => p.ChipId == competitor.ChipId).ToList();
                    compPunches.Punches = punches;
                    things.Add(compPunches);
                }
                return things;
            }
        }

        public string CurrentStage()
        {
            using (var context = new CompetitorContext())
            {
                return context.Stages.Single(s => s.Current).Name;
            }
        }
    }
    
    public class CompetitorPunches : Competitor
    {
        public List<Punch> Punches { get; set; }
    }
}