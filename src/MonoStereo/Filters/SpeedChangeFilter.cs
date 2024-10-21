using MonoStereo.Structures;
using NAudio.Dsp;
using System.Collections.Generic;

namespace MonoStereo.Filters
{
    public class SpeedChangeFilter(float speed = 1f) : AudioFilter
    {
        public override FilterPriority Priority => FilterPriority.ApplyLast;
        private readonly Dictionary<MonoStereoProvider, WdlResampler> resamplers = [];

        private float _speed = speed;
        public float Speed
        {
            get
            {
                return _speed;
            }
            set
            {
                if (_speed == value)
                    return;

                _speed = value;

                foreach (var sampler in resamplers)
                    sampler.Value.SetRates(AudioStandards.SampleRate, AudioStandards.SampleRate / _speed);
            }
        }

        public override void Apply(MonoStereoProvider provider)
        {
            var resampler = new WdlResampler();

            resampler.SetMode(true, 2, false);
            resampler.SetFilterParms();
            resampler.SetFeedMode(false); // output driven
            resampler.SetRates(AudioStandards.SampleRate, AudioStandards.SampleRate / _speed);

            resamplers.Add(provider, resampler);
        }

        public override void Unapply(MonoStereoProvider provider)
        {
            resamplers.Remove(provider);
        }

        public override int ModifyRead(float[] buffer, int offset, int count)
        {
            if (_speed == 1f)
                return base.ModifyRead(buffer, offset, count);

            if (_speed == 0f)
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

        public override void Dispose()
        {
            resamplers.Clear();
            base.Dispose();
        }
    }
}