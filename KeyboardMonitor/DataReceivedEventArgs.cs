using System;

namespace KeyboardMonitor
{
    public class DataReceivedEventArgs : EventArgs
    {
        public uint Identifier { get; set; }
        public byte[] Content { get; set; }
        public uint Length { get; set; }

        public DataReceivedEventArgs(uint identifier, uint length)
        {
            Identifier = identifier;
            Content = new byte[length];
            Length = length;
        }
    }
}