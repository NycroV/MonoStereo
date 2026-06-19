using NAudio.Utils;
using NAudio.Wave;
using System;
using System.Collections.Generic;

namespace MonoStereo.Structures.SampleProviders
{
    public class BufferedReader : ISampleProvider, IDisposable
    {
        public WaveFormat WaveFormat { get; }
        public readonly ISampleProvider sampleProvider;

        public float SecondsToHold
        {
            get => bufferLength / (WaveFormat.SampleRate * WaveFormat.Channels);
            set => bufferLength = (int)(WaveFormat.SampleRate * WaveFormat.Channels * value);
        }

        private float[] inBuffer;
        private int bufferLength;

        private bool sourceSamplesAvailable = true;
        public int BufferedSamples { get => sampleBuffer.Count; }

        private readonly QueuedLock clearBufferLock = new();
        private readonly Queue<float> sampleBuffer = [];
        
        public bool Disposing = false;

        /// <summary>
        /// Creates a new buffered sample provider
        /// </summary>
        public BufferedReader(ISampleProvider source, float secondsToHold)
        {
            WaveFormat = source.WaveFormat;
            SecondsToHold = secondsToHold;
            sampleProvider = source;
        }

        public void ReadAhead(int overrideSampleRequest = 0)
        {
            clearBufferLock.Execute(() =>
            {
                if (Disposing)
                    return;

                int samplesRequested = bufferLength - sampleBuffer.Count;
                if (overrideSampleRequest > 0)
                    samplesRequested = overrideSampleRequest;

                else
                    samplesRequested -= samplesRequested % WaveFormat.Channels;

                if (samplesRequested >= AudioStandards.ReadBufferSize || (overrideSampleRequest > 0 && samplesRequested > 0))
                {
                    inBuffer = BufferHelpers.Ensure(inBuffer, samplesRequested);
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
            });
        }

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
                    ReadAhead(count - read);

                    if (!sourceSamplesAvailable)
                        break;
                }
            }

            return read;
        }

        public void ClearBuffer() => clearBufferLock.Execute(sampleBuffer.Clear);

        public void Dispose()
        {
            Disposing = true;
            ClearBuffer();
        }
    }
}