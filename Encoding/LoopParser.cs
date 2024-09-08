using System;
using System.Collections.Generic;
using System.Linq;

namespace MonoStereo.Encoding
{
    public static class LoopParser
    {
        public static void ParseLoop(this IDictionary<string, string> comments, out long loopStart, out long loopEnd, int channels)
        {
            // Check for looping tags
            string loopStartKey = comments.Keys.FirstOrDefault(k => k.Equals("LOOPSTART", StringComparison.OrdinalIgnoreCase), null);
            if (loopStartKey is not null)
                loopStart = long.Parse(comments[loopStartKey]) * channels;

            else
                loopStart = -1;

            string loopEndKey = comments.Keys.FirstOrDefault(k => k.Equals("LOOPEND", StringComparison.OrdinalIgnoreCase), null);
            if (loopEndKey is not null)
                loopEnd = long.Parse(comments[loopEndKey]) * channels;

            else
            {
                string loopLengthKey = comments.Keys.FirstOrDefault(k => k.Equals("LOOPLENGTH", StringComparison.OrdinalIgnoreCase), null);
                if (loopLengthKey is not null)
                {
                    long looplength = long.Parse(comments[loopLengthKey]);
                    loopEnd = loopStart + (looplength * channels);
                }

                else
                    loopEnd = -1;
            }
        }

        public static Dictionary<string, string> ComposeComments(this IReadOnlyDictionary<string, IReadOnlyList<string>> comments) => comments.ToDictionary(c => c.Key, c => c.Value.FirstOrDefault(string.Empty));
    }
}
