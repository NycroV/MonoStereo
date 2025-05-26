using MonoStereo.Structures.SampleProviders;
using MonoStereo.Structures;
using NAudio.Wave;
using System;
using System.Collections.Generic;

namespace MonoStereo.Decoding
{
    // From NAudio.Vorbis
    public class OggReader(System.IO.Stream sourceStream, bool closeOnDispose = true) : WaveStream, ISampleProvider, ISeekable
    {
        public VorbisSampleProvider SampleProvider = new(sourceStream, closeOnDispose);

        public OggReader(string fileName)
            : this(System.IO.File.OpenRead(fileName), true)
        {
            FileName = fileName;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SampleProvider?.Dispose();
                SampleProvider = null;
            }

            base.Dispose(disposing);
        }

        public string FileName { get; } = string.Empty;

        public override WaveFormat WaveFormat => SampleProvider.WaveFormat;

        /// <summary>
        /// Length of the stream, in bytes.
        /// </summary>
        public override long Length => SampleProvider.Length * WaveFormat.BlockAlign;

        /// <summary>
        /// Length of the stream, in samples.
        /// </summary>
        public long SampleLength => SampleProvider.Length * WaveFormat.Channels;

        /// <summary>
        /// Current byte position of the stream.
        /// </summary>
        public override long Position
        {
            get => SampleProvider.SamplePosition * WaveFormat.BlockAlign;
            set => SampleProvider.SamplePosition = value / WaveFormat.BlockAlign;
        }

        /// <summary>
        /// Current sample position of the stream.
        /// </summary>
        public long SamplePosition
        {
            get => SampleProvider.SamplePosition * WaveFormat.Channels;
            set => SampleProvider.SamplePosition = value / WaveFormat.Channels;
        }

        // This buffer can be static because it can only be used by 1 instance per thread
        [ThreadStatic]
        private static float[] _conversionBuffer = null;

        public override int Read(byte[] buffer, int offset, int count)
        {
            // adjust count so it is in floats instead of bytes
            count /= sizeof(float);

            // make sure we don't have an odd count
            count -= count % SampleProvider.WaveFormat.Channels;

            // get the buffer, creating a new one if none exists or the existing one is too small
            var cb = _conversionBuffer ??= new float[count];
            if (cb.Length < count)
                cb = _conversionBuffer = new float[count];

            // let Read(float[], int, int) do the actual reading; adjust count back to bytes
            int cnt = Read(cb, 0, count) * sizeof(float);

            // move the data back to the request buffer
            Buffer.BlockCopy(cb, 0, buffer, offset, cnt);

            // done!
            return cnt;
        }

        public int Read(float[] buffer, int offset, int count) => SampleProvider.Read(buffer, offset, count);

        /// <summary>
        /// Seeks the current stream to the sample position specified.
        /// </summary>
        /// <param name="samplePosition">The sample position to seek to.</param>
        /// <returns>The sample position seeked to.</returns>
        public long Seek(long samplePosition)
        {
            SampleProvider.Seek(samplePosition);
            return Position;
        }

        public int StreamCount => SampleProvider.StreamCount;

        public int? NextStreamIndex { get; set; }

        public bool GetNextStreamIndex()
        {
            if (!NextStreamIndex.HasValue)
            {
                NextStreamIndex = SampleProvider.GetNextStreamIndex();
                return NextStreamIndex.HasValue;
            }
            return false;
        }

        public int CurrentStream
        {
            get => SampleProvider.GetCurrentStreamIndex();
            set
            {
                SampleProvider.SwitchStreams(value);

                NextStreamIndex = null;
            }
        }

        /// <summary>
        /// Gets the encoder's upper bitrate of the current selected Vorbis stream
        /// </summary>
        public int UpperBitrate => SampleProvider.UpperBitrate;

        /// <summary>
        /// Gets the encoder's nominal bitrate of the current selected Vorbis stream
        /// </summary>
        public int NominalBitrate => SampleProvider.NominalBitrate;

        /// <summary>
        /// Gets the encoder's lower bitrate of the current selected Vorbis stream
        /// </summary>
        public int LowerBitrate => SampleProvider.LowerBitrate;

        /// <summary>
        /// Gets the encoder's vendor string for the current selected Vorbis stream
        /// </summary>
        public string Vendor => SampleProvider.Tags.EncoderVendor;

        /// <summary>
        /// Gets the comments in the current selected Vorbis stream
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<string>> Comments => SampleProvider.Tags.All;

        /// <summary>
        /// Gets stats from each decoder stream available
        /// </summary>
        public NVorbis.Contracts.IStreamStats[] Stats => new[] { SampleProvider.Stats };
    }
}
