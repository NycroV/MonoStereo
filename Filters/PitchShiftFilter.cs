using Microsoft.Xna.Framework;
using NAudio.Dsp;
using System;

namespace MonoStereo.Filters
{
    public class PitchShiftFilter(float pitch) : AudioFilter
    {
        // Don't even worry about naming conventions here, they follow audio standards, not coding standards

        private readonly int fftSize = 4096;
        private readonly long osamp = 4L;
        private readonly SmbPitchShifter shifterLeft = new();
        private readonly SmbPitchShifter shifterRight = new();

        //Limiter constants
        const float LIM_THRESH = 0.95f;
        const float LIM_RANGE = (1f - LIM_THRESH);

        private float pitch = pitch;
        public float PitchFactor
        {
            get { return pitch; }
            set { pitch = value; }
        }

        public override void PostProcess(float[] buffer, int offset, int samplesRead)
        {
            if (pitch == 1f)
                return;

            int sampleRate = AudioStandards.StandardSampleRate;
            var left = new float[(samplesRead >> 1)];
            var right = new float[(samplesRead >> 1)];
            var index = 0;
            for (var sample = offset; sample <= samplesRead + offset - 1; sample += 2)
            {
                left[index] = buffer[sample];
                right[index] = buffer[sample + 1];
                index += 1;
            }

            shifterLeft.PitchShift(pitch, samplesRead >> 1, fftSize, osamp, sampleRate, left);
            shifterRight.PitchShift(pitch, samplesRead >> 1, fftSize, osamp, sampleRate, right);
            index = 0;

            for (var sample = offset; sample <= samplesRead + offset - 1; sample += 2)
            {
                buffer[sample] = Limiter(left[index]);
                buffer[sample + 1] = Limiter(right[index]);
                index += 1;
            }
        }

        private static float Limiter(float sample)
        {
            float res;
            if ((LIM_THRESH < sample))
            {
                res = (sample - LIM_THRESH) / LIM_RANGE;
                res = (float)((Math.Atan(res) / MathHelper.PiOver2) * LIM_RANGE + LIM_THRESH);
            }
            else if ((sample < -LIM_THRESH))
            {
                res = -(sample + LIM_THRESH) / LIM_RANGE;
                res = -(float)((Math.Atan(res) / MathHelper.PiOver2) * LIM_RANGE + LIM_THRESH);
            }
            else
            {
                res = sample;
            }
            return res;
        }
    }
}
