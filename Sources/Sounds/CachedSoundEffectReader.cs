using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MonoStereo.AudioSources.Sounds
{
    public class CachedSoundEffectReader(CachedSoundEffect cachedSound) : ILoopableSoundEffectSource
    {
        public string FileName { get => CachedSoundEffect.FileName; }

        public CachedSoundEffect CachedSoundEffect { get; private set; } = cachedSound;

        public WaveFormat WaveFormat => CachedSoundEffect.WaveFormat;

        public Dictionary<string, string> Comments { get; } = cachedSound.Comments.ToDictionary();

        public PlaybackState PlaybackState { get; set; } = PlaybackState.Playing;

        public long Length => CachedSoundEffect.AudioData.Length;

        public long Position { get; set; } = 0;

        public long LoopStart { get => CachedSoundEffect.LoopStart; }

        public long LoopEnd { get => CachedSoundEffect.LoopEnd; }

        public bool IsLooped { get; set; } = false;

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
                {
                    Array.Copy(CachedSoundEffect.AudioData, Position, buffer, offset + samplesCopied, samplesToCopy);
                    samplesCopied += samplesToCopy;
                    Position += samplesToCopy;
                }

                if (IsLooped && Position == endIndex)
                {
                    long startIndex = Math.Max(0, LoopStart);
                    Position = startIndex;
                }
            }
            while (IsLooped && samplesCopied < count);

            return samplesCopied;
        }

        public void Close() { }
    }
}
