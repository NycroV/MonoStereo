﻿using MonoStereo.SampleProviders;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace MonoStereo.Encoding
{
    public class SoundEffectFileReader : ISeekableSampleProvider, IDisposable
    {
        public string FileName { get; private set; }

        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(AudioStandards.SampleRate, AudioStandards.ChannelCount);

        public BinaryReader Stream { get; private set; }

        public ImmutableDictionary<string, string> Comments { get; set; }

        private readonly long bufferOffset;

        public long Length => (Stream.BaseStream.Length - bufferOffset) / AudioStandards.BytesPerSample;

        public long Position
        {
            get => (Stream.BaseStream.Position - bufferOffset) / AudioStandards.BytesPerSample;
            set => Stream.BaseStream.Position = (value * AudioStandards.BytesPerSample) + bufferOffset;
        }

        public SoundEffectFileReader(string fileName)
        {
            string filePath = $"{fileName}.xnb";
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