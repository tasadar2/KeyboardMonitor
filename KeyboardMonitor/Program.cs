using Topshelf;

namespace KeyboardMonitor
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<KeyboardMonitorService>(s =>
                {
                    s.ConstructUsing(name => new KeyboardMonitorService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("Monitors computer activities to report to keyboard.");
                x.SetDisplayName("Keyboard Monitor");
                x.SetServiceName("KeyboardMonitor");
            });
        }
    }
}
