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
    }
}
