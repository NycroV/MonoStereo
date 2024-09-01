using MonoStereo.Filters;
using NAudio.Wave;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MonoStereo.SampleProviders
{
    public abstract class MonoStereoProvider : ISampleProvider, IDisposable
    {
        public abstract WaveFormat WaveFormat { get; }

        public float Volume
        {
            get => ((FilterBase)filters[0]).Volume;
            set => ((FilterBase)filters[0]).Volume = value;
        }

        public MonoStereoProvider() => filters.Add(new FilterBase(this));

        private readonly ArrayList filters = ArrayList.Synchronized([]);

        public IEnumerable<AudioFilter> Filters { get => filters.Cast<AudioFilter>(); }

        public abstract PlaybackState PlaybackState { get; set; }

        public int Read(float[] buffer, int offset, int count)
        {
            var sortedFilters = filters.Cast<AudioFilter>().OrderBy(filter => filter.Priority).ToArray();

            for (int i = 1; i < sortedFilters.Length; i++)
                sortedFilters[i].Provider = sortedFilters[i - 1];

            return sortedFilters[^1].Read(buffer, offset, count);
        }

        public abstract int ReadSource(float[] buffer, int offset, int count);

        public void AddFilter(AudioFilter filter)
        {
            filters.Add(filter);
            filter.Apply(this);
        }

        public void RemoveFilter(AudioFilter filter)
        {
            filters.Remove(filter);
            filter.Unapply(this);
        }

        public virtual void Close() { }

        public void Dispose()
        {
            Close();

            lock (filters)
            {
                foreach (var filter in filters.Cast<AudioFilter>())
                    filter.Dispose();

                filters.Clear();
            }

            GC.SuppressFinalize(this);
        }
    }

    file class FilterBase(MonoStereoProvider stereoProvider) : AudioFilter
    {
        private readonly MonoStereoProvider provider = stereoProvider;

        public override ISampleProvider Provider { get => provider; }

        public override FilterPriority Priority => FilterPriority.ApplyFirst;

        public float Volume { get; set; } = 1f;

        public override int ModifyRead(float[] buffer, int offset, int count) => provider.ReadSource(buffer, offset, count);

        public override void PostProcess(float[] buffer, int offset, int samplesRead)
        {
            for (int i = offset; i < samplesRead; i++)
                buffer[i] *= Volume;
        }
    }
}
