﻿using MonoStereo.SampleProviders;
using NAudio.Wave;
using System.Collections.Generic;
using System.Threading;

namespace MonoStereo.AudioSources.Songs
{
    /// <summary>
    /// Reads a song a configurable amount of seconds ahead of time into memory to offload expensive IO operations to a background thread.<br/>
    /// <br/>
    /// IMPORTANT: The <see cref="Position"/> property is NOT guaranteed to be accurate to the position of the underlying reader, as this encapsulator reads ahead of time.<br/>
    /// It should be accurate before any looping occurs, but after that it will display as if it is one continuous reader, not a looped read.<br/>
    /// If you need this value to be accurate, it is recommended to implement some of your own logic to guarantee accuracy.
    /// </summary>
    public class BufferedSongReader : ISongSource
    {
        #region Buffer Assets

        private static readonly List<BufferedSongReader> bufferedReaders = [];
        private static Thread readerThread;
        private readonly QueuedLock readerLock = new();

        #endregion

        public BufferedSongReader(ISongSource source, float secondsToHold = 5f)
        {
            Source = source;
            Reader = new(source, secondsToHold);

            // If the thread that cached buffered song readers
            // ahead of time has yet to be started, start it.
            if (readerThread is null)
            {
                readerThread = new(CacheBuffers) { Priority = ThreadPriority.AboveNormal };
                readerThread.Start();
            }

            cachedPosition = source.Position;
            bufferedReaders.Add(this);
        }

        #region Playback

        public readonly ISongSource Source;

        private readonly BufferedReader Reader;

        public WaveFormat WaveFormat => Source.WaveFormat;

        public PlaybackState PlaybackState
        {
            get => Source.PlaybackState;
            set => Source.PlaybackState = value;
        }

        public Dictionary<string, string> Comments => Source.Comments;

        #endregion

        #region Play region

        public long Length => Source.Length;

        private long cachedPosition;

        public bool IsLooped
        {
            get => Source.IsLooped;
            set
            {
                if (Source.IsLooped != value)
                {
                    Source.IsLooped = value;
                    Position = cachedPosition;
                }
            }
        }

        public long Position
        {
            get => cachedPosition;
            set
            {
                try
                {
                    readerLock.Enter();
                    Reader.ClearBuffer();

                    value %= Length;
                    Source.Position = value;
                    cachedPosition = value;
                }

                finally
                {
                    readerLock.Exit();
                }
            }
        }

        #endregion

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

                // Remove buffers that have been closed
                // If a buffer is null, it may not be finished creating yet. Default to false to account for this case.
                if (reader?.Reader?.Disposing ?? false)
                {
                    bufferedReaders.RemoveAt(i);
                    i--;
                    continue;
                }

                if (reader?.PlaybackState == PlaybackState.Playing)
                {
                    try
                    {
                        reader.readerLock.Enter();
                        reader.Reader.ReadAhead();
                    }
                    finally
                    {
                        reader.readerLock.Exit();
                    }
                }

                if (i == bufferedReaders.Count - 1)
                    i = -1;
            }
        }
    }
}
