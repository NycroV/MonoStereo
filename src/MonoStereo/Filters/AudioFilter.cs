using MonoStereo.Structures;
using NAudio.Wave;
using System;

namespace MonoStereo.Filters
{
    // Filter priorities allow for smoother application of filters to audio.
    // Typically, filters that modify the position of an underlying stream will have
    // custom priority. Speed change, for example, should be applied last - as it changes
    // the number of samples that will be read from the source on a given Read() call.
    public enum FilterPriority
    {
        ApplyFirst,
        None,
        ApplyLast
    }

    public abstract class AudioFilter : ISampleProvider, IDisposable
    {
        // Usually the filter that was applied just before this one
        public ISampleProvider Provider { get; set; }

        public MonoStereoProvider Source { get; set; }

        public WaveFormat WaveFormat => Provider.WaveFormat;

        public virtual FilterPriority Priority { get => FilterPriority.None; }

        public virtual int ModifyRead(float[] buffer, int offset, int count) => Provider.Read(buffer, offset, count);

        public virtual void PostProcess(float[] buffer, int offset, int samplesRead) { }

        public virtual void Apply(MonoStereoProvider provider) { }

        public virtual void Unapply(MonoStereoProvider provider) { }

        public virtual void Dispose() => GC.SuppressFinalize(this);

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = ModifyRead(buffer, offset, count);
            PostProcess(buffer, offset, samplesRead);
            return samplesRead;
        }

        internal int CompareTo(AudioFilter other) => Priority.CompareTo(other.Priority);
    }

    internal record class FilterEntry(AudioFilter Filter, uint AddIndex) : IComparable<FilterEntry>
    {
        public int CompareTo(FilterEntry other)
        {
            int compare = Filter.CompareTo(other.Filter);

            if (compare == 0)
                compare = AddIndex.CompareTo(other.AddIndex);

            return compare;
        }
    }
}
