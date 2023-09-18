namespace Promoted.Lib
{
    public class DeliveryRequestOptions
    {
        public bool OnlyLogToMetrics { get; set; }

        public Promoted.Event.CohortMembership? Experiment { get; set; } = null;

        private int _retrievalInsertionOffset;

        public int RetrievalInsertionOffset
        {
            get { return _retrievalInsertionOffset; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("RetrievalInsertionOffset must be greater than or equal to 0.");
                }
                _retrievalInsertionOffset = value;
            }
        }
    }
}
