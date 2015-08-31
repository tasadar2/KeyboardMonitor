using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MiscUtil.Conversion;

namespace KeyboardMonitor
{
    public class Communicator
    {
        public const int DiscoverPort = 27831;
        private const int MaxDataSize = 512;
        private const short EndCommand = 0x512B;
        private readonly byte[] _endCommandBytes = EndianBitConverter.Big.GetBytes(EndCommand);

        public delegate void DataReceivedDelegate(object sender, DataReceivedEventArgs e);
        public event DataReceivedDelegate DataReceived;

        public delegate void EndpointDiscoveredDelegate(object sender, EndpointDiscoveredEventArgs e);
        public event EndpointDiscoveredDelegate EndpointDiscovered;

        public Dictionary<uint, Message> Messages = new Dictionary<uint, Message>();
        public Dictionary<ulong, IPEndPoint> Endpoints = new Dictionary<ulong, IPEndPoint>();

        private byte[] _listenPortBytes;
        private int _listenPort;
        public int ListenPort
        {
            get
            {
                return _listenPort;
            }
            private set
            {
                _listenPort = value;
                _listenPortBytes = EndianBitConverter.Big.GetBytes((short)value);
            }
        }

        private Socket _listenerSocket;

        public Communicator(int port = 0)
        {
            var data = new SocketData();
            data.Socket.Bind(new IPEndPoint(IPAddress.Any, port));
            StartReceive(data);
            _listenerSocket = data.Socket;
            ListenPort = ((IPEndPoint)data.Socket.LocalEndPoint).Port;
            LoggerInstance.LogWriter.DebugFormat("Listening on {0}", data.Socket.LocalEndPoint);
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
                    var content = GenerateCommand(MessageType.Discover);
                    LoggerInstance.LogWriter.DebugFormat("Sending Discover to {0}", new IPEndPoint(broadcastInfo.BroadcastAddress, port));
                    Send(socket, content, new IPEndPoint(broadcastInfo.BroadcastAddress, port));
                }
            }
        }

        public void Subscribe(IPAddress remoteIpAddress, IPAddress ipAddress, int port)
        {
            var content = GenerateCommand(MessageType.Subscribe);
            LoggerInstance.LogWriter.DebugFormat("Sending Subscribe to {0}", new IPEndPoint(ipAddress, port));
            Send(content, new IPEndPoint(ipAddress, port));
        }

        public void Unsubscribe(IPAddress remoteIpAddress, IPAddress ipAddress, int port)
        {
            var content = GenerateCommand(MessageType.Subscribe);
            LoggerInstance.LogWriter.DebugFormat("Sending Unsubscribe to {0}", new IPEndPoint(ipAddress, port));
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
                        var payload = MessageType.Message.Bytes
                                                 .Concat(EndianBitConverter.Big.GetBytes(messageId))
                                                 .Concat(EndianBitConverter.Big.GetBytes(contentBytes.Length))
                                                 .Concat(EndianBitConverter.Big.GetBytes((ushort)parts.Count))
                                                 .Concat(EndianBitConverter.Big.GetBytes((ushort)part))
                                                 .Concat(EndianBitConverter.Big.GetBytes(index))
                                                 .Concat(EndianBitConverter.Big.GetBytes((ushort)partBytes.Length))
                                                 .Concat(partBytes)
                                                 .Concat(_endCommandBytes)
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

        private byte[] GenerateCommand(MessageType messageType)//, IPAddress remoteIpAddress)
        {
            using (var writer = new MemoryStream(10))
            {
                writer.Write(messageType.Bytes, 0, 2);
                writer.Write(new byte[] { 0, 0, 0, 0 }, 0, 4);
                //writer.Write(remoteIpAddress.GetAddressBytes(), 0, 4);
                writer.Write(_listenPortBytes, 0, 2);
                writer.Write(_endCommandBytes, 0, 2);
                return writer.ToArray();
            }
        }

        private static IPEndPoint GetEndpoint(IEnumerable<byte> endpointBytes)
        {
            var byteArray = endpointBytes.ToArray();
            return new IPEndPoint(new IPAddress(BitConverter.ToUInt32(byteArray, 0)), EndianBitConverter.Big.ToUInt16(byteArray, 4));
        }

        private void ProcessData(IPEndPoint remoteEndpoint, byte[] data, int length)
        {
            if (length > 2)
            {
                ulong hash;
                var address = remoteEndpoint.Address;
                var port = EndianBitConverter.Big.ToUInt16(data, 6);
                var endpoint = new IPEndPoint(address, port);
                var command = data.SubArray(0, 2);

                // Discover
                // FF 15		Start
                // FF FF FF FF  IP Address
                // FF FF        Port
                // 51 2B		End
                if (command.ArrayEquals(MessageType.Discover.Bytes))
                {
                    LoggerInstance.LogWriter.DebugFormat("Received Discover on {0}", remoteEndpoint);

                    var content = GenerateCommand(MessageType.Discovered);
                    LoggerInstance.LogWriter.DebugFormat("Sending Discovered to {0}", endpoint);
                    Send(content, endpoint);
                }

                // Discovered
                // FF 16		Start
                // FF FF FF FF  IP Address
                // FF FF        Port
                // 51 2B		End
                else if (command.ArrayEquals(MessageType.Discovered.Bytes))
                {
                    LoggerInstance.LogWriter.DebugFormat("Received Discovered on {0}", remoteEndpoint);
                    EndpointDiscovered.BeginInvoke(this, new EndpointDiscoveredEventArgs(address, address, port), null, null);
                }

                // Subscribe
                // FF 17		Start
                // FF FF FF FF  IP Address
                // FF FF        Port
                // 51 2B		End
                else if (command.ArrayEquals(MessageType.Subscribe.Bytes))
                {
                    LoggerInstance.LogWriter.DebugFormat("Received Subscribe on {0}", remoteEndpoint);
                    hash = BitConverter.ToUInt64(address.GetAddressBytes()
                                                         .Concat(BitConverter.GetBytes(port).Take(2))
                                                         .Concat(new byte[2]).ToArray(), 0);
                    Endpoints[hash] = endpoint;
                }

                // Unsubscribe
                // FF 18		Start
                // FF FF FF FF  IP Address
                // FF FF        Port
                // 51 2B		End
                else if (command.ArrayEquals(MessageType.Unsubscribe.Bytes))
                {
                    LoggerInstance.LogWriter.DebugFormat("Received UnSubscribe on {0}", remoteEndpoint);
                    hash = BitConverter.ToUInt64(address.GetAddressBytes()
                                                         .Concat(BitConverter.GetBytes(port).Take(2))
                                                         .Concat(new byte[2]).ToArray(), 0);
                    Endpoints.Remove(hash);
                }

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
                else if (command.ArrayEquals(MessageType.Message.Bytes))
                {
                    BuildReceivedMessage(data, length);
                }
            }
        }

        private void BuildReceivedMessage(byte[] data, int length)
        {
            if (length > 20)
            {
                var messageIdentifier = EndianBitConverter.Big.ToUInt32(data, 0x2);
                var messageLength = EndianBitConverter.Big.ToUInt32(data, 0x6);
                var messageParts = EndianBitConverter.Big.ToUInt16(data, 0xa);
                var messagePart = EndianBitConverter.Big.ToUInt16(data, 0xc);
                var messagePartStart = EndianBitConverter.Big.ToUInt32(data, 0xe);
                var messagePartLength = EndianBitConverter.Big.ToUInt16(data, 0x12);

                if (length >= 0x14 + messagePartLength && data.SubArray(0x14 + messagePartLength, 2).ArrayEquals(_endCommandBytes))
                {
                    Message message;
                    if (!Messages.TryGetValue(messageIdentifier, out message))
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