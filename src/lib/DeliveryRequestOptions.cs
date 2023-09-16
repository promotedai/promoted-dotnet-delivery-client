namespace Promoted.Lib
{
    public class DeliveryRequestOptions
    {
        public bool OnlyLogToMetrics { get; set; }

        public Event.CohortMembership? Experiment { get; set; } = null;

        private int _insertionStartIndex;

        public int InsertionStartIndex
        {
            get { return _insertionStartIndex; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("InsertionStartIndex must be greater than or equal to 0.");
                }
                _insertionStartIndex = value;
            }
        }
    }
}
