using MonoStereo.SampleProviders;
using NAudio.Wave;
using System.Collections.Generic;

namespace MonoStereo.AudioSources.Songs
{
    public class BufferedSongReader(ISongSource source) : ISongSource
    {
        private readonly ISongSource Source = source;

        private BufferedReader Reader = new(source);

        public PlaybackState PlaybackState { get => Source.PlaybackState; set => Source.PlaybackState = value; }

        public Dictionary<string, string> Comments => Source.Comments;

        public long Length => Source.Length;

        public bool IsLooped { get => Source.IsLooped; set => Source.IsLooped = value; }

        public long Position { get => Reader.Position; set => Reader.Position = value; }

        public WaveFormat WaveFormat => Source.WaveFormat;

        public void Close()
        {
            Reader.Dispose();
            Source.Close();
        }

        public int Read(float[] buffer, int offset, int count) => Reader.Read(buffer, offset, count);
    }
}
