using System;
using System.Collections.Generic;

namespace FaceCord.Common
{
    public static class StringExtensions
    {
        public static IEnumerable<string> ChunksUpTo(this string str, int maxChunkSize)
        {
            for (var i = 0; i < str.Length; i += maxChunkSize)
                yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
        }
    }
}
