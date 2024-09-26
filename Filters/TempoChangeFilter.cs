using MonoStereo.SampleProviders;
using NAudio.Dsp;
using System.Collections.Generic;

namespace MonoStereo.Filters
{
    public class TempoChangeFilter : AudioFilter
    {
        public TempoChangeFilter(float tempo = 1f)
        {
            Tempo = tempo;
        }

        public override FilterPriority Priority => FilterPriority.ApplyLast;
        private readonly Dictionary<MonoStereoProvider, WdlResampler> resamplers = [];

        public float Tempo
        {
            get
            {
                return speed;
            }
            set
            {
                if (speed == value)
                    return;

                lock (pitchLock)
                {
                    speed = value;

                    foreach (var sampler in resamplers)
                        sampler.Value.SetRates(AudioStandards.SampleRate, AudioStandards.SampleRate / speed);

                    if (value != 0)
                        pitch = 1f / value;

                    else
                        pitch = 1f;
                }
            }
        }

        private float speed = float.NaN;
        private float pitch = float.NaN;
        private readonly object pitchLock = new();

        private float speedCache = float.NaN;
        private float pitchCache = float.NaN;

        private readonly SmbPitchShifter shifterLeft = new();
        private readonly SmbPitchShifter shifterRight = new();

        public override void Apply(MonoStereoProvider provider)
        {
            if (resamplers.ContainsKey(provider))
                return;

            var resampler = new WdlResampler();
            resampler.SetMode(true, 2, false);
            resampler.SetFilterParms();
            resampler.SetFeedMode(false); // output driven

            // Doing these in this order ensures speed is properly set with regard to race conditions
            resamplers.Add(provider, resampler);
            resampler.SetRates(AudioStandards.SampleRate, AudioStandards.SampleRate / speed);
        }

        public override void Unapply(MonoStereoProvider provider)
        {
            resamplers.Remove(provider);
        }

        public override int ModifyRead(float[] buffer, int offset, int count)
        {
            lock (pitchLock)
            {
                speedCache = speed;
                pitchCache = pitch;
            }

            if (speedCache == 1f)
                return base.ModifyRead(buffer, offset, count);

            if (speedCache == 0f)
            {
                for (int i = 0; i < count; i++)
                    buffer[offset + i] = 0f;

                return count;
            }

            if (!resamplers.TryGetValue(Source, out var resampler))
                return base.ModifyRead(buffer, offset, count);

            int framesRequested = count / AudioStandards.ChannelCount;
            int inNeeded = resampler.ResamplePrepare(framesRequested, AudioStandards.ChannelCount, out float[] inBuffer, out int inBufferOffset);

            int inAvailable = base.ModifyRead(inBuffer, inBufferOffset, inNeeded * AudioStandards.ChannelCount) / AudioStandards.ChannelCount;
            int outAvailable = resampler.ResampleOut(buffer, offset, inAvailable, framesRequested, AudioStandards.ChannelCount);

            return outAvailable * AudioStandards.ChannelCount;
        }

        public override void PostProcess(float[] buffer, int offset, int samplesRead) => PitchShiftFilter.PitchShift(pitchCache, shifterLeft, shifterRight, buffer, offset, samplesRead);
    }
}