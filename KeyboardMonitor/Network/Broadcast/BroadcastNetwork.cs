using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace KeyboardMonitor.Network.Broadcast
{
    public static class BroadcastNetwork
    {
        public static IEnumerable<BroadcastInfo> GetBroadcastInformation()
        {
            var info = new List<BroadcastInfo>();
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces().Where(ni => ni.OperationalStatus == OperationalStatus.Up))
            {
                foreach (var unicastInfo in networkInterface.GetIPProperties().UnicastAddresses)
                {
                    if (unicastInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        var address = BitConverter.ToUInt32(unicastInfo.Address.GetAddressBytes(), 0);
                        var mask = BitConverter.ToUInt32(unicastInfo.IPv4Mask.GetAddressBytes(), 0);

                        info.Add(new BroadcastInfo
                        {
                            Address = unicastInfo.Address,
                            ListenAddress = new IPAddress(address & mask),
                            BroadcastAddress = new IPAddress(address | (~mask)),
                        });
                    }
                }
            }

            return info;
        }
    }
}
