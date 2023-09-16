namespace Promoted.Lib
{
    public class DeliveryRequestOptions
    {
        public bool OnlyLogToMetrics { get; set; }

        public Event.CohortMembership? Experiment { get; set; } = null;
    }
}
