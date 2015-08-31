using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using KeyboardMonitor;
using Newtonsoft.Json;
using Topshelf;

namespace Tester
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<TestService>(s =>
                {
                    s.ConstructUsing(name => new TestService());
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

    public class TestService
    {
        private static Communicator _communicator;

        public void Start()
        {
            _communicator = new Communicator();
            Thread.Sleep(5000);
            _communicator.EndpointDiscovered += _communicator_EndpointDiscovered;
            _communicator.DataReceived += _communicator_DataReceived;
            _communicator.Discover(Communicator.DiscoverPort);
        }

        void _communicator_DataReceived(object sender, DataReceivedEventArgs e)
        {
            LoggerInstance.LogWriter.Info(Encoding.UTF8.GetString(e.Content));
        }

        public void Stop()
        {

        }

        private void _communicator_EndpointDiscovered(object sender, EndpointDiscoveredEventArgs e)
        {
            _communicator.Subscribe(e.IpAddress, e.Port);
        }
    }
}
