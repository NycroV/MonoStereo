﻿using MonoStereo.SampleProviders;
using MonoStereo.Outputs;
using NAudio.Wave;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using MonoStereo.Structures;

namespace MonoStereo
{
    public static class AudioManager
    {
        private static Thread AudioThread { get; set; }

        public static bool IsRunning { get; private set; }

        public static IMonoStereoOutput Output { get; set; }

        public static AudioMixer SoundMixer { get; private set; }

        public static AudioMixer MusicMixer { get; private set; }

        public static AudioMixer MasterMixer { get; private set; }

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

        private static readonly List<Song> activeSongs = [];

        private static readonly List<SoundEffect> activeSoundEffects = [];

        public static ReadOnlyCollection<Song> ActiveSongs { get { lock (activeSongs) { return activeSongs.Cast<Song>().ToList().AsReadOnly(); } } }

        public static ReadOnlyCollection<SoundEffect> ActiveSoundEffects { get { lock (activeSoundEffects) { return activeSoundEffects.Cast<SoundEffect>().ToList().AsReadOnly(); } } }

        /// <summary>
        /// Adds a song input to the mixer.<br/>
        /// Instead of using this, use <see cref="Song.Play"/>
        /// </summary>
        public static void AddSongInput(Song song)
        {
            lock (activeSongs) { activeSongs.Add(song); }
        }

        /// <summary>
        /// Removes a song input from the mixer.<br/>
        /// Instead of using this, use <see cref="Song.Stop"/>
        /// </summary>
        public static void RemoveSongInput(Song song)
        {
            lock (activeSongs) { activeSongs.Remove(song); }
        }

        /// <summary>
        /// Adds a sound effect input to the mixer.<br/>
        /// Instead of using this, use <see cref="SoundEffect.Play"/>
        /// </summary>
        public static void AddSoundEffectInput(SoundEffect soundEffect)
        {
            lock (activeSoundEffects) { activeSoundEffects.Add(soundEffect); }
        }

        /// <summary>
        /// Removes a sound effect input from the mixer.<br/>
        /// Instead of using this, use <see cref="SoundEffect.Stop"/>
        /// </summary>
        public static void RemoveSoundInput(SoundEffect soundEffect)
        {
            lock (activeSoundEffects) { activeSoundEffects.Remove(soundEffect); }
        }

        /// <summary>
        /// If an <see cref="Exception"/> was thrown by the playback thread, this method will throw that error again.<br/>
        /// This allows you to check for audio errors on the main thread.
        /// </summary>
        public static void ThrowIfErrored()
        {
            if (PlaybackError != null)
                throw PlaybackError;
        }

        // Used to forward errors from the playback thread.
        private static Exception PlaybackError = null;

        /// <summary>
        /// Initializes the MonoStereo Audio Engine. Note that using this method overload will default to WinMM as an output source.<br/>
        /// If you would like to supply your own output source, use <see cref="InitializeCustomOutput(IMonoStereoOutput, Func{bool}, float, float, float)"/> instead.
        /// </summary>
        /// <param name="shouldShutdown">The function to determine when the audio engine should shut down. Have the delegate return true when your game is being/has been closed.</param>
        /// <param name="masterVolume">The master mixer volume</param>
        /// <param name="musicVolume">The volume for music</param>
        /// <param name="soundEffectVolume">The volume for sound effects</param>
        /// <param name="latency">The desired latency, in ms.<br/>
        /// Higher latency will create slightly delayed audio playback, but can help reduce laggy/choppy audio when lots of post-processing or filters are involved.<br/>
        /// 100ms-150ms is typically fine for most purposes.</param>
        /// <param name="deviceNumber">The index of the output device you would like to play to.</param>
        public static void Initialize(
            Func<bool> shouldShutdown,

            float masterVolume = 1f,
            float musicVolume = 1f,
            float soundEffectVolume = 1f,

            int? deviceIndex = null,
            double? latency = null)
        {
            // Initialize to PortAudio output by default.
            // Passing in null utilizes the system defaults.
            PortAudioOutput output = DefaultOutput(deviceIndex, latency);
            InitializeCustomOutput(output, shouldShutdown, masterVolume, musicVolume, soundEffectVolume);
        }

        /// <summary>
        /// Creates the default MonoStereo output stream, which is routed through PortAudio.
        /// </summary>
        /// <param name="deviceIndex"></param>
        /// <param name="latency"></param>
        /// <returns></returns>
        public static PortAudioOutput DefaultOutput(int? deviceIndex = null, double? latency = null) => new(deviceIndex, latency);

        /// <summary>
        /// Initializes the MonoStereo Audio Engine using a custom <see cref="IMonoStereoOutput"/>. You should only use this if you know what you're doing.<br/>
        /// Providing a custom output allows you to change how audio is actually played back to the user.
        /// </summary>
        /// <param name="customOutput">The custom <see cref="IMonoStereoOutput"/> the audio engine should output to.</param>
        /// <param name="shouldShutdown">The function to determine when the audio engine should shut down. Have the delegate return true when your game is being/has been closed.</param>
        /// <param name="masterVolume">The master mixer volume</param>
        /// <param name="musicVolume">The volume for music</param>
        /// <param name="soundEffectVolume">The volume for sound effects</param>
        public static void InitializeCustomOutput(
            IMonoStereoOutput customOutput,
            Func<bool> shouldShutdown,
            
            float masterVolume = 1f,
            float musicVolume = 1f,
            float soundEffectVolume = 1f)
        {
            SoundMixer = new(soundEffectVolume);
            MusicMixer = new(musicVolume);
            MasterMixer = new(masterVolume, SoundMixer, MusicMixer);

            SoundMixer.Source.MixerInputEnded += (sender, e) =>
            {
                SoundEffect sound = e.SampleProvider as SoundEffect;
                sound.Stop();
            };

            MusicMixer.Source.MixerInputEnded += (sender, e) =>
            {
                Song song = e.SampleProvider as Song;
                song.Stop();
            };

            Output = customOutput;

            PlaybackError = null;
            Output.Init(MasterMixer);

            AudioThread = new(() => RunAudioThread(shouldShutdown)) { Priority = ThreadPriority.BelowNormal };
            AudioThread.Start();

            IsRunning = true;
        }

        internal static void RunAudioThread(Func<bool> shouldShutdown)
        {
            try
            {
                Output.Play();

                while (!shouldShutdown())
                    Update();
            }

            catch (Exception ex)
            {
                PlaybackError = ex;
            }

            finally
            {
                // After the `shouldShutdown` function has returned true,
                // commense shutdown. This is done in a way such that the engine
                // COULD be started again without the need for program reload.
                Output.Dispose();
                MasterMixer.Dispose();
                MusicMixer.Dispose();
                SoundMixer.Dispose();

                IsRunning = false;
            }
        }

        internal static void Update()
        {
            static void UpdateInputs(AudioMixer mixer, IList newInputs)
            {
                // The .ToList() calls duplicate the sources so as not to
                // cause enumeration errors
                var inputs = mixer.MixerInputs.Cast<MonoStereoProvider>().ToList();

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
                    mixer.Source.SetMixerInputs(sources);
                }
            }

            Output.Update();
            UpdateInputs(MusicMixer, activeSongs);
            UpdateInputs(SoundMixer, activeSoundEffects);
        }
    }
}
