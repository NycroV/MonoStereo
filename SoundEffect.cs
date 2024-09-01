using MonoStereo.AudioSources;
using MonoStereo.AudioSources.Sounds;
using MonoStereo.SampleProviders;
using NAudio.Wave;
using System.Collections.Generic;

namespace MonoStereo
{
    public class SoundEffect(ISoundEffectSource source) : MonoStereoProvider, ISeekableSampleProvider
    {
        public SoundEffect(string fileName) : this(new SoundEffectReader(fileName))
        { }

        public SoundEffect(CachedSoundEffect cachedSound) : this(new CachedSoundEffectReader(cachedSound))
        { }

        private ISoundEffectSource Source { get; set; } = source;

        public override WaveFormat WaveFormat { get => Source.WaveFormat; }

        public Dictionary<string, string> Comments { get => Source.Comments; }

        public override PlaybackState PlaybackState
        {
            get => Source.PlaybackState;
            set => Source.PlaybackState = value;
        }

        public long Length => Source.Length;

        public long Position
        {
            get => Source.Position;
            set => Source.Position = value;
        }

        public bool IsLooped
        {
            get => Source.IsLooped;
            set => Source.IsLooped = value;
        }

        public override int ReadSource(float[] buffer, int offset, int count)
        {
            int samplesRead = 0;

            if (PlaybackState == PlaybackState.Paused)
            {
                for (int i = 0; i < count; i++)
                {
                    buffer[i] = 0;
                    samplesRead++;
                }
            }

            else if (PlaybackState == PlaybackState.Playing)
                samplesRead = Source.Read(buffer, offset, count);

            else
                return 0;

            return samplesRead;
        }

        public void Play()
        {
            PlaybackState = PlaybackState.Playing;
            AudioManager.ActiveSoundEffects.Add(this);
        }

        public override void Close() => Source.Close();
    }
}
