using MonoStereo.SampleProviders;
using PortAudioSharp;
using System;
using System.Runtime.InteropServices;
using PortAudioStream = PortAudioSharp.Stream;

namespace MonoStereo.Outputs
{
    public class PortAudioOutput(int? deviceIndex, double? latency) : IMonoStereoOutput
    {
        #region Properties

        public int? DeviceIndex { get; } = deviceIndex;

        public double? Latency { get; } = latency;

        public PortAudioStream PlaybackStream { get; private set; }

        private AudioMixer output = null;

        private float[] intermediaryBuffer = null;

        #endregion

        public void Init(AudioMixer waveProvider)
        {
            // Initialize PortAudio's API and assign the output.
            PortAudio.Initialize();
            output = waveProvider;

            // If no device index or specific latency are requested, we use the system defaults.
            int deviceIndex = DeviceIndex ?? PortAudio.DefaultOutputDevice;
            double latency = Latency ?? PortAudio.GetDeviceInfo(deviceIndex).defaultLowInputLatency;

            // This should be pretty self explanatory
            StreamParameters streamFormat = new()
            {
                device = deviceIndex,
                channelCount = AudioStandards.ChannelCount,
                sampleFormat = SampleFormat.Float32,
                suggestedLatency = latency,
                hostApiSpecificStreamInfo = IntPtr.Zero // This is the equivalent of `state` for a Thread WaitCallback. We don't need it.
            };

            // We use NoFlag here so that PortAudio can handle clipping and dithering for us. C is so much faster!
            PlaybackStream = new(null, streamFormat, AudioStandards.SampleRate, 0, StreamFlags.NoFlag, Callback, IntPtr.Zero);

            // The intermediary buffer is read into, and then marshalled to PortAudio.
            intermediaryBuffer = [];
        }

        private StreamCallbackResult Callback(
            IntPtr input,                         // Unused currently. This would be a microphone input if it was used. Potential future implementation?
            IntPtr output,                        // Where we will copy our output samples to.
            uint frameCount,                      // The number of frames requested.
            ref StreamCallbackTimeInfo timeInfo,  // Time info about the stream. Unused.
            StreamCallbackFlags statusFlag,       // Status about the stream. Also unused - we will always read the requested sample count.
            IntPtr userData)                      // This is the equivalent of `state` for a Thread WaitCallback. We don't need it.

        {
            // Get the actual number of samples we need to read
            int sampleCount = (int)frameCount * AudioStandards.ChannelCount;

            // Make sure our intermediary buffer is long enough to hold all the samples.
            EnsureBuffer(ref intermediaryBuffer, sampleCount);
            this.output.Read(intermediaryBuffer, 0, sampleCount);

            // Copy the read samples to PortAudio's output.
            Marshal.Copy(intermediaryBuffer, 0, output, sampleCount);
            return StreamCallbackResult.Continue;
        }

        // Makes sure the buffer is long enough to be read into.
        private static void EnsureBuffer(ref float[] buffer, int count)
        {
            if (buffer.Length < count)
                buffer = new float[count];
        }

        // Starts the playback stream.
        public void Play()
        {
            PlaybackStream.Start();
        }

        public void Update()
        {
            
        }

        public void Dispose()
        {
            PlaybackStream.Dispose();
        }
    }
}
