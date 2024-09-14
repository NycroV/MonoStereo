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

            cachedPosition = source.Position;
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

        private readonly object positionLock = new();
        private long cachedPosition = 0;

        public long Position
        {
            get => cachedPosition;
            set
            {
                lock (positionLock)
                {
                    Reader.ClearBuffer();
                    Source.Position = value;
                    cachedPosition = value;
                }
            }
        }

        public WaveFormat WaveFormat => Source.WaveFormat;

        public void Close()
        {
            Reader.Dispose();
            Source.Close();
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int read = Reader.Read(buffer, offset, count);
            cachedPosition += read;
            return read;
        }

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
                {
                    lock (reader?.positionLock)
                    {
                        reader?.Reader?.ReadAhead();
                    }
                }

                if (i == bufferedReaders.Count - 1)
                    i = -1;
            }
        }
    }
}
