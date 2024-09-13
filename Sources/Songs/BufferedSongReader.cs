using MonoStereo.SampleProviders;
using NAudio.Wave;
using System.Collections.Generic;
using System.Threading;

namespace MonoStereo.AudioSources.Songs
{
    public class BufferedSongReader : ISongSource
    {
        private static readonly List<BufferedSongReader> bufferedReaders = [];
        private static Thread readerThread;

        public BufferedSongReader(ISongSource source, float secondsToHold = 5f)
        {
            Source = source;
            Reader = new(source, secondsToHold);

            if (readerThread is null)
            {
                readerThread = new(CacheBuffers)
                {
                    Priority = ThreadPriority.AboveNormal
                };

                readerThread.Start();
            }

            bufferedReaders.Add(this);
        }

        private readonly ISongSource Source;

        private readonly BufferedReader Reader;

        public PlaybackState PlaybackState
        {
            get => Source.PlaybackState;
            set => Source.PlaybackState = value;
        }

        public Dictionary<string, string> Comments => Source.Comments;

        public long Length => Source.Length;

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

        private static void CacheBuffers()
        {
            for (int i = 0; i < bufferedReaders.Count; i++)
            {
                if (!AudioManager.IsRunning)
                    break;

                var reader = bufferedReaders[i];

                if (reader?.Reader?.Disposing ?? false)
                {
                    bufferedReaders.RemoveAt(i);
                    i--;
                    continue;
                }

                if (reader?.PlaybackState == PlaybackState.Playing)
                    reader?.Reader?.ReadAhead();

                if (i == bufferedReaders.Count - 1)
                    i = -1;
            }
        }
    }
}
