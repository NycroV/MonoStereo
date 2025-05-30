using MonoStereo.Sources;
using MonoStereo.Sources.Sounds;
using MonoStereo.Structures;
using NAudio.Wave;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace MonoStereo
{
    public class SoundEffect : MonoStereoProvider
    {
        #region Creation

        /// <summary>
        /// Creates a new <see cref="CachedSoundEffect"/> from the file at the specified path.<br/>
        /// If the passed in file path does not have a file extension, MonoStereo will assume you have run the file through the content pipeline and append .xnb to the end.
        /// </summary>
        [UsedImplicitly]
        public static SoundEffect Create(string fileName) => Create(new UniversalAudioSource(fileName));

        /// <summary>
        /// Creates a new <see cref="SoundEffect"/> with the specified source.
        /// </summary>
        [UsedImplicitly]
        public static SoundEffect Create(ISoundEffectSource source) => new(source);
        
        /// <summary>
        /// Creates a new <see cref="SoundEffect"/> from a given <see cref="CachedSoundEffect"/>.
        /// </summary>
        [UsedImplicitly]
        public static SoundEffect Create(CachedSoundEffect cachedSound) => Create(new CachedSoundEffectReader(cachedSound));

        /// <inheritdoc cref="CachedSoundEffect.Create(string)"/>
        [UsedImplicitly]
        public static CachedSoundEffect Cache(string fileName) => CachedSoundEffect.Create(fileName);

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

        /// <summary>
        /// Begins playback of this <see cref="SoundEffect"/>. Will restart playback if this sound effect is seekable.
        /// </summary>
        public override void Play()
        {
            PlaybackState = PlaybackState.Playing;

            if (!MonoStereoEngine.ActiveInputs<SoundEffect>().Contains(this))
                MonoStereoEngine.AddInput<SoundEffect>(this);

            else if (Source is ISeekable seekableSoundEffectSource)
                seekableSoundEffectSource.Position = 0;

            Source.OnPlay();
        }

        /// <summary>
        /// Pauses this <see cref="SoundEffect"/>. This will remain as an active input in the sound effect mixer, but will not play any audio until resumed.
        /// </summary>
        public override void Pause()
        {
            base.Pause();
            Source.OnPause();
        }

        /// <summary>
        /// Resumes this <see cref="SoundEffect"/> if it is paused.
        /// </summary>
        public override void Resume()
        {
            base.Resume();
            Source.OnResume();
        }
        
        /// <summary>
        /// Stops this <see cref="SoundEffect"/>. This does not immediately remove it from the mixer - but does mark it for removal on the next audio thread update.<br/>
        /// To immediately remove this sound effect from the mixer, use <see cref="RemoveInput"/> instead.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            Source.OnStop();
        }

        /// <summary>
        /// Removes this <see cref="SoundEffect"/> from the active audio mixer.
        /// </summary>
        public override void RemoveInput()
        {
            MonoStereoEngine.AudioMixers<SoundEffect>().RemoveInput(this);
            Source.OnRemoveInput();
        }

        public override void Dispose()
        {
            base.Dispose();
            Source.Dispose();
        }
    }
}
