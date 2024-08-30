using MonoStereo.SampleProviders;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace MonoStereo.Encoding
{
    public class WavReader : ISeekableSampleProvider, IDisposable
    {
        public string FileName { get; private set; }

        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(AudioStandards.StandardSampleRate, AudioStandards.StandardChannelCount);

        public BinaryReader Stream { get; private set; }

        public ImmutableDictionary<string, string> Comments { get; set; }

        private readonly long bufferOffset;

        public long Length => Stream.BaseStream.Length - bufferOffset;

        public long Position
        {
            get => Stream.BaseStream.Position - bufferOffset;
            set => Stream.BaseStream.Position = value + bufferOffset;
        }

        public WavReader(string fileName)
        {
            string filePath = $"Assets/{fileName}.xnb";
            if (!File.Exists(filePath))
                throw new ArgumentException($"Specified file not found! - {filePath}");

            FileName = fileName;
            Stream = new(File.OpenRead(filePath));

            Dictionary<string, string> comments = [];
            int commentCount = Stream.ReadInt32();

            for (int i = 0; i < commentCount; i++)
                comments.Add(Stream.ReadString(), Stream.ReadString());

            Comments = comments.ToImmutableDictionary();
            bufferOffset = Stream.BaseStream.Position;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            long samplesAvailable = (Length - Position) / AudioStandards.BytesPerSample;
            int samplesToCopy = (int)Math.Min(samplesAvailable, count);

            for (int i = 0; i <= samplesToCopy; i++)
                buffer[offset + i] = Stream.ReadSingle();

            return samplesToCopy;
        }

        public void Dispose()
        {
            FileName = null;
            Stream.Close();
            Comments = null;

            GC.SuppressFinalize(this);
        }
    }
}
