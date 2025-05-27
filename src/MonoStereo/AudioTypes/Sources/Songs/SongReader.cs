using MonoStereo.Decoding;
using MonoStereo.Structures;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;

namespace MonoStereo.Sources.Songs
{
    // This is essentially just a wrapper on an OggReader, with support for automatic looping.
    // MonoStereo song files are compiled as OggFiles by default.
    public class SongReader : ISeekableSongSource, ILoopTags
    {
        #region Metadata

        public string FileName { get; private set; }

        public WaveFormat WaveFormat { get => OggReader.WaveFormat; }

        public Dictionary<string, string> Comments { get; private set; }

        #endregion

        #region Playback

        public OggReader OggReader { get; private set; }

        public PlaybackState PlaybackState { get; set; } = PlaybackState.Stopped;

        #endregion

        #region Play region

        public long Length => OggReader.SampleLength;

        public long Position
        {
            get => OggReader.SamplePosition;
            set
            {
                if (OggReader.SamplePosition != value)
                    OggReader.SamplePosition = value;
            }
        }

        public long LoopStart { get; private set; } = -1;

        public long LoopEnd { get; private set; } = -1;

        public bool IsLooped { get; set; } = false;

        #endregion

        public SongReader(string fileName)
        {
            string filePath = $"{fileName}.xnb";
            if (!File.Exists(filePath))
                throw new ArgumentException($"Specified file not found! - {filePath}");

            FileName = fileName;

            OggReader = new(filePath);
            Comments = OggReader.Comments.ComposeComments();
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

        public void Dispose() => OggReader.Dispose();
    }
}
