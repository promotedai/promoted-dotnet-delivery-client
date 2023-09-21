// Interface for passing a logger into this library.

namespace Promoted.Lib
{
    public interface ILog
    {
        void Info(string message);
        void Error(string message);
    }
}
