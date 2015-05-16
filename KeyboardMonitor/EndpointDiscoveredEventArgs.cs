using System;
using System.Net;

namespace KeyboardMonitor
{
    public class EndpointDiscoveredEventArgs : EventArgs
    {
        public IPAddress RemoteIpAddress { get; set; }
        public IPAddress IpAddress { get; set; }
        public int Port { get; set; }

        public EndpointDiscoveredEventArgs(IPAddress remoteIpAddress, IPAddress ipaddress, int port)
        {
            IpAddress = ipaddress;
            Port = port;
            RemoteIpAddress = remoteIpAddress;
        }
    }
}
