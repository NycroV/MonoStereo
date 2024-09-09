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

        public override int ReadSource(float[] buffer, int offset, int count) => Source.Read(buffer, offset, count);

        public override void Play()
        {
            PlaybackState = PlaybackState.Playing;

            if (!AudioManager.ActiveSoundEffects.Contains(this))
                AudioManager.AddSoundEffectInput(this);

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
            AudioManager.RemoveSoundInput(this);
        }
    }
}
