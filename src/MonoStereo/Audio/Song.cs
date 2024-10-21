using MonoStereo.Sources;
using MonoStereo.Sources.Songs;
using MonoStereo.Structures;
using NAudio.Wave;
using System.Collections.Generic;

namespace MonoStereo
{
    public class Song : MonoStereoProvider
    {
        #region Creation

        /// <summary>
        /// Creates a new Song from the file at the specified path, using an intermediary buffer to make sure samples are always cached in memory.
        /// Note: this will only work if the file has been compiled by MonoStereo's pipeline tool, or is a .ogg file.
        /// </summary>
        public static Song CreateBuffered(string fileName, float secondsToBuffer = 5f) => CreateBuffered(new SongReader(fileName), secondsToBuffer);

        /// <summary>
        /// Creates a new song with the specified source, using an intermediary buffer to make sure samples are always cached in memory.
        /// </summary>
        public static Song CreateBuffered(ISongSource source, float secondsToBuffer = 5f) => Create(BufferedSongReader.Create(source, secondsToBuffer));

        /// <summary>
        /// Creates a new Song from the file at the specified path.
        /// Note: this will only work if the file has been compiled by MonoStereo's pipeline tool, or is a .ogg file.
        /// </summary>
        public static Song Create(string fileName) => Create(new SongReader(fileName));

        /// <summary>
        /// Creates a new song with the specified source.
        /// </summary>
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

        public override void Play()
        {
            PlaybackState = PlaybackState.Playing;

            if (!AudioManager.ActiveSongs.Contains(this))
                AudioManager.AddSongInput(this);

            else if (Source is ISeekableSongSource seekableSongSource)
                seekableSongSource.Position = 0;

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
            AudioManager.RemoveSongInput(this);
            Source.Close();
        }
    }
}
