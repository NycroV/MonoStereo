using NAudio.Utils;
using NAudio.Wave;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace MonoStereo.SampleProviders
{
    public class BufferedReader : ISampleProvider, IDisposable
    {
        public WaveFormat WaveFormat { get; }
        private readonly ISampleProvider sampleProvider;

        public float SecondsToHold { get => bufferLength / WaveFormat.SampleRate / WaveFormat.Channels; set => bufferLength = (int)(WaveFormat.SampleRate * WaveFormat.Channels * value); }
        private int bufferLength;
        private bool samplesAvailable = true;

        private readonly object clearBufferLock = new();
        private readonly ConcurrentQueue<float> sampleBuffer = [];
        private float[] inBuffer;
        public int BufferedSamples { get => sampleBuffer.Count; }

        private static readonly List<BufferedReader> bufferedReaders = [];
        private static Thread readerThread;
        private bool disposing = false;

        /// <summary>
        /// Creates a new buffered WaveProvider
        /// </summary>
        public BufferedReader(ISampleProvider source, float secondsToHold)
        {
            WaveFormat = source.WaveFormat;
            SecondsToHold = secondsToHold;
            sampleProvider = source;

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

        private void ReadAhead()
        {
            int samplesRequested = bufferLength - sampleBuffer.Count;
            inBuffer = BufferHelpers.Ensure(inBuffer, samplesRequested);

            lock (clearBufferLock)
            {
                int read = sampleProvider.Read(inBuffer, 0, samplesRequested);

                if (read > 0)
                {
                    for (int i = 0; i < read; i++)
                        sampleBuffer.Enqueue(inBuffer[i]);
                }

                else
                    samplesAvailable = false;
            }
        }

        /// <summary>
        /// Reads from this SampleProvider
        /// Will always return count floats, since we will zero-fill the buffer if not enough available
        /// </summary>
        public int Read(float[] buffer, int offset, int count)
        {
            int read = 0;

            while (read < count && samplesAvailable)
            {
                if (sampleBuffer.TryDequeue(out float sample))
                {
                    buffer[offset++] = sample;
                    read++;
                }

                else
                    ReadAhead();
            }

            return read;
        }

        private static void CacheBuffers()
        {
            while (AudioManager.IsRunning)
            {
                for (int i = 0; i < bufferedReaders.Count; i++)
                {
                    var reader = bufferedReaders[i];

                    if (reader?.disposing ?? false)
                    {
                        bufferedReaders.RemoveAt(i);
                        i--;
                        continue;
                    }

                    reader?.ReadAhead();
                }
            }
        }

        public void ClearBuffer()
        {
            lock (clearBufferLock)
            {
                sampleBuffer.Clear();
            }
        }

        public void Dispose()
        {
            disposing = true;
        }
    }
}