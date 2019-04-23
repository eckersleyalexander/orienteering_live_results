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
    }
}