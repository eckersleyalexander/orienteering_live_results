using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Orienteering_LR_Desktop.Database
{
    [NotMapped]
    class ClassContext : DbContext
    {
        public DbSet<ClassCourse> ClassCourses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ClassCourse>()
                .HasKey(c => new { c.RaceClassId, c.CourseId });
        }
    }


	public class Competitor {
        public int CompetitorId {get; set;}
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Age { get; set; }
        public int StartNo { get; set; }
        public string Gender { get; set; }
        public int ClassId { get; set; } // fk -> Class
        public int ClubId { get; set; } // fk -> Club
        public int RaceClassId { get; set; } // fk -> RaceClass



	}	

    public class Club {
        public int ClubId { get; set; }
        public string name { get; set; }
    }

    public class Team {
        public int TeamId { get; set; }
        public int CompetitorPos { get; set; }
        public int CompetitorId { get; set; }
    }

    public class CompTimes {
        public int CompetitorId { get; set; }
        public string Stage { get; set; }
        public int ChipId { get; set; }
        public string Times { get; set; }
    }


    public enum RaceClassType {
        Normal = 0,
        Championship = 1,
        Secondary = 2,
        Youngsters = 3,
        Open = 4,
        Beginners = 5,
        Seven = 6,
        Eight = 7,
        Nine = 8,
        Ten = 9

    }
    public class RaceClass {
        public int RaceClassId { get; set; }
        public string Abbreviation { get; set; }
        public string Name { get; set; }
        public int AgeFrom { get; set; }
        public int AgeTo { get; set; }
        public string Gender { get; set; }
        public int _RaceClassTypeValue { get; set; }
        public RaceClassType RaceClassType 
        {
            get => (RaceClassType)_RaceClassTypeValue;
            set => _RaceClassTypeValue = (int)value;
        }
    }

    public class Course {
        public int CourseId { get; set; }
        public float Distance { get; set; } // in km
        public float Climb { get; set; } // in m
        public string Description { get; set; }

        public string CourseData { get; set; }
        public string DistanceData { get; set; }
    }

    public class ClassCourse {
        public int CompetitionPos { get; set; }
        public string Stage { get; set; }

        public int RaceClassId { get; set; }
        public int CourseId { get; set; }
    }

    public class Punch {
        public int ChipId { get; set; }
        public string stage { get; set; }
        public int CheckId { get; set; }
        public int Timestamp { get; set; }
    }


}