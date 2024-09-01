using System;
using System.Collections.Generic;
using System.Linq;

namespace MonoStereo.Encoding
{
    public static class LoopParser
    {
        public static void ParseLoop(this IDictionary<string, string> comments, out long loopStart, out long loopEnd)
        {
            // Check for looping tags
            string loopStartKey = comments.Keys.FirstOrDefault(k => k.Equals("LOOPSTART", StringComparison.OrdinalIgnoreCase), null);
            if (loopStartKey is not null)
                loopStart = long.Parse(comments[loopStartKey]);

            else
                loopStart = -1;

            string loopEndKey = comments.Keys.FirstOrDefault(k => k.Equals("LOOPEND", StringComparison.OrdinalIgnoreCase), null);
            if (loopEndKey is not null)
                loopEnd = long.Parse(comments[loopEndKey]);

            else
            {
                string loopLengthKey = comments.Keys.FirstOrDefault(k => k.Equals("LOOPLENGTH", StringComparison.OrdinalIgnoreCase), null);
                if (loopLengthKey is not null)
                {
                    long looplength = long.Parse(comments[loopLengthKey]);
                    loopEnd = loopStart + looplength;
                }

                else
                    loopEnd = -1;
            }
        }
    }
}
