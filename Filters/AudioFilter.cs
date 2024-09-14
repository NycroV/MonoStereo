using MonoStereo.SampleProviders;
using NAudio.Wave;
using System;
using System.Collections.Generic;

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
        public ISampleProvider Provider { get; set; }

        public MonoStereoProvider Source { get; set; }

        public virtual FilterPriority Priority { get => FilterPriority.None; }

        public WaveFormat WaveFormat => Provider.WaveFormat;

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

        public int CompareTo(AudioFilter other) => Priority.CompareTo(other.Priority);
    }

    internal record class FilterEntry(AudioFilter Filter, uint AddIndex);

    internal class FilterComparer : IComparer<FilterEntry>
    {
        public int Compare(FilterEntry x, FilterEntry y)
        {
            int compare = x.Filter.CompareTo(y.Filter);

            if (compare == 0)
                compare = x.AddIndex.CompareTo(y.AddIndex);

            return compare;
        }
    }
}
