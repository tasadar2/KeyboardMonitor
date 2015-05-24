using System;
using System.Linq;
using System.Reflection;
using log4net;
using log4net.Config;

namespace KeyboardMonitor
{
    public class LoggerInstance
    {
        public static ILog LogWriter;

        static LoggerInstance()
        {
            LogWriter = LogManager.GetLogger(Assembly.GetExecutingAssembly().GetTypes().First());
            XmlConfigurator.Configure();
        }
    }
}
