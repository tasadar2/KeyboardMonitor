using System.Runtime.InteropServices;

namespace KeyboardMonitor.Gathering.FrameRate
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct FrapsData
    {
        [MarshalAs(UnmanagedType.U4)]
        public uint StructSize;
        [MarshalAs(UnmanagedType.U4)]
        public uint FramesPerSecond;
        [MarshalAs(UnmanagedType.U4)]
        public uint TotalFrames;
        [MarshalAs(UnmanagedType.U4)]
        public uint TimeOfLastFrame;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string GameName;

        [MarshalAs(UnmanagedType.U4)]
        public uint unknown1;
        [MarshalAs(UnmanagedType.U4)]
        public uint unknown2;
        [MarshalAs(UnmanagedType.U4)]
        public uint unknown3;
        [MarshalAs(UnmanagedType.U4)]
        public uint unknown4;
        [MarshalAs(UnmanagedType.U4)]
        public uint ResolutionX;
        [MarshalAs(UnmanagedType.U4)]
        public uint ResolutionY;
        [MarshalAs(UnmanagedType.U4)]
        public uint unknown7;
    };
}