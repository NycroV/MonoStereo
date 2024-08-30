using CARDS.MonoStereo.Encoding;
using MonoStereo.Encoding;
using MonoStereo.SampleProviders;
using NAudio.Wave;
using NVorbis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoStereo.Audio
{
    public class SoundEffectReader : ISoundEffectSource
    {
        public string FileName { get; private set; }

        public WavReader WavReader { get; private set; }

        public ImmutableDictionary<string, string> Comments { get => WavReader.Comments; }

        public WaveFormat WaveFormat { get => WavReader.WaveFormat; }

        public PlaybackState PlaybackState { get; set; } = PlaybackState.Playing;

        public long Length => WavReader.Length;

        public long Position
        {
            get => WavReader.Position;
            set => WavReader.Position = value;
        }

        public long LoopStart { get; private set; } = -1;

        public long LoopEnd { get; private set; } = -1;

        public bool IsLooped { get; set; } = false;

        public SoundEffectReader(string fileName)
        {
            string filePath = $"{fileName}.xnb";
            if (!File.Exists(filePath))
                throw new ArgumentException($"Specified file not found! - {filePath}");

            FileName = fileName;
            WavReader = new(filePath);

            Comments.ParseLoop(out long loopStart, out long loopEnd);
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
                    samplesCopied += WavReader.Read(buffer, offset + samplesCopied, samplesToCopy);

                if (IsLooped && Position == endIndex)
                {
                    long startIndex = Math.Max(0, LoopStart);
                    Position = startIndex;
                }
            }

            while (IsLooped && samplesCopied < count);

            return samplesCopied;
        }

        public void Close() => WavReader.Dispose();
    }
}
