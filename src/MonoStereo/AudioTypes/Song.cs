using MonoStereo.Sources;
using MonoStereo.Sources.Songs;
using MonoStereo.Structures;
using NAudio.Wave;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace MonoStereo
{
    public class Song : MonoStereoProvider
    {
        #region Creation

        /// <summary>
        /// Creates a new Song from the file at the specified path, using an intermediary buffer to make sure samples are always cached in memory.
        /// Note: this will only work if the file has been compiled by MonoStereo's pipeline tool, or is a .ogg file.
        /// </summary>
        [UsedImplicitly]
        public static Song CreateBuffered(string fileName, float secondsToBuffer = 5f) => CreateBuffered(new SongReader(fileName), secondsToBuffer);

        /// <summary>
        /// Creates a new song with the specified source, using an intermediary buffer to make sure samples are always cached in memory.
        /// </summary>
        [UsedImplicitly]
        public static Song CreateBuffered(ISongSource source, float secondsToBuffer = 5f) => Create(BufferedSongReader.Create(source, secondsToBuffer));

        /// <summary>
        /// Creates a new Song from the file at the specified path.
        /// Note: this will only work if the file has been compiled by MonoStereo's pipeline tool, or is a .ogg file.
        /// </summary>
        [UsedImplicitly]
        public static Song Create(string fileName) => Create(new SongReader(fileName));

        /// <summary>
        /// Creates a new song with the specified source.
        /// </summary>
        [UsedImplicitly]
        public static Song Create(ISongSource source) => new(source);

        protected Song(ISongSource source)
        {
            Source = source;
        }

        #endregion

        #region Metadata

        public override WaveFormat WaveFormat { get => Source.WaveFormat; }

        public virtual Dictionary<string, string> Comments { get => Source.Comments; }

        #endregion

        #region Playback

        public virtual ISongSource Source { get; }

        public override PlaybackState PlaybackState
        {
            get => Source.PlaybackState;
            set => Source.PlaybackState = value;
        }

        public bool IsLooped
        {
            get => Source.IsLooped;
            set => Source.IsLooped = value;
        }

        #endregion

        public override int ReadSource(float[] buffer, int offset, int count) => Source.Read(buffer, offset, count);

        /// <summary>
        /// Begins playback of this <see cref="Song"/>. Will restart playback if this song is seekable.
        /// </summary>
        public override void Play()
        {
            PlaybackState = PlaybackState.Playing;

            if (!MonoStereoEngine.ActiveInputs<Song>().Contains(this))
                MonoStereoEngine.AddInput<Song>(this);

            else if (Source is ISeekableSongSource seekableSongSource)
                seekableSongSource.Position = 0;

            Source.OnPlay();
        }

        /// <summary>
        /// Pauses this <see cref="Song"/>. This will remain as an active input in the song mixer, but will not play any audio until resumed.
        /// </summary>
        public override void Pause()
        {
            base.Pause();
            Source.OnPause();
        }

        /// <summary>
        /// Resumes this <see cref="Song"/> if it is paused.
        /// </summary>
        public override void Resume()
        {
            base.Resume();
            Source.OnResume();
        }
        
        /// <summary>
        /// Stops this <see cref="Song"/>. This does not immediately remove it from the mixer - but does mark it for removal on the next audio thread update.<br/>
        /// To immediately remove this sobg from the mixer, use <see cref="RemoveInput"/> instead.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            Source.OnStop();
        }

        /// <summary>
        /// Removes this <see cref="Song"/> from the active audio mixer.
        /// </summary>
        public override void RemoveInput()
        {
            MonoStereoEngine.AudioMixers<Song>().RemoveInput(this);
            Source.OnRemoveInput();
        }
    }
}
