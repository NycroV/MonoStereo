using MonoStereo.AudioSources.Sounds;
using MonoStereo.SampleProviders;
using NAudio;
using NAudio.Wave;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace MonoStereo
{
    public static class AudioManager
    {
        private static Thread AudioThread { get; set; }

        private static WaveOutEvent Output { get; set; }

        private static Stopwatch Time { get; set; } = new();

        public static AudioMixer SoundMixer { get; private set; }

        public static AudioMixer MusicMixer { get; private set; }

        public static AudioMixer MasterMixer { get; private set; }

        internal static List<CachedSoundEffect> CachedSounds { get; private set; } = [];

        /// <summary>
        /// Controls the volume of sound effects
        /// </summary>
        public static float SoundEffectVolume
        {
            get => SoundMixer.Volume;
            set => SoundMixer.Volume = value;
        }

        /// <summary>
        /// Controls the volume of music
        /// </summary>
        public static float MusicVolume
        {
            get => MusicMixer.Volume;
            set => MusicMixer.Volume = value;
        }

        /// <summary>
        /// Controls the volume of the master output
        /// </summary>
        public static float MasterVolume
        {
            get => MasterMixer.Volume;
            set => MasterMixer.Volume = value;
        }

        public static readonly ConcurrentBag<Song> ActiveSongs = [];

        public static readonly ConcurrentBag<SoundEffect> ActiveSoundEffects = [];

        /// <summary>
        /// Initializes the MonoStereo Audio Engine
        /// </summary>
        /// <param name="shouldShutdown">The function to determine when the audio engine should shut down. Have the delegate return true when your game is being/has been closed.</param>
        /// <param name="latency">The desired latency, in ms.<br/>
        /// Higher latency will create slightly delayed audio playback, but can help reduce laggy/choppy audio when lots of post-processing or filters are involved.<br/>
        /// 100ms is typically fine for most purposes.</param>
        /// <param name="masterVolume">The master mixer volume</param>
        /// <param name="musicVolume">The volume for music</param>
        /// <param name="soundEffectVolume">The volume for sound effects</param>
        public static void Initialize(Func<bool> shouldShutdown, int latency = 100, float masterVolume = 1f, float musicVolume = 1f, float soundEffectVolume = 1f)
        {
            Time.Start();

            SoundMixer = new(soundEffectVolume);
            MusicMixer = new(musicVolume);
            MasterMixer = new(masterVolume);

            MasterMixer.AddInput(SoundMixer);
            MasterMixer.AddInput(MusicMixer);

            SoundMixer.Inputs.MixerInputEnded += (sender, e) =>
            {
                SoundEffect sound = e.SampleProvider as SoundEffect;
                sound.PlaybackState = PlaybackState.Stopped;
            };

            MusicMixer.Inputs.MixerInputEnded += (sender, e) =>
            {
                Song song = e.SampleProvider as Song;
                song.PlaybackState = PlaybackState.Stopped;
            };

            Output = new() { DesiredLatency = latency };
            Output.Init(MasterMixer);
            Output.Play();

            AudioThread = new(() => RunAudioThread(shouldShutdown));
            AudioThread.Start();
        }

        internal static void RunAudioThread(Func<bool> shouldShutdown)
        {
            while (!shouldShutdown())
                Update();

            Output.Dispose();
            MasterMixer.Dispose();
            MusicMixer.Dispose();
            SoundMixer.Dispose();

            foreach (CachedSoundEffect sound in CachedSounds.ToArray())
                sound.Dispose();
        }

        internal static void Update()
        {
            static void UpdateInputs(AudioMixer mixer, IEnumerable<ISampleProvider> newInputs)
            {
                var inputs = mixer.Inputs.MixerInputs.Cast<MonoStereoProvider>();

                foreach (var input in inputs)
                {
                    if (input.PlaybackState == PlaybackState.Stopped)
                        input.Close();
                }

                mixer.Inputs.SetMixerInputs(newInputs);
            }

            UpdateInputs(MusicMixer, ActiveSongs.ToArray());
            UpdateInputs(SoundMixer, ActiveSoundEffects.ToArray());
        }

        #region Devices

        public static int DeviceCount => WaveInterop.waveOutGetNumDevs();

        public static WaveOutCapabilities GetCapabilities(int deviceNumber)
        {
            var caps = new WaveOutCapabilities();
            var structSize = Marshal.SizeOf(caps);
            MmException.Try(WaveInterop.waveOutGetDevCaps((IntPtr)deviceNumber, out caps, structSize), "waveOutGetDevCaps");
            return caps;
        }

        public static void ReassignAudioDevice(int deviceNumber)
        {
            Output.Dispose();

            Output = new WaveOutEvent() { DeviceNumber = deviceNumber };
            Output.Init(MasterMixer);

            Output.Play();
        }

        #endregion
    }
}
