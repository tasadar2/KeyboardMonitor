using System.Net;

namespace KeyboardMonitor.Network.Broadcast
{
    public class BroadcastInfo
    {
        public IPAddress Address { get; set; }
        public IPAddress ListenAddress { get; set; }
        public IPAddress BroadcastAddress { get; set; }
    }
}
