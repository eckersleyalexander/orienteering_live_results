using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media.Converters;

namespace Orienteering_LR_Desktop.Database
{
    public class Store
    {
        public void CreateStage(int stageId)
        {
            using (var context = new CompetitorContext())
            {
                var stage = new Stage {StageId = stageId, Current = false};
                context.Stages.Add(stage);
                context.SaveChanges();
            }
        }

        public void SetCurrentStage(int stageId)
        {
            using (var context = new CompetitorContext())
            {
                var stage = context.Stages.Find(stageId);
                if (stage != null)
                {
                    stage.Current = true;
                    context.Update(stage);
                    context.SaveChanges();
                } else
                {
                    context.Stages.Add(new Stage { StageId = stageId, Current = true });
                    context.SaveChanges();
                }
                
            }
           
        }

        public void CreatePunch(int chipId, int controlId, int timestamp)
        {
            using (var context = new CompetitorContext())
            {
                var punch = new Punch {ChipId = chipId, Stage = 1, CheckpointId = controlId, Timestamp = timestamp};
                context.Punches.Add(punch);
                context.SaveChanges();
            }
        }

        public void CreateCompetitor(Competitor comp)
        {
            using (var context = new CompetitorContext())
            {
                context.Competitors.Add(comp);
                context.SaveChanges();
            }
        }

        public void CreateClub(Club club)
        {
            using (var context = new CompetitorContext())
            {
                context.Clubs.Add(club);
                context.SaveChanges();
            }
        }

        public void CreateRaceClass(RaceClass race)
        {
            using (var context = new CompetitorContext())
            {
                context.RaceClasses.Add(race);
                context.SaveChanges();
            }
        }

        public void CreateTeam(Team team)
        {
            using (var context = new CompetitorContext())
            {
                context.Teams.Add(team);
                context.SaveChanges();
            }
        }

        public void CreateCourse(Course course)
        {
            using (var context = new CompetitorContext())
            {
                context.Courses.Add(course);
                context.SaveChanges();
            }
        }

        public void CreateClassCourse(ClassCourse classCourse)
        {
            using (var context = new CompetitorContext())
            {
                context.ClassCourses.Add(classCourse);
                context.SaveChanges();
            }
        }

        public void CreateCompTimes(CompTime compTimes)
        {
            using (var context = new CompetitorContext())
            {
                context.CompTimes.Add(compTimes);
                context.SaveChanges();
            }
        }
    }
}
