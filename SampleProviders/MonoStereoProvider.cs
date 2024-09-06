using MonoStereo.Filters;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MonoStereo.SampleProviders
{
    public abstract class MonoStereoProvider : ISampleProvider, IDisposable
    {
        public abstract WaveFormat WaveFormat { get; }

        // The first filter in the filters list will ALWAYS be a FilterBase -
        // this is how the volume is controlled, as well as the chain of filter reading is assembled.
        //
        // This filter is generally not visible to end users, as removing it would cause issues.
        public float Volume
        {
            get => filterBase.Volume;
            set => filterBase.Volume = value;
        }

        private readonly FilterBase filterBase;

        public MonoStereoProvider()
        {
            filterBase = new(this);
            filters.Add(filterBase);
        }

        private readonly SortedSet<AudioFilter> filters = [];

        public IEnumerable<AudioFilter> Filters
        {
            get
            {
                // Remove the FilterBase from the available filters
                AudioFilter[] castFilters;
                lock (filters) { castFilters = filters.Cast<AudioFilter>().ToArray(); }
                return castFilters.TakeLast(castFilters.Length - 1);
            }
        }

        public abstract PlaybackState PlaybackState { get; set; }

        public int Read(float[] buffer, int offset, int count)
        {
            lock (filters)
            {
                for (int i = 1; i < filters.Count; i++)
                    filters.ElementAt(i).Provider = filters.ElementAt(i - 1);

                return filters.Last().Read(buffer, offset, count);
            }
        }

        public abstract int ReadSource(float[] buffer, int offset, int count);

        public void AddFilter(AudioFilter filter)
        {
            lock (filters) { filters.Add(filter); }
            filter.Apply(this);
        }

        public void RemoveFilter(AudioFilter filter)
        {
            lock (filters) { filters.Remove(filter); }
            filter.Unapply(this);
        }

        // Remove every filter except for the FilterBase
        public void ClearFilters()
        {
            foreach (var filter in Filters.ToArray())
            {
                filters.Remove(filter);
                filter.Unapply(this);
            }
        }

        public abstract void Play();

        public virtual void Pause() => PlaybackState = PlaybackState.Paused;

        public virtual void Resume() => PlaybackState = PlaybackState.Playing;

        public virtual void Stop() => PlaybackState = PlaybackState.Stopped; 

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

    internal class FilterBase(MonoStereoProvider stereoProvider) : AudioFilter
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
