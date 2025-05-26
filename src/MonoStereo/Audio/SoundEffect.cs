using MonoStereo.Sources;
using MonoStereo.Sources.Sounds;
using MonoStereo.Structures;
using NAudio.Wave;
using System.Collections.Generic;

namespace MonoStereo
{
    public class SoundEffect : MonoStereoProvider
    {
        #region Creation

        public static SoundEffect Create(string fileName) => Create(new SoundEffectReader(fileName));

        public static SoundEffect Create(CachedSoundEffect cachedSound) => Create(new CachedSoundEffectReader(cachedSound));

        public static SoundEffect Create(ISoundEffectSource source) => new(source);

        protected SoundEffect(ISoundEffectSource source)
        {
            Source = source;
        }

        #endregion

        #region Metadata

        public override WaveFormat WaveFormat { get => Source.WaveFormat; }

        public virtual Dictionary<string, string> Comments { get => Source.Comments; }

        #endregion

        #region Playback

        public virtual ISoundEffectSource Source { get; }

        public override PlaybackState PlaybackState
        {
            get => Source.PlaybackState;
            set => Source.PlaybackState = value;
        }

        public virtual bool IsLooped
        {
            get => Source.IsLooped;
            set => Source.IsLooped = value;
        }

        #endregion

        public override int ReadSource(float[] buffer, int offset, int count) => Source.Read(buffer, offset, count);

        public override void Play()
        {
            PlaybackState = PlaybackState.Playing;

            if (!AudioManager.AudioMixers<SoundEffect>().Inputs.Contains(this))
                AudioManager.AudioMixers<SoundEffect>().AddInput(this);

            else if (Source is ISeekableSoundEffectSource seekableSoundEffectSource)
                seekableSoundEffectSource.Position = 0;

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
        
        public override void Stop()
        {
            base.Stop();
            Source.OnStop();
        }

        public override void RemoveInput()
        {
            AudioManager.AudioMixers<SoundEffect>().RemoveInput(this);
            Source.OnRemoveInput();
        }
    }
}
