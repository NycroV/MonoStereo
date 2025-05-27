using MonoStereo.Decoding;
using MonoStereo.Structures;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MonoStereo.Sources.Sounds
{
    // This is essentually just a wrapper on the MonoStereo SoundEffectFileReader,
    // with support for automatic looping.
    public class SoundEffectReader : ISeekableSoundEffectSource, ILoopTags
    {
        #region Metadata

        public string FileName { get; private set; }

        public Dictionary<string, string> Comments { get; }

        public WaveFormat WaveFormat { get => WavReader.WaveFormat; }

        #endregion

        #region Playback

        public SoundEffectFileReader WavReader { get; private set; }

        public PlaybackState PlaybackState { get; set; } = PlaybackState.Stopped;

        #endregion

        #region Play region

        public long Length => WavReader.Length;

        public long Position
        {
            get => WavReader.Position;
            set => WavReader.Position = value;
        }

        public long LoopStart { get; private set; } = -1;

        public long LoopEnd { get; private set; } = -1;

        public bool IsLooped { get; set; } = false;

        #endregion

        public SoundEffectReader(string fileName)
        {
            string filePath = $"{fileName}.xnb";
            if (!File.Exists(filePath))
                throw new ArgumentException($"Specified file not found! - {filePath}");

            FileName = fileName;
            WavReader = new(filePath);

            Comments = WavReader.Comments.ToDictionary();
            Comments.ParseLoop(out long loopStart, out long loopEnd, WaveFormat.Channels);

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

                long samplesAvailable = endIndex - Position;
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

        public void Dispose() => WavReader.Dispose();
    }
}
