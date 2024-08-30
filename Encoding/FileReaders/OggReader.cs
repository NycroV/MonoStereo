using System;
using System.Linq;
using Wave = NAudio.Wave;
using NAudio.Vorbis;

namespace MonoStereo.Encoding
{
    // From NAudio.Vorbis
    public class OggReader : Wave.WaveStream, Wave.ISampleProvider
    {
        public VorbisSampleProvider SampleProvider;

        public OggReader(string fileName)
            : this(System.IO.File.OpenRead(fileName), true)
        {
        }

        public OggReader(System.IO.Stream sourceStream, bool closeOnDispose = false)
        {
            // To maintain consistent semantics with v1.1, we don't expose the events and auto-advance / stream removal features of VorbisSampleProvider.
            // If one wishes to use those features, they should really use VorbisSampleProvider directly...
            SampleProvider = new VorbisSampleProvider(sourceStream, closeOnDispose);
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

        public override Wave.WaveFormat WaveFormat => SampleProvider.WaveFormat;

        public override long Length => SampleProvider.Length * SampleProvider.WaveFormat.BlockAlign;

        public override long Position
        {
            get => SampleProvider.SamplePosition * SampleProvider.WaveFormat.BlockAlign;
            set
            {
                if (!SampleProvider.CanSeek) throw new InvalidOperationException("Cannot seek!");
                if (value < 0 || value > Length) throw new ArgumentOutOfRangeException(nameof(value));

                SampleProvider.Seek(value / SampleProvider.WaveFormat.BlockAlign);
            }
        }

        public long SamplePosition
        {
            get => SampleProvider.SamplePosition;
            set => SampleProvider.SamplePosition = value;
        }

        // This buffer can be static because it can only be used by 1 instance per thread
        [ThreadStatic]
        static float[] _conversionBuffer = null;

        public override int Read(byte[] buffer, int offset, int count)
        {
            // adjust count so it is in floats instead of bytes
            count /= sizeof(float);

            // make sure we don't have an odd count
            count -= count % SampleProvider.WaveFormat.Channels;

            // get the buffer, creating a new one if none exists or the existing one is too small
            var cb = _conversionBuffer ?? (_conversionBuffer = new float[count]);
            if (cb.Length < count)
            {
                cb = (_conversionBuffer = new float[count]);
            }

            // let Read(float[], int, int) do the actual reading; adjust count back to bytes
            int cnt = Read(cb, 0, count) * sizeof(float);

            // move the data back to the request buffer
            Buffer.BlockCopy(cb, 0, buffer, offset, cnt);

            // done!
            return cnt;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            if (IsParameterChange) throw new InvalidOperationException("A parameter change is pending.  Call ClearParameterChange() to clear it.");

            return SampleProvider.Read(buffer, offset, count);
        }

        public bool IsParameterChange => false;

        [Obsolete("Was never used and will be removed.")]
        public void ClearParameterChange() { }

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
        public string[] Comments => SampleProvider.Tags.All.SelectMany(k => k.Value, (kvp, Item) => $"{kvp.Key}={Item}").ToArray();

        /// <summary>
        /// Gets the number of bits read that are related to framing and transport alone
        /// </summary>
        [Obsolete("No longer supported.", true)]
        public long ContainerOverheadBits => throw new NotSupportedException();

        /// <summary>
        /// Gets stats from each decoder stream available
        /// </summary>
        public NVorbis.Contracts.IStreamStats[] Stats => new[] { SampleProvider.Stats };
    }
}
