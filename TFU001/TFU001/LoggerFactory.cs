using System;
using System.Linq;
using log4net;

namespace TFU001
{
    public static class LoggerFactory
    {
        public static ILog GetLogger()
        {
            return LogManager.GetLogger(Constants.LoggingRepositoryName);
        }
    }
}