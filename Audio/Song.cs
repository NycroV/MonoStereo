using MonoStereo.AudioSources;
using MonoStereo.AudioSources.Songs;
using MonoStereo.SampleProviders;
using NAudio.Wave;
using System.Collections.Generic;
using System.Linq;

namespace MonoStereo
{
    public class Song(ISongSource source) : MonoStereoProvider, ISeekableSampleProvider
    {
        public Song(string fileName) : this(new SongReader(fileName))
        { }

        private ISongSource Source { get; set; } = source;

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

            // If the song is paused, we don't want the mixer to think it's stopped.
            // Fill the buffer with empty samples to imitate "no audio".
            else if (PlaybackState == PlaybackState.Paused)
            {
                for (int i = 0; i < count; i++)
                    buffer[i] = 0;

                samplesRead = count;
            }

            return samplesRead;
        }

        public override void Play()
        {
            PlaybackState = PlaybackState.Playing;

            if (!AudioManager.ActiveSongs.Contains(this))
                AudioManager.AddSongInput(this);

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
            AudioManager.RemoveSongInput(this);
            Source.Close();
        }
    }
}
