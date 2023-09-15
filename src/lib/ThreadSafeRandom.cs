// Random isn't thread-safe so we need to use per-thread instances. But if we do a simple ThreadLocal<Random>,
// instances created within ~15ms of each other will all have the same seed. Here we use a shared Random to
// uniquely seed each of the per-thread instances, and only pay the synchronization cost at construction time.
//
// .NET 6 has a built-in thread-safe Random impl, but that isn't our target.
// This is copied roughly from https://stackoverflow.com/a/57962385

namespace Promoted.Lib
{
    public static class ThreadSafeRandom
    {
        private static readonly System.Random _globalRandom = new Random();
        private static readonly ThreadLocal<Random> _localRandom = new ThreadLocal<Random>(() =>
        {
            lock (_globalRandom)
            {
                return new Random(_globalRandom.Next());
            }
        });

        public static float NextFloat()
        {
            return (float)_localRandom.Value.NextDouble();
        }
    }
}
