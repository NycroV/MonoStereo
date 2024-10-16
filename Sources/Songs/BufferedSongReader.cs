using MonoStereo.SampleProviders;
using NAudio.Wave;
using System.Collections.Generic;
using System.Threading;

namespace MonoStereo.AudioSources.Songs
{
    /// <summary>
    /// Reads a song a configurable amount of seconds ahead of time into memory to offload expensive IO operations to a background thread.
    /// </summary>
    public class BufferedSongReader : ISongSource
    {
        #region Buffer Assets

        private protected static readonly List<BufferedSongReader> bufferedReaders = [];
        private protected static Thread readerThread { get; private set; }
        private protected readonly QueuedLock readerLock = new();

        #endregion

        public static BufferedSongReader Create(ISongSource source, float secondsToHold = 5f)
        {
            if (source is ISeekableSongSource seekable)
                return new SeekableBufferedSongReader(seekable, secondsToHold);

            return new BufferedSongReader(source, secondsToHold);
        }

        internal BufferedSongReader(ISongSource source, float secondsToHold = 5f)
        {
            Source = source;
            Reader = new(source, secondsToHold);

            // If the thread that caches buffered song readers
            // ahead of time has yet to be started, start it.
            if (readerThread is null)
            {
                readerThread = new(CacheBuffers) { Priority = ThreadPriority.AboveNormal };
                readerThread.Start();
            }

            bufferedReaders.Add(this);
        }

        #region Playback

        public readonly ISongSource Source;

        public readonly BufferedReader Reader;

        public WaveFormat WaveFormat => Source.WaveFormat;

        public PlaybackState PlaybackState
        {
            get => Source.PlaybackState;
            set => Source.PlaybackState = value;
        }

        public Dictionary<string, string> Comments => Source.Comments;

        public virtual bool IsLooped
        {
            get => Source.IsLooped;
            set => Source.IsLooped = value;
        }

        #endregion

        public void Close()
        {
            Reader.Dispose();
            Source.Close();
        }

        public virtual int Read(float[] buffer, int offset, int count) => Reader.Read(buffer, offset, count);

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
                    reader.readerLock.Execute(reader.Reader.ReadAhead);

                if (i >= bufferedReaders.Count - 1)
                    i = -1;
            }
        }
    }

    /// <summary>
    /// Reads a song a configurable amount of seconds ahead of time into memory to offload expensive IO operations to a background thread.<br/>
    /// <br/>
    /// IMPORTANT: The <see cref="Position"/> property is NOT guaranteed to be accurate to the position of the underlying reader, as this encapsulator reads ahead of time.<br/>
    /// It should be accurate before any looping occurs, but after that it will display as if it is one continuous reader, not a looped read.<br/>
    /// If you need this value to be accurate, it is recommended to implement some of your own logic to guarantee accuracy.
    /// </summary>
    public class SeekableBufferedSongReader : BufferedSongReader, ISeekableSongSource
    {
        public new ISeekableSongSource Source => base.Source as ISeekableSongSource;
        
        internal SeekableBufferedSongReader(ISeekableSongSource source, float secondsToHold = 5f) : base(source, secondsToHold)
        {
            cachedPosition = source.Position;
        }

        private long cachedPosition;

        public long Length => Source.Length;

        public long Position
        {
            get => cachedPosition;
            set
            {
                readerLock.Execute(() =>
                {
                    Reader.ClearBuffer();

                    value %= Length;
                    Source.Position = value;
                    cachedPosition = value;
                });
            }
        }

        public override bool IsLooped
        {
            get => base.IsLooped;
            set
            {
                bool looped = base.IsLooped;
                base.IsLooped = value;

                if (looped != value)
                    Position = cachedPosition;
            }
        }

        public override int Read(float[] buffer, int offset, int count)
        {
            int read = base.Read(buffer, offset, count);
            cachedPosition += read;
            return read;
        }
    }
}
