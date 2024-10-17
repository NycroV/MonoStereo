using NAudio.Wave;
using System;

namespace MonoStereo.SampleProviders
{
    public interface ISeekable
    {
        public long Position { get; set; }

        public long Length { get; }

        public void Seek(TimeSpan position, WaveFormat waveFormat) => Position = (long)(position.TotalSeconds * waveFormat.SampleRate) * waveFormat.Channels;
    }
}
