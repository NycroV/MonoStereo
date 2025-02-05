using MonoStereo.SampleProviders;
using MonoStereo.Structures;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using PortAudioSharp;
using System;
using System.Collections.Generic;
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

        private AudioMixer mixer = null;

        private ISampleProvider output = null;

        private float[] intermediaryBuffer = null;

        private Exception playbackError = null;

        #endregion

        #region Output Utilities

        /// <summary>
        /// Lists all PortAudio device indexes that are available for output (speakers or headphones).
        /// </summary>
        /// <returns>An array of device indexes that are valid to pass as the "output device index" parameter.</returns>
        public static int[] GetOutputDeviceIndexes()
        {
            List<int> indexes = [];
            
            for (int i = 0; i < PortAudio.DeviceCount; i++)
            {
                if (PortAudio.GetDeviceInfo(i).maxOutputChannels > 0)
                    indexes.Add(i);
            }

            return indexes.ToArray();
        }

        // This is currently unused. Potential future implementation?
        //
        ///// <summary>
        ///// Lists all PortAudio device indexes that are available for input (microphones).
        ///// </summary>
        ///// <returns>An array of device indexes that are valid to pass as the "output device index" parameter.</returns>
        //public static int[] GetInputDeviceIndexes()
        //{
        //    List<int> indexes = [];

        //    for (int i = 0; i < PortAudio.DeviceCount; i++)
        //    {
        //        if (PortAudio.GetDeviceInfo(i).maxInputChannels > 0)
        //            indexes.Add(i);
        //    }

        //    return indexes.ToArray();
        //}

        #endregion

        public void Init(AudioMixer waveProvider)
        {
            // Initialize PortAudio's API and assign the output.
            PortAudio.Initialize();

            mixer = waveProvider;

            // If no device index or specific latency are requested, we use the system defaults.
            int deviceIndex = DeviceIndex ?? PortAudio.DefaultOutputDevice;
            var deviceInfo = PortAudio.GetDeviceInfo(deviceIndex);

            double latency = Latency ?? deviceInfo.defaultLowOutputLatency;

            // Mix down to mono if the output only supports 1 channel.
            if (deviceInfo.maxOutputChannels == 1)
                output = new StereoToMonoSampleProvider(mixer)
                {
                    LeftVolume = 0.5f,
                    RightVolume = 0.5f
                };

            else
                output = mixer;

            // This should be pretty self explanatory
            StreamParameters outputStreamFormat = new()
            {
                device = deviceIndex,
                channelCount = AudioStandards.ChannelCount,
                sampleFormat = SampleFormat.Float32,
                suggestedLatency = latency,
                hostApiSpecificStreamInfo = IntPtr.Zero // This is the equivalent of `state` for a Thread WaitCallback. We don't need it.
            };

            // We use NoFlag here so that PortAudio can handle clipping and dithering for us. C is faster!
            PlaybackStream = new(null, outputStreamFormat, AudioStandards.SampleRate, 0, StreamFlags.NoFlag, Callback, IntPtr.Zero);

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

            // If playback errors, we want to be able to shut down the engine to prevent deadlocks.
            try
            {
                this.output.Read(intermediaryBuffer, 0, sampleCount);
            }
            catch (Exception ex)
            {
                playbackError = ex;
                return StreamCallbackResult.Abort;
            }

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
            if (playbackError != null)
                throw playbackError;
        }

        public void Dispose()
        {
            PlaybackStream?.Dispose();
            PortAudio.Terminate();

            PlaybackStream = null;
            output = null;
        }
    }
}
