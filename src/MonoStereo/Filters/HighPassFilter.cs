using MonoStereo.Structures;
using NAudio.Dsp;
using System.Collections.Generic;

namespace MonoStereo.Filters
{
    public class HighPassFilter(float cutoffFrequency = 100f, float q = 0.7f) : AudioFilter
    {
        private readonly Dictionary<MonoStereoProvider, BiQuadFilter> filters = [];
        private readonly QueuedLock filterLock = new();

        public float CutoffFrequency
        {
            get => _cutoffFrequency;
            set
            {
                filterLock.Execute(() =>
                {
                    if (value > 0)
                    {
                        foreach (var filter in filters.Values)
                            filter.SetHighPassFilter(AudioStandards.SampleRate, value, Q);
                    }

                    _cutoffFrequency = value;
                });
            }
        }

        public float Q
        {
            get => _q;
            set
            {
                filterLock.Execute(() =>
                {
                    foreach (var filter in filters.Values)
                        filter.SetHighPassFilter(AudioStandards.SampleRate, CutoffFrequency, value);

                    _q = value;
                });
            }
        }


        private float _cutoffFrequency = cutoffFrequency;
        private float _q = q;

        public override void Apply(MonoStereoProvider provider) => filterLock.Execute(() => filters.Add(provider, BiQuadFilter.HighPassFilter(AudioStandards.SampleRate, _cutoffFrequency, _q)));

        public override void Unapply(MonoStereoProvider provider) => filterLock.Execute(() => filters.Remove(provider));

        public override void PostProcess(float[] buffer, int offset, int samplesRead)
        {
            if (_cutoffFrequency <= 0)
                return;

            var filter = filters[Source];

            for (int i = 0; i < samplesRead; i++)
                buffer[offset + i] = filter.Transform(buffer[offset + i]);
        }
    }
}
