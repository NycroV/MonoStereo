using MonoStereo.SampleProviders;
using NAudio.Wave;
using System;
using System.Collections.Immutable;

namespace MonoStereo.Audio
{
    public class CachedSoundEffectReader(CachedSoundEffect cachedSound) : ISoundEffectSource
    {
        public string FileName { get => CachedSoundEffect.FileName; }

        public CachedSoundEffect CachedSoundEffect { get; private set; } = cachedSound;

        public WaveFormat WaveFormat => CachedSoundEffect.WaveFormat;

        public ImmutableDictionary<string, string> Comments => CachedSoundEffect.Comments;

        public PlaybackState PlaybackState { get; set; } = PlaybackState.Playing;

        public long Length => CachedSoundEffect.AudioData.Length * AudioStandards.BytesPerSample;

        public long BufferPosition { get; set; } = 0;

        public long Position
        {
            get => BufferPosition * AudioStandards.BytesPerSample;
            set => BufferPosition = value / AudioStandards.BytesPerSample;
        }

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

                long samplesAvailable = (endIndex / AudioStandards.BytesPerSample) - BufferPosition;
                long samplesRemaining = count - samplesCopied;

                int samplesToCopy = (int)Math.Min(samplesAvailable, samplesRemaining);
                if (samplesToCopy > 0)
                {
                    Array.Copy(CachedSoundEffect.AudioData, BufferPosition, buffer, offset + samplesCopied, samplesToCopy);
                    samplesCopied += samplesToCopy;
                    BufferPosition += samplesToCopy;
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
