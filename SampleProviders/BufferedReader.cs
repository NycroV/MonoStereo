using NAudio.Wave;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace MonoStereo.SampleProviders
{
    public class BufferedReader : ISeekableSampleProvider, IDisposable
    {
        private ConcurrentQueue<float> sampleBuffer = [];

        private int sampleEstimate = 0;

        private bool samplesAvailable = true;

        private bool disposing = false;

        private readonly ISeekableSampleProvider source;

        public BufferedReader(ISeekableSampleProvider provider)
        {
            source = provider;
            _position = provider.Position;

            Thread readerThread = new(() =>
            {
                while (!disposing)
                    ReadAhead();

                sampleBuffer.Clear();
                sampleBuffer = null;
            });
        }

        private long _position;

        public long Position
        {
            get => _position;
            set
            {
                source.Position = value;
                sampleBuffer.Clear();
                _position = value;
            }
        }

        public WaveFormat WaveFormat => source.WaveFormat;

        public void ReadAhead()
        {
            int samplesRequired = sampleEstimate - sampleBuffer.Count;

            if (samplesRequired > 0 && samplesAvailable)
            {
                float[] buffer = new float[samplesRequired];
                var samplesRead = source.Read(buffer, 0, samplesRequired);

                for (int i = 0; i < samplesRead; i++)
                    sampleBuffer?.Enqueue(buffer[i]);

                if (samplesRead == 0)
                    samplesAvailable = false;
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            sampleEstimate = count;
            int samplesRead = 0;

            bool OutSample()
            {
                if (sampleBuffer.TryDequeue(out float sample))
                {
                    buffer[offset++] = sample;
                    samplesRead++;
                    return true;
                }

                return false;
            }

            for (int i = 0; i < count; i++)
            {
                if (!OutSample())
                {
                    if (samplesAvailable)
                        ReadAhead();

                    else
                        break;

                    i--;
                }
            }

            _position += samplesRead;
            return samplesRead;
        }

        public void Dispose()
        {
            disposing = true;
            GC.SuppressFinalize(this);
        }
    }
}
