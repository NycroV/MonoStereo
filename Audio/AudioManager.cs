using MonoStereo.SampleProviders;
using NAudio;
using NAudio.Wave;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace MonoStereo
{
    public static class AudioManager
    {
        private static Thread AudioThread { get; set; }

        public static bool IsRunning { get; private set; }

        internal static HighPriorityWaveOutEvent Output { get; set; }

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

        private static readonly ArrayList activeSongs = ArrayList.Synchronized([]);

        private static readonly ArrayList activeSoundEffects = ArrayList.Synchronized([]);

        public static ReadOnlyCollection<Song> ActiveSongs { get { lock (activeSongs) { return activeSongs.Cast<Song>().ToList().AsReadOnly(); } } }

        public static ReadOnlyCollection<SoundEffect> ActiveSoundEffects { get { lock (activeSoundEffects) { return activeSoundEffects.Cast<SoundEffect>().ToList().AsReadOnly(); } } }

        public static void AddSongInput(Song song)
        {
            lock (activeSongs) { activeSongs.Add(song); }
        }

        public static void RemoveSongInput(Song song)
        {
            lock (activeSongs) { activeSongs.Remove(song); }
        }

        public static void AddSoundEffectInput(SoundEffect soundEffect)
        {
            lock (activeSoundEffects) { activeSoundEffects.Add(soundEffect); }
        }

        public static void RemoveSoundInput(SoundEffect soundEffect)
        {
            lock (activeSoundEffects) { activeSoundEffects.Remove(soundEffect); }
        }

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
        public static void Initialize(Func<bool> shouldShutdown, int latency = 150, float masterVolume = 1f, float musicVolume = 1f, float soundEffectVolume = 1f)
        {
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

            AudioThread = new(() => RunAudioThread(shouldShutdown)) { Priority = ThreadPriority.BelowNormal };
            AudioThread.Start();

            IsRunning = true;
        }

        internal static void RunAudioThread(Func<bool> shouldShutdown)
        {
            while (!shouldShutdown())
                Update();

            // After the `shouldShutdown` function has returned true,
            // commense shutdown. This is done in a way such that the engine
            // COULD be started again without the need for program reload.
            Output.Dispose();
            MasterMixer.Dispose();
            MusicMixer.Dispose();
            SoundMixer.Dispose();

            IsRunning = false;

            foreach (CachedSoundEffect sound in CachedSounds.ToArray())
                sound.Dispose();
        }

        internal static void Update()
        {
            static void UpdateInputs(AudioMixer mixer, ArrayList newInputs)
            {
                // The .ToList() calls duplicate the sources so as not to
                // cause enumeration errors
                var inputs = mixer.Inputs.MixerInputs.Cast<MonoStereoProvider>().ToList();

                foreach (var input in inputs)
                {
                    // Close() will remove the input from it's corresponding list,
                    // so after this is called it will be removed from the mixer's inputs.
                    if (input.PlaybackState == PlaybackState.Stopped)
                        input.Close();
                }

                lock (newInputs)
                {
                    var sources = newInputs.Cast<ISampleProvider>();
                    mixer.Inputs.SetMixerInputs(sources);
                }
            }

            UpdateInputs(MusicMixer, activeSongs);
            UpdateInputs(SoundMixer, activeSoundEffects);
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

            Output = new() { DeviceNumber = deviceNumber };
            Output.Init(MasterMixer);

            Output.Play();
        }

        #endregion
    }
}
