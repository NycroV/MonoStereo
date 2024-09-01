using MonoStereo.SampleProviders;
using NAudio.Wave;
using System;
using System.Collections.Generic;

namespace MonoStereo.Filters
{
    public class StutterFilter(TimeSpan stutterLength) : AudioFilter
    {
        private TimeSpan stutterLength = stutterLength;

        private int sampleCount = (int)(stutterLength.TotalSeconds * AudioStandards.SampleRate);

        public TimeSpan StutterLength
        {
            get => stutterLength;
            set
            {
                stutterLength = value;
                sampleCount = (int)(stutterLength.TotalSeconds * AudioStandards.SampleRate);
            }
        }

        public readonly Dictionary<ISeekableSampleProvider, (int, bool, float[])> Stutters = [];

        public override int ModifyRead(float[] buffer, int offset, int count)
        {
            ISampleProvider source = Provider;

            while (source is AudioFilter filter)
                source = filter.Provider;

            if (source is not ISeekableSampleProvider provider)
                return base.ModifyRead(buffer, offset, count);

            var stutter = Stutters[provider];

            int stutterPosition = stutter.Item1;
            bool stutterFilled = stutter.Item2;
            float[] stutterBuffer = stutter.Item3;

            int samplesCopied = 0;

            do
            {
                int remainingSamples = stutterBuffer.Length - stutterPosition;
                int samplesToCopy = Math.Min(remainingSamples, count - samplesCopied);

                if (samplesToCopy > 0)
                {
                    if (!stutterFilled)
                        Provider.Read(stutterBuffer, stutterPosition, samplesToCopy);

                    Array.Copy(stutterBuffer, stutterPosition, buffer, offset + samplesCopied, samplesToCopy);
                    samplesCopied += samplesToCopy;
                    stutterPosition += samplesToCopy;
                }

                if (stutterPosition == stutterBuffer.Length)
                {
                    stutterPosition = 0;
                    stutterFilled = true;
                }
            }
            while (samplesCopied < count);

            Stutters[provider] = (stutterPosition, stutterFilled, stutterBuffer);
            return samplesCopied;
        }

        public override void Apply(MonoStereoProvider provider)
        {
            if (provider is not ISeekableSampleProvider source)
                return;

            int stutterPosition = 0;
            bool stutterFilled = false;
            float[] stutterBuffer = new float[sampleCount];

            Stutters[source] = (stutterPosition, stutterFilled, stutterBuffer);
        }

        public override void Unapply(MonoStereoProvider provider)
        {
            if (provider is not ISeekableSampleProvider source)
                return;

            if (!Stutters.TryGetValue(source, out (int, bool, float[]) stutter))
                return;

            Stutters.Remove(source);
            bool stutterFilled = stutter.Item2;

            if (!stutterFilled)
                return;

            int stutterPosition = stutter.Item1;
            float[] stutterBuffer = stutter.Item3;

            int samplesLeft = stutterBuffer.Length - stutterPosition;
            source.Position -= samplesLeft * AudioStandards.BytesPerSample;
        }
    }
}
