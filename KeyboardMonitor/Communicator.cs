using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace KeyboardMonitor
{
    public enum MessageType : ushort
    {
        Discover = 0xff15,
        Discovered = 0xff16,
        Subscribe = 0xff17,
        Unsubscribe = 0xff18,
        Message = 0xff19,
    }

    public class Communicator
    {
        public const int DiscoverPort = 27831;
        private const int MaxDataSize = 512;
        private const ushort EndMessage = 0x512B;

        public delegate void DataReceivedDelegate(object sender, DataReceivedEventArgs e);
        public event DataReceivedDelegate DataReceived;

        public delegate void EndpointDiscoveredDelegate(object sender, EndpointDiscoveredEventArgs e);
        public event EndpointDiscoveredDelegate EndpointDiscovered;

        public Dictionary<uint, Message> Messages = new Dictionary<uint, Message>();
        public Dictionary<ulong, IPEndPoint> Endpoints = new Dictionary<ulong, IPEndPoint>();

        public int ListenPort { get; private set; }
        private Socket _listenerSocket;

        public Communicator(int port = 0)
        {
            var data = new SocketData();
            data.Socket.Bind(new IPEndPoint(IPAddress.Any, port));
            StartReceive(data);
            _listenerSocket = data.Socket;
            ListenPort = ((IPEndPoint)data.Socket.LocalEndPoint).Port;
        }

        public void Close()
        {
            if (_listenerSocket != null)
            {
                _listenerSocket.Close();
                _listenerSocket = null;
            }
        }

        public void Discover(int port)
        {
            using (var socket = SocketData.CreateSocket())
            {
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);

                foreach (var broadcastInfo in GetBroadcastInformation())
                {
                    var content = GenerateCommand(MessageType.Discover, broadcastInfo.Address);
                    Send(socket, content, new IPEndPoint(broadcastInfo.BroadcastAddress, port));
                }
            }
        }

        public void Subscribe(IPAddress remoteIpAddress, IPAddress ipAddress, int port)
        {
            var content = GenerateCommand(MessageType.Subscribe, remoteIpAddress);
            Send(content, new IPEndPoint(ipAddress, port));
        }

        public void Unsubscribe(IPAddress remoteIpAddress, IPAddress ipAddress, int port)
        {
            var content = GenerateCommand(MessageType.Subscribe, remoteIpAddress);
            Send(content, new IPEndPoint(ipAddress, port));
        }

        //  FF 19		Start
        //  FF FF FF FF	Message Identifier
        //  FF FF FF FF	Message Length
        //  FF FF		Message Parts
        //  FF FF		Message Part
        //  FF FF FF FF	Message Part Start
        //  FF FF		Message Part Length
        //  FF .. .. FF	Message Part Content
        //  51 2B		End
        public void SendToSubscribers(string content)
        {
            if (Endpoints.Any())
            {
                var contentBytes = Encoding.UTF8.GetBytes(content);
                var parts = new List<byte[]>();
                const int maxMessageSize = MaxDataSize - 22;
                var index = 0;
                while (index < contentBytes.Length)
                {
                    byte[] tempArray;
                    if (index + maxMessageSize >= contentBytes.Length)
                    {
                        tempArray = new byte[contentBytes.Length - index];
                        Array.Copy(contentBytes, index, tempArray, 0, contentBytes.Length - index);
                    }
                    else
                    {
                        tempArray = new byte[maxMessageSize];
                        Array.Copy(contentBytes, index, tempArray, 0, maxMessageSize);
                    }

                    parts.Add(tempArray);

                    index += maxMessageSize;
                }

                using (var socket = SocketData.CreateSocket())
                {
                    var messageId = Interlocked.Increment(ref _messageId);
                    index = 0;
                    var part = 0;
                    foreach (var partBytes in parts)
                    {
                        var payload = BitConverter.GetBytes((ushort)MessageType.Message)
                                                  .Concat(BitConverter.GetBytes(messageId))
                                                  .Concat(BitConverter.GetBytes(contentBytes.Length))
                                                  .Concat(BitConverter.GetBytes((ushort)parts.Count))
                                                  .Concat(BitConverter.GetBytes((ushort)part))
                                                  .Concat(BitConverter.GetBytes(index))
                                                  .Concat(BitConverter.GetBytes((ushort)partBytes.Length))
                                                  .Concat(partBytes)
                                                  .Concat(BitConverter.GetBytes(EndMessage))
                                                  .ToArray();

                        foreach (var endpoint in Endpoints.Values)
                        {
                            Send(socket, payload, endpoint);
                        }

                        part++;
                        index += partBytes.Length;
                    }
                }
            }
        }

        private int _messageId;

        private void Send(byte[] content, EndPoint endpoint)
        {
            using (var socket = SocketData.CreateSocket())
            {
                Send(socket, content, endpoint);
            }
        }

        private void Send(Socket socket, byte[] content, EndPoint endpoint)
        {
            socket.BeginSendTo(content, 0, content.Length, SocketFlags.None, endpoint, null, null);
        }

        private IEnumerable<BroadcastInfo> GetBroadcastInformation()
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

        public class BroadcastInfo
        {
            public IPAddress Address { get; set; }
            public IPAddress ListenAddress { get; set; }
            public IPAddress BroadcastAddress { get; set; }
        }

        private void StartReceive(SocketData data)
        {
            data.Socket.BeginReceiveFrom(data.Buffer, 0, data.BufferSize, SocketFlags.None, ref data.RemoteEndpoint, ReceiveCallback, data);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            var data = (SocketData)ar.AsyncState;
            try
            {
                var bytesReceived = data.Socket.EndReceiveFrom(ar, ref data.RemoteEndpoint);

                if (bytesReceived > 0)
                {
                    ProcessData((IPEndPoint)data.RemoteEndpoint, data.Buffer, bytesReceived);
                }

                StartReceive(data);
            }
            catch (ObjectDisposedException)
            { }
        }

        private byte[] GenerateCommand(MessageType messageType, IPAddress remoteIpAddress)
        {
            return BitConverter.GetBytes((ushort)messageType)
                               .Concat(remoteIpAddress.GetAddressBytes())
                               .Concat(BitConverter.GetBytes((ushort)ListenPort))
                               .Concat(BitConverter.GetBytes(EndMessage))
                               .ToArray();
        }

        private static IPEndPoint GetEndpoint(IEnumerable<byte> endpointBytes)
        {
            var byteArray = endpointBytes.ToArray();
            return new IPEndPoint(new IPAddress(BitConverter.ToUInt32(byteArray, 0)), BitConverter.ToUInt16(byteArray, 4));
        }

        private void ProcessData(IPEndPoint remoteEndpoint, byte[] data, int length)
        {
            if (length > 2)
            {
                ulong hash;
                IPEndPoint endpoint;
                switch ((MessageType)BitConverter.ToUInt16(data, 0))
                {
                    // Discover
                    // FF 15		Start
                    // FF FF FF FF  IP Address
                    // FF FF        Port
                    // 51 2B		End
                    case MessageType.Discover:
                        endpoint = GetEndpoint(data.Skip(2).Take(6));

                        var content = GenerateCommand(MessageType.Discovered, remoteEndpoint.Address);
                        Send(content, endpoint);
                        break;

                    // Discovered
                    // FF 16		Start
                    // FF FF FF FF  IP Address
                    // FF FF        Port
                    // 51 2B		End
                    case MessageType.Discovered:
                        endpoint = GetEndpoint(data.Skip(2).Take(6));
                        EndpointDiscovered.BeginInvoke(this, new EndpointDiscoveredEventArgs(remoteEndpoint.Address, endpoint.Address, endpoint.Port), null, null);
                        break;

                    // Subscribe
                    // FF 17		Start
                    // FF FF FF FF  IP Address
                    // FF FF        Port
                    // 51 2B		End
                    case MessageType.Subscribe:
                        endpoint = GetEndpoint(data.Skip(2).Take(6));
                        hash = BitConverter.ToUInt64(endpoint.Address.GetAddressBytes()
                                                                .Concat(BitConverter.GetBytes(endpoint.Port).Take(2))
                                                                .Concat(new byte[2]).ToArray(), 0);
                        Endpoints[hash] = endpoint;

                        break;

                    // Unsubscribe
                    // FF 18		Start
                    // FF FF FF FF  IP Address
                    // FF FF        Port
                    // 51 2B		End
                    case MessageType.Unsubscribe:
                        endpoint = GetEndpoint(data.Skip(2).Take(6));
                        hash = BitConverter.ToUInt64(endpoint.Address.GetAddressBytes()
                                                           .Concat(BitConverter.GetBytes(endpoint.Port).Take(2))
                                                           .Concat(new byte[2]).ToArray(), 0);
                        Endpoints.Remove(hash);

                        break;

                    //  Message
                    //  FF 19		Start
                    //  FF FF FF FF	Message Identifier
                    //  FF FF FF FF	Message Length
                    //  FF FF		Message Parts
                    //  FF FF		Message Part
                    //  FF FF FF FF	Message Part Start
                    //  FF FF		Message Part Length
                    //  FF .. .. FF	Message Part Content
                    //  51 2B		End
                    case MessageType.Message:
                        BuildReceivedMessage(data, length);
                        break;
                }
            }
        }

        private void BuildReceivedMessage(byte[] data, int length)
        {
            if (length > 20)
            {
                var messageIdentifier = BitConverter.ToUInt32(data, 0x2);
                var messageLength = BitConverter.ToUInt32(data, 0x6);
                var messageParts = BitConverter.ToUInt16(data, 0xa);
                var messagePart = BitConverter.ToUInt16(data, 0xc);
                var messagePartStart = BitConverter.ToUInt32(data, 0xe);
                var messagePartLength = BitConverter.ToUInt16(data, 0x12);

                if (length >= 0x14 + messagePartLength && BitConverter.ToUInt16(data, 0x14 + messagePartLength) == EndMessage)
                {
                    Message message;
                    if (Messages.ContainsKey(messageIdentifier))
                    {
                        message = Messages[messageIdentifier];
                    }
                    else
                    {
                        Messages.Add(messageIdentifier, message = new Message(messageIdentifier, messageLength, messageParts));
                    }

                    message.Parts.Remove(messagePart);
                    Array.Copy(data, 0x14, message.Content, messagePartStart, messagePartLength);

                    if (message.Parts.Count == 0)
                    {
                        Messages.Remove(messageIdentifier);
                        DataReceived.BeginInvoke(this, message, null, null);
                    }
                }
            }
        }

    }
}