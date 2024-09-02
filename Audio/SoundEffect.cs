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

            if (PlaybackState == PlaybackState.Playing)
                samplesRead = Source.Read(buffer, offset, count);

            // If the sound effect is paused, we don't want the mixer to think it's stopped.
            // Fill the buffer with empty samples to imitate "no audio".
            else if (PlaybackState == PlaybackState.Paused)
            {
                for (int i = 0; i < count; i++)
                {
                    buffer[i] = 0;
                    samplesRead++;
                }
            }

            else
                return 0;

            return samplesRead;
        }

        public override void Play()
        {
            PlaybackState = PlaybackState.Playing;

            if (!AudioManager.activeSoundEffects.Contains(this))
                AudioManager.activeSoundEffects.Add(this);

            else
                Source.Position = 0;

            Source.OnPlay();
        }

        public override void Pause()
        {
            base.Pause();
            Source.OnPause();
        }

        public override void Resume()
        {
            base.Resume();
            Source.OnResume();
        }

        // Close will be called after a song is marked as stopped.
        public override void Stop()
        {
            base.Stop();
            Source.OnStop();
        }

        public override void Close()
        {
            Source.Close();
            AudioManager.activeSoundEffects.Remove(this);
        }
    }
}
