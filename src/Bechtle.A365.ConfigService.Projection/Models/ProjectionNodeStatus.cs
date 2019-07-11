namespace Bechtle.A365.ConfigService.Projection.Models
{
    public class ProjectionNodeStatus
    {
        public ProjectionStatus CurrentStatus { get; set; }

        public ProjectionEventStatus LastEvent { get; set; }

        public ProjectionEventStatus CurrentEvent { get; set; }

        public string NodeId { get; set; }
    }
}