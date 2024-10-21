using MonoStereo.Structures;
using NAudio.Dsp;
using System;
using System.Collections.Generic;

namespace MonoStereo.Filters
{
    public class PitchShiftFilter(float pitch) : AudioFilter
    {
        // PitchShiftFilter.Set
        public class Set
        {
            public readonly SmbPitchShifter Left = new();
            public readonly SmbPitchShifter Right = new();
        }

        // Pitch shifters
        public readonly Dictionary<MonoStereoProvider, Set> FilterSets = [];

        //Limiter constants
        internal const float LIM_THRESH = 0.95f;
        internal const float LIM_RANGE = 1f - LIM_THRESH;
        internal const float PiOver2 = 1.57079637f;

        internal const int fftSize = 4096;
        internal const long osamp = 4L;

        private float pitch = pitch;
        public float PitchFactor
        {
            get { return pitch; }
            set { pitch = value; }
        }

        public override void Apply(MonoStereoProvider provider) => FilterSets.Add(provider, new());

        public override void Unapply(MonoStereoProvider provider) => FilterSets.Remove(provider);

        public override void PostProcess(float[] buffer, int offset, int samplesRead) => PitchShift(pitch, FilterSets[Source], buffer, offset, samplesRead);

        public static void PitchShift(float pitch, Set filterSet, float[] buffer, int offset, int samplesRead)
        {
            if (pitch == 1f)
                return;

            int sampleRate = AudioStandards.SampleRate;
            var left = new float[(samplesRead >> 1)];
            var right = new float[(samplesRead >> 1)];
            var index = 0;

            for (var sample = offset; sample <= samplesRead + offset - 1; sample += 2)
            {
                left[index] = buffer[sample];
                right[index] = buffer[sample + 1];
                index += 1;
            }

            filterSet.Left.PitchShift(pitch, samplesRead >> 1, fftSize, osamp, sampleRate, left);
            filterSet.Right.PitchShift(pitch, samplesRead >> 1, fftSize, osamp, sampleRate, right);
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
            if (LIM_THRESH < sample)
            {
                res = (sample - LIM_THRESH) / LIM_RANGE;
                res = (float)(Math.Atan(res) / PiOver2 * LIM_RANGE + LIM_THRESH);
            }
            else if (sample < -LIM_THRESH)
            {
                res = -(sample + LIM_THRESH) / LIM_RANGE;
                res = -(float)(Math.Atan(res) / PiOver2 * LIM_RANGE + LIM_THRESH);
            }
            else
            {
                res = sample;
            }
            return res;
        }
    }
}
