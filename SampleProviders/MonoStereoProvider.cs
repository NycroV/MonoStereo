using MonoStereo.Filters;
using NAudio.Wave;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MonoStereo.SampleProviders
{
    public abstract class MonoStereoProvider : ISampleProvider
    {
        public abstract WaveFormat WaveFormat { get; }

        public IEnumerable<AudioFilter> ActiveFilters { get => filters; }

        public float Volume
        {
            get => ((VolumeProvider)filters[0]).Volume;
            set => ((VolumeProvider)filters[0]).Volume = value;
        }

        private readonly List<AudioFilter> filters = [];

        private readonly ConcurrentQueue<AudioFilter> filterAddQueue = [];

        private readonly ConcurrentQueue<AudioFilter> filterRemovalQueue = [];

        public MonoStereoProvider() => filters.Add(new VolumeProvider(this)); 

        public int Read(float[] buffer, int offset, int count)
        {
            while (filterAddQueue.TryDequeue(out AudioFilter filter))
            {
                filters.Add(filter);
                filter.Apply(this);
            }

            while (filterRemovalQueue.TryDequeue(out AudioFilter filter))
            {
                filters.Remove(filter);
                filter.Unapply(this);
            }

            for (int i = 1; i < filters.Count; i++)
                filters[i].Provider = filters[i - 1];

            return filters[^1].Read(buffer, offset, count);
        }

        public abstract int ReadSource(float[] buffer, int offset, int count);

        public void AddFilter(AudioFilter filter) => filterAddQueue.Enqueue(filter);

        public void RemoveFilter(AudioFilter filter) => filterRemovalQueue.Enqueue(filter);
    }

    file class VolumeProvider(MonoStereoProvider stereoProvider) : AudioFilter
    {
        private readonly MonoStereoProvider provider = stereoProvider;

        public override ISampleProvider Provider { get => provider; }

        public float Volume { get; set; } = 1f;

        public override int ModifyRead(float[] buffer, int offset, int count) => provider.ReadSource(buffer, offset, count);

        public override void PostProcess(float[] buffer, int offset, int samplesRead)
        {
            for (int i = offset; i < samplesRead; i++)
                buffer[i] *= Volume;
        }
    }
}
