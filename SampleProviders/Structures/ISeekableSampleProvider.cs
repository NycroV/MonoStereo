using NAudio.Wave;
using System;

namespace MonoStereo.SampleProviders
{
    public interface ISeekableSampleProvider : ISampleProvider
    {
        public long Position { get; set; }

        public void Seek(TimeSpan position)
        {
            long samplePos = (long)(position.TotalSeconds * WaveFormat.SampleRate) * WaveFormat.Channels;
            Position = samplePos * AudioStandards.BytesPerSample;
        }
    }
}
