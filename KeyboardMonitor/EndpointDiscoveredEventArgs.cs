using System;
using System.Net;

namespace KeyboardMonitor
{
    public class EndpointDiscoveredEventArgs : EventArgs
    {
        public IPAddress IpAddress { get; set; }
        public int Port { get; set; }

        public EndpointDiscoveredEventArgs(IPAddress ipaddress, int port)
        {
            IpAddress = ipaddress;
            Port = port;
        }
    }
}
