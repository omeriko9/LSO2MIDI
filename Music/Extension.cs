﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvertLSO
{
    public static class Extension
    {
        public static byte[] SubPattern(this byte[] arr, int StartIndex, params int[] Patterns)
        {
            var toReturn = new byte[0];

            for (var i = StartIndex; i < arr.Length; i += 1)
            {
                int toCompare = BitConverter.ToInt32(arr.Skip(i).Take(4).ToArray(), 0);
                foreach (var pat in Patterns)
                    if (toCompare == pat)
                        return arr.Skip(StartIndex).Take(i - StartIndex).ToArray();
            }

            return toReturn;
        }

        public static int FindPattern(this byte[] arr, int StartIndex, params int[] Patterns)
        {
           
            for (var i = StartIndex; i < arr.Length-4; i += 1)
            {
                int toCompare = BitConverter.ToInt32(arr.Skip(i).Take(4).ToArray(), 0);
                foreach (var pat in Patterns)
                    if (toCompare == pat)
                        return i;
            }

            return -1;
        }

        /// <summary>
        /// Helper methods for the lists.
        /// </summary>
        /// https://stackoverflow.com/a/24087164

        public static List<List<T>> ChunkBy<T>(this List<T> source, int chunkSize)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }


        public static int getInt(this byte[] arr, int offset)
        {
            return BitConverter.ToInt32(arr.Skip(offset).Take(4).ToArray(), 0);
        }


        public static int ToUInt24(this byte[] posArr, int offset)
        {
            var posInt = posArr.getInt(offset);
            if ((posInt & 0x00800000) > 0)
            {
                posInt = (int)((uint)posInt | (uint)0xFF000000);
            }

            return posInt;
        }
    }
}
