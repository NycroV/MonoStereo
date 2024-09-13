using NAudio.Wave;
using System;

namespace MonoStereo.SampleProviders
{
    public interface ISeekableSampleProvider : ISampleProvider
    {
        public long Position { get; set; }

        public long Length { get; }

        public void Seek(TimeSpan position) => Position = (long)(position.TotalSeconds * WaveFormat.SampleRate) * WaveFormat.Channels;
    }
}
