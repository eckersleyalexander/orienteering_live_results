using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media.Converters;

namespace Orienteering_LR_Desktop.Database
{
    public class Store
    {
        public void SetCurrentStage(string stageName)
        {
            using (var context = new CompetitorContext())
            {
                var stage = new Stage {Name = stageName, Current = false};
                context.Stages.Add(stage);
                context.SaveChanges();
            }
        }

        public void CreatePunch(int chipId, int controlId, int timestamp)
        {
            using (var context = new CompetitorContext())
            {
                var punch = new Punch {ChipId = chipId, Stage = "1", CheckpointId = controlId, Timestamp = timestamp};
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
    }
}
