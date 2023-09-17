namespace Promoted.Lib
{
    public class DeliveryClientOptions
    {
        private float _shadowTrafficRate;

        public float ShadowTrafficRate
        {
            get { return _shadowTrafficRate; }
            set
            {
                if (value < 0 || value > 1)
                {
                    throw new ArgumentException("ShadowTrafficRate must be between 0 and 1, inclusive.");
                }
                _shadowTrafficRate = value;
            }
        }

        public bool Validate { get; set; }
    }
}
