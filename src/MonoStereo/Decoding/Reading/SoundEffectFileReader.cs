using MonoStereo.Structures;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace MonoStereo.Decoding
{
    // Provides a way to read raw, uncompressed samples from a stream that is preceded by comments.
    // This custom structure is used by the sound effect compiler in the pipeline.
    public class SoundEffectFileReader : WaveStream, ISampleProvider, ISeekable, IDisposable
    {
        public override WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(AudioStandards.SampleRate, AudioStandards.ChannelCount);

        public BinaryReader Stream { get; private set; }

        public ImmutableDictionary<string, string> Comments { get; set; }

        private readonly long bufferOffset;

        /// <summary>
        /// Length of the stream, in samples.
        /// </summary>
        public override long Length => (Stream.BaseStream.Length - bufferOffset) / AudioStandards.BytesPerSample;

        /// <summary>
        /// Current sample position of the stream.
        /// </summary>
        public override long Position
        {
            get => (Stream.BaseStream.Position - bufferOffset) / AudioStandards.BytesPerSample;
            set => Stream.BaseStream.Position = (value * AudioStandards.BytesPerSample) + bufferOffset;
        }

        public SoundEffectFileReader(Stream fileStream)
        {
            Stream = new(fileStream);

            Dictionary<string, string> comments = [];
            int commentCount = Stream.ReadInt32();

            for (int i = 0; i < commentCount; i++)
                comments.Add(Stream.ReadString(), Stream.ReadString());

            Comments = comments.ToImmutableDictionary();
            bufferOffset = Stream.BaseStream.Position;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            long samplesAvailable = Length - Position;
            int samplesToCopy = (int)Math.Min(samplesAvailable, count);

            for (int i = 0; i < samplesToCopy; i++)
                buffer[offset + i] = Stream.ReadSingle();

            return samplesToCopy;
        }

        public override int Read(byte[] buffer, int offset, int count) => Stream.Read(buffer, offset, count);

        public new void Dispose()
        {
            Stream.Close();
            Comments = null;

            GC.SuppressFinalize(this);
        }
    }
}
