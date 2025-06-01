using MP3Sharp;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoStereo.Decoding
{
    // Standardizes the Mp3Stream into a WaveStream.
    internal class Mp3Reader : WaveStream
    {
        public Mp3Reader(Stream baseStream)
        {
            Stream = new(baseStream);

            // Mp3 reader will always be 16 bit stereo.
            WaveFormat = WaveFormat.CreateCustomFormat(
                WaveFormatEncoding.Pcm,
                Stream.Frequency,
                AudioStandards.ChannelCount,
                Stream.Frequency / 1000 * sizeof(short) * AudioStandards.ChannelCount,
                sizeof(short) * AudioStandards.ChannelCount,
                sizeof(short) * 8);
        }

        private readonly MP3Stream Stream;

        public override WaveFormat WaveFormat { get; }

        public override long Length => Stream.Length;

        public override long Position
        {
            get => Stream.Position / AudioStandards.BytesPerSample;
            set => Stream.Position = value * AudioStandards.BytesPerSample;
        }

        public override int Read(byte[] buffer, int offset, int count) => Stream.Read(buffer, offset, count);

        protected override void Dispose(bool disposing) => Stream.Dispose();
    }
}
