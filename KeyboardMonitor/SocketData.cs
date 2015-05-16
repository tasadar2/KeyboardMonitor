using System.Net;
using System.Net.Sockets;

namespace KeyboardMonitor
{
    public class SocketData
    {
        public readonly Socket Socket;
        public byte[] Buffer;
        public int BufferSize;
        public EndPoint RemoteEndpoint;

        public SocketData(Socket socket = null, int bufferSize = 512)
        {
            Socket = socket ?? CreateSocket();
            BufferSize = bufferSize;
            Buffer = new byte[BufferSize];
            RemoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
        }

        public static Socket CreateSocket()
        {
            return new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }
    }
}