using ATL;
using MP3Sharp;
using NAudio.Wave;
using System.IO;

namespace MonoStereo.Decoding
{
    // Standardizes the Mp3Stream into a WaveStream.
    internal class Mp3Reader : WaveStream
    {
        public Mp3Reader(Stream baseStream)
        {
            long streamPosition = baseStream.Position;
            Track track = new(baseStream);
            var length = track.DurationMs;
            audioOffset = track.TechnicalInformation.AudioDataOffset;
            baseStream.Position = streamPosition;

            Stream = new(baseStream);
            baseStream.Position = audioOffset;

            // Mp3 reader will always be 16 bit stereo.
            WaveFormat = WaveFormat.CreateCustomFormat(
                WaveFormatEncoding.Pcm,
                Stream.Frequency,
                Stream.ChannelCount,
                (int)(Stream.Frequency / 1000f * sizeof(short) * Stream.ChannelCount),
                sizeof(short) * Stream.ChannelCount,
                sizeof(short) * 8);

            Length = (long)(length * (WaveFormat.SampleRate / 1000d) * WaveFormat.BlockAlign);
        }

        private readonly MP3Stream Stream;

        public override WaveFormat WaveFormat { get; }

        public override long Length { get; }

        private readonly long audioOffset = 0L;
        public override long Position
        {
            get => (Stream.Position - audioOffset) / WaveFormat.BlockAlign * WaveFormat.Channels;
            set => Stream.Position = (value / WaveFormat.Channels) * WaveFormat.BlockAlign + audioOffset;
        }

        public override int Read(byte[] buffer, int offset, int count) => Stream.Read(buffer, offset, count);

        protected override void Dispose(bool disposing) => Stream.Dispose();
    }
}
