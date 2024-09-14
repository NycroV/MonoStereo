using NAudio.Utils;
using NAudio.Wave;
using System;
using System.Collections.Concurrent;

namespace MonoStereo.SampleProviders
{
    public class BufferedReader : ISampleProvider, IDisposable
    {
        public WaveFormat WaveFormat { get; }
        private readonly ISampleProvider sampleProvider;

        public float SecondsToHold { get => bufferLength / WaveFormat.SampleRate / WaveFormat.Channels; set => bufferLength = (int)(WaveFormat.SampleRate * WaveFormat.Channels * value); }
        private int bufferLength;
        private bool sourceSamplesAvailable = true;

        private readonly object clearBufferLock = new();
        private readonly ConcurrentQueue<float> sampleBuffer = [];
        private float[] inBuffer;
        public int BufferedSamples { get => sampleBuffer.Count; }

        public bool Disposing = false;

        /// <summary>
        /// Creates a new buffered WaveProvider
        /// </summary>
        public BufferedReader(ISampleProvider source, float secondsToHold)
        {
            WaveFormat = source.WaveFormat;
            SecondsToHold = secondsToHold;
            sampleProvider = source;
        }

        public void ReadAhead()
        {
            if (Disposing)
                return;

            int samplesRequested = bufferLength - sampleBuffer.Count;
            while (samplesRequested % WaveFormat.Channels != 0)
                samplesRequested--;

            if (samplesRequested > 0)
            {
                inBuffer = BufferHelpers.Ensure(inBuffer, samplesRequested);

                lock (clearBufferLock)
                {
                int read = sampleProvider.Read(inBuffer, 0, samplesRequested);

                if (read > 0)
                {
                    sourceSamplesAvailable = true;
                    for (int i = 0; i < read; i++)
                        sampleBuffer.Enqueue(inBuffer[i]);
                }

                else
                    sourceSamplesAvailable = false;
            }
        }
        }

        /// <summary>
        /// Reads from this SampleProvider
        /// Will always return count floats, since we will zero-fill the buffer if not enough available
        /// </summary>
        public int Read(float[] buffer, int offset, int count)
        {
            int read = 0;

            while (read < count)
            {
                if (sampleBuffer.TryDequeue(out float sample))
                {
                    buffer[offset++] = sample;
                    read++;
                }

                else
                {
                    ReadAhead();

                    if (!sourceSamplesAvailable)
                        break;
                }
            }

            return read;
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
            Disposing = true;
        }
    }
}