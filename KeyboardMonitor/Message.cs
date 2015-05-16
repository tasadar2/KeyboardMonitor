using System.Collections.Generic;

namespace KeyboardMonitor
{
    public class Message : DataReceivedEventArgs
    {
        public readonly HashSet<uint> Parts;

        public Message(uint identifier, uint length, ushort parts)
            : base(identifier, length)
        {
            Parts = new HashSet<uint>();
            for (ushort part = 0; part < parts; part++)
            {
                Parts.Add(part);
            }
        }
    }
}