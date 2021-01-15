using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BigMohammadBot
{
    public static class Extensions
    {
        public static ulong ToInt64(this byte[] Bytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                byte[] Reversed = Enumerable.Reverse(Bytes).ToArray();
                return BitConverter.ToUInt64(Reversed, 0);
            }
            else
                return BitConverter.ToUInt64(Bytes, 0);
        }

        public static byte[] ToByteArray(this ulong Num)
        {
            byte[] ReturnValue = BitConverter.GetBytes(Num);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(ReturnValue);
            return ReturnValue;
        }
    }
}
