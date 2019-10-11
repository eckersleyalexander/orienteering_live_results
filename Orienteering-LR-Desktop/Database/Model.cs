using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;

namespace Orienteering_LR_Desktop.Database
{
    [NotMapped]
    public class CompetitorContext : DbContext
    {
        public DbSet<Competitor> Competitors { get; set; }
        public DbSet<Club> Clubs { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<CompTime> CompTimes { get; set; }
        public DbSet<RaceClass> RaceClasses { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<ClassCourse> ClassCourses { get; set; }
        public DbSet<Punch> Punches { get; set; }
        public DbSet<Stage> Stages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ClassCourse>()
                .HasKey(c => new { c.RaceClassId, c.Stage, c.CompetitionPos });
            modelBuilder.Entity<CompTime>()
                .HasKey(c => new { c.CompetitorId, c.Stage });
            modelBuilder.Entity<Stage>()
                .HasKey(c => new {c.StageId});
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=LRDB.db");
        }
    }


	public class Competitor {
        public int CompetitorId {get; set;}
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Nullable<int> Age { get; set; }
        public Nullable<int> StartNo { get; set; }
        public string Gender { get; set; }
        
        public Nullable<int> ClubId { get; set; } // fk -> Club
        public Club Club { get; set; }
        
        public Nullable<int> RaceClassId { get; set; } // fk -> RaceClass
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

    public class CompTime {
        public int CompetitorId { get; set; }
        public Competitor Competitor { get; set; }
        public int Stage { get; set; }
        public int ChipId { get; set; }
        public int? Status { get; set; }
        public string Times { get; set; }
        public int? StartTime { get; set; }
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
        public Nullable<int> AgeFrom { get; set; }
        public Nullable<int> AgeTo { get; set; }
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
        public Nullable<float> Distance { get; set; } // in km
        public Nullable<float> Climb { get; set; } // in m
        public string Description { get; set; }
        public string CourseData { get; set; }
        public string DistanceData { get; set; }
    }

    public class ClassCourse {
        public int CompetitionPos { get; set; } // this is just 1 (for now)
        public int Stage { get; set; }
        public Nullable<int> StartTime { get; set; }
        public int RaceClassId { get; set; }
        public RaceClass RaceClass { get; set; }
        public Nullable<int> CourseId { get; set; }
        public Course Course { get; set; }
    }

    public class Punch {
        public int PunchId { get; set; }
        public int ChipId { get; set; }
        public int Stage { get; set; }
        public int CheckpointId { get; set; }
        public int Timestamp { get; set; }
    }

    public class Stage
    {
        public int StageId { get; set; } // pk
        public bool Current { get; set; }
    }


}