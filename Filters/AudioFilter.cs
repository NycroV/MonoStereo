using MonoStereo.SampleProviders;
using NAudio.Wave;
using System.Collections.Generic;

namespace MonoStereo.Filters
{
    public abstract class AudioFilter : ISampleProvider
    {
        public virtual ISampleProvider Provider { get; set; }

        public WaveFormat WaveFormat => Provider.WaveFormat;

        public bool ClearReady { get; set; } = false;

        /// <summary>
        /// This is used for tagging this filter with data regarding any info about it, such as where it is being applied from,<br/>
        /// a specific identifier, or any other data you may want to add. This is useful for accessing specific filters from a list, or<br/>
        /// specific sets of filters.
        /// </summary>
        public readonly Dictionary<string, string> Tags = [];

        public virtual int ModifyRead(float[] buffer, int offset, int count) => Provider.Read(buffer, offset, count);

        public virtual void PostProcess(float[] buffer, int offset, int samplesRead) { }

        public virtual void Apply(MonoStereoProvider provider) { }

        public virtual void Unapply(MonoStereoProvider provider) { }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = ModifyRead(buffer, offset, count);
            PostProcess(buffer, offset, samplesRead);
            return samplesRead;
        }
    }
}
