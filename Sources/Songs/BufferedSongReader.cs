using MonoStereo.SampleProviders;
using NAudio.Wave;
using System.Collections.Generic;

namespace MonoStereo.AudioSources.Songs
{
    public class BufferedSongReader(ISongSource source, float secondsToHold = 5f) : ISongSource
    {
        private readonly ISongSource Source = source;

        private readonly BufferedReader Reader = new(source, secondsToHold);

        public PlaybackState PlaybackState
        {
            get => Source.PlaybackState;
            set => Source.PlaybackState = value;
        }

        public Dictionary<string, string> Comments => Source.Comments;

        public long Length => Source.Length;

        public bool IsLooped
        {
            get => Source.IsLooped;
            set
            {
                if (Source.IsLooped != value)
                    Reader.ClearBuffer();

                Source.IsLooped = value;
            }
        }

        public long Position
        {
            get => Source.Position - Reader.BufferedSamples;
            set
            {
                Source.Position = value;
                Reader.ClearBuffer();
            }
        }

        public WaveFormat WaveFormat => Source.WaveFormat;

        public void Close()
        {
            Reader.Dispose();
            Source.Close();
        }

        public int Read(float[] buffer, int offset, int count) => Reader.Read(buffer, offset, count);
    }
}
