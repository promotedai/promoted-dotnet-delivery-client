// Demonstration of implementing ILog with NLog.

using NLog;

namespace Promoted.Exe
{
    public class NLogAdapter : Promoted.Lib.ILog
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public void Info(string message)
        {
            _logger.Info(message);
        }

        public void Error(string message)
        {
            _logger.Error(message);
        }
    }
}
