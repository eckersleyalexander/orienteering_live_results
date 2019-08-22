using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Orienteering_LR_Desktop.Database
{
    [NotMapped]
    class CompetitorContext : DbContext
    {
        public DbSet<Competitor> Competitors { get; set; }
        public DbSet<Club> Clubs { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<CompTimes> CompTimes { get; set; }
        public DbSet<RaceClass> RaceClasses { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Punch> Punches { get; set; }
        public DbSet<Stage> Stages { get; set; }
        public DbSet<ClassCourse> ClassCourses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ClassCourse>()
                .HasKey(c => new { c.RaceClassId, c.CourseId });
            modelBuilder.Entity<CompTimes>()
                .HasKey(c => new { c.CompetitorId, c.Stage });
            modelBuilder.Entity<Stage>()
                .HasKey(c => new {c.Name});
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=testdb.db");
        }
    }


	public class Competitor {
        public int CompetitorId {get; set;}
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Age { get; set; }
        public int StartNo { get; set; }
        public string Gender { get; set; }
        public int ChipId { get; set; }
        public int ClubId { get; set; } // fk -> Club
        public Club Club { get; set; }
        public int RaceClassId { get; set; } // fk -> RaceClass
        public RaceClass RaceClass { get; set; }
	}	

    public class Club {
        public int ClubId { get; set; }
        public string Name { get; set; }
    }

    public class Team {
        public int TeamId { get; set; }
        public int CompetitorPos { get; set; }
        public int CompetitorId { get; set; }
        public Competitor Competitor { get; set; }
    }

    public class CompTimes {
        public int CompetitorId { get; set; }
        public Competitor Competitor { get; set; }
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
        public Course Course { get; set; }
    }

    public class Punch {
        public int PunchId { get; set; }
        public int ChipId { get; set; }
        public string Stage { get; set; }
        public int CheckpointId { get; set; }
        public int Timestamp { get; set; }
    }

    public class Stage
    {
        public string Name { get; set; } // pk
        public bool Current { get; set; }
    }


}