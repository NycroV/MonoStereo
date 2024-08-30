using CARDS.MonoStereo.Encoding;
using MonoStereo.Encoding;
using MonoStereo.SampleProviders;
using NAudio.Wave;
using NVorbis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace MonoStereo.Audio
{
    public class SongReader : ISongSource
    {
        public string FileName { get; private set; }

        public OggReader OggReader { get; private set; }

        public ImmutableDictionary<string, string> Comments { get; private set; }

        public WaveFormat WaveFormat { get => OggReader.WaveFormat; }

        public PlaybackState PlaybackState { get; set; } = PlaybackState.Playing;

        public long Length => OggReader.Length;

        public long Position
        {
            get => OggReader.SamplePosition;
            set => OggReader.SamplePosition = value;
        }

        public long LoopStart { get; private set; } = -1;

        public long LoopEnd { get; private set; } = -1;

        public bool IsLooped { get; set; } = false;

        public SongReader(string fileName)
        {
            string filePath = $"{fileName}.xnb";
            if (!File.Exists(filePath))
                throw new ArgumentException($"Specified file not found! - {filePath}");

            FileName = fileName;
            Dictionary<string, string> comments = [];

            using (NVorbisReader commentReader = new(filePath))
            {
                foreach (var c in commentReader.Tags.All)
                    comments.Add(c.Key, c.Value[0]);
            }

            OggReader = new(filePath);
            comments.ParseLoop(out long loopStart, out long loopEnd);
            Comments = comments.ToImmutableDictionary();

            LoopStart = loopStart;
            LoopEnd = loopEnd;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesCopied = 0;

            do
            {
                long endIndex = Length;

                if (IsLooped && LoopEnd != -1)
                    endIndex = LoopEnd;

                long samplesAvailable = (endIndex - Position) / AudioStandards.BytesPerSample;
                long samplesRemaining = count - samplesCopied;

                int samplesToCopy = (int)Math.Min(samplesAvailable, samplesRemaining);
                if (samplesToCopy > 0)
                    samplesCopied += OggReader.Read(buffer, offset + samplesCopied, samplesToCopy);

                if (IsLooped && Position == endIndex)
                {
                    long startIndex = Math.Max(0, LoopStart);
                    Position = startIndex;
                }
            }

            while (IsLooped && samplesCopied < count);

            return samplesCopied;
        }

        public void Close() => OggReader.Dispose();
    }
}
