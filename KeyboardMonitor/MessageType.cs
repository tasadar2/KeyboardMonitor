using System;
using System.Collections.Generic;
using System.Linq;
using MiscUtil.Conversion;

namespace KeyboardMonitor
{
    //public class MessageType
    //{
    //    public static readonly MessageType Discover = new MessageType(0xff15);
    //    public static readonly MessageType Discovered = new MessageType(0xff16);
    //    public static readonly MessageType Subscribe = new MessageType(0xff17);
    //    public static readonly MessageType Unsubscribe = new MessageType(0xff18);
    //    public static readonly MessageType Message = new MessageType(0xff19);

    //    public byte[] Bytes { get; }

    //    public MessageType(int key)
    //    {
    //        Bytes = EndianBitConverter.Big.GetBytes((ushort)key);
    //    }
    //}

    //public static class LackOfABetterName
    //{
    //    public static TType[] SubArray<TType>(this TType[] array, int index, int length)
    //    {
    //        var result = new TType[length];
    //        Array.Copy(array, index, result, 0, length);
    //        return result;
    //    }

    //    public static bool ArrayEquals<TType>(this TType[] array1, TType[] array2)
    //    {
    //        if (ReferenceEquals(array1, array2))
    //            return true;

    //        if (array1 == null || array2 == null)
    //            return false;

    //        if (array1.Length != array2.Length)
    //            return false;

    //        var comparer = EqualityComparer<TType>.Default;
    //        return !array1.Where((t, i) => !comparer.Equals(t, array2[i])).Any();
    //    }

    //}

    public enum MessageType : ushort
    {
        Discover = 0xff15,
        Discovered = 0xff16,
        Subscribe = 0xff17,
        Unsubscribe = 0xff18,
        Message = 0xff19,
    }

}

