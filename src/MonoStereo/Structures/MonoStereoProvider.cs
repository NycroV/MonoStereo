using MonoStereo.Filters;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MonoStereo.Structures
{
    public abstract class MonoStereoProvider : ISampleProvider, IDisposable
    {
        public abstract WaveFormat WaveFormat { get; }

        // The first filter in the filters list will ALWAYS be a FilterBase -
        // this is how the volume is controlled, as well as the chain of filter reading is assembled.
        //
        // This filter is generally not visible to end users, as removing it would cause issues.
        public virtual float Volume
        {
            get => _filterBase.Volume;
            set => _filterBase.Volume = value;
        }

        private uint _filterIndex = 0;
        private readonly FilterBase _filterBase;

        public MonoStereoProvider()
        {
            _filterBase = new(this);
            AddFilter(_filterBase);
        }

        private readonly SortedSet<FilterEntry> _filters = [];

        public virtual IEnumerable<AudioFilter> Filters
        {
            get
            {
                // Remove the FilterBase from the available filters
                AudioFilter[] castFilters;
                lock (_filters) { castFilters = _filters.Select(entry => entry.Filter).ToArray(); }
                return castFilters.TakeLast(castFilters.Length - 1);
            }
        }

        public abstract PlaybackState PlaybackState { get; set; }

        public int Read(float[] buffer, int offset, int count)
        {
            switch (PlaybackState)
            {
                case PlaybackState.Playing:
                {
                    lock (_filters)
                    {
                        for (int i = 1; i < _filters.Count; i++)
                        {
                            var entry = _filters.ElementAt(i).Filter;
                            entry.Provider = _filters.ElementAt(i - 1).Filter;
                            entry.Source = this;
                        }

                        return _filters.Last().Filter.Read(buffer, offset, count);
                    }
                }
                case PlaybackState.Paused:
                {
                    for (int i = 0; i < count; i++)
                        buffer[offset + i] = 0;

                    return count;
                }
                case PlaybackState.Stopped:
                default:
                    return 0;
            }
        }

        public abstract int ReadSource(float[] buffer, int offset, int count);

        public void AddFilter(AudioFilter filter)
        {
            if (Filters.Contains(filter))
                return;

            lock (_filters) { _filters.Add(new(filter, _filterIndex++)); }
            filter.Apply(this);
        }

        public void RemoveFilter(AudioFilter filter)
        {
            lock (_filters) { _filters.Remove(_filters.FirstOrDefault(entry => entry.Filter == filter, null)); }
            filter.Unapply(this);
        }

        // Remove every filter except for the FilterBase
        public void ClearFilters()
        {
            foreach (AudioFilter filter in Filters.ToArray())
                RemoveFilter(filter);
        }

        public abstract void Play();

        public virtual void Pause() => PlaybackState = PlaybackState.Paused;

        public virtual void Resume() => PlaybackState = PlaybackState.Playing;

        public virtual void Stop() => PlaybackState = PlaybackState.Stopped;

        public abstract void RemoveInput();

        public virtual void Dispose()
        {
            RemoveInput();

            lock (_filters)
            {
                _filters.Clear();
            }

            GC.SuppressFinalize(this);
        }
    }

    internal class FilterBase : AudioFilter
    {
        public FilterBase(MonoStereoProvider stereoProvider)
        {
            BaseProvider = stereoProvider;
            Provider = stereoProvider;
        }

        public readonly MonoStereoProvider BaseProvider;

        public override FilterPriority Priority => FilterPriority.ApplyFirst;

        public float Volume { get; set; } = 1f;

        public override int ModifyRead(float[] buffer, int offset, int count) => BaseProvider.ReadSource(buffer, offset, count);

        public override void PostProcess(float[] buffer, int offset, int samplesRead)
        {
            if (Volume == 1f)
                return;

            for (int i = offset; i < samplesRead; i++)
                buffer[i] *= Volume;
        }
    }
}
