using NAudio;
using NAudio.Wave;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using MonoStereo.SampleProviders;
using System.Collections.Concurrent;

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

        public static IEnumerable<SoundEffect> ActiveSounds { get => SoundMixer.Inputs.MixerInputs.Cast<SoundEffect>(); }

        public static IEnumerable<Song> ActiveSongs { get => MusicMixer.Inputs.MixerInputs.Cast<Song>(); }

        private static readonly ConcurrentQueue<SoundEffect> queuedSounds = new();

        private static readonly ConcurrentQueue<Song> queuedSongs = new();

        /// <summary>
        /// Initializes the MonoStereo Audio Engine
        /// </summary>
        /// <param name="shutdownFunction">The function to determine when the audio engine should shut down. Have the delegate return true when your game is being/has been closed.</param>
        /// <param name="masterVolume">The master mixer volume</param>
        /// <param name="musicVolume">The volume for music</param>
        /// <param name="soundEffectVolume">The volume for sound effects</param>
        public static void Initialize(Func<bool> shutdownFunction, float masterVolume = 1f, float musicVolume = 1f, float soundEffectVolume = 1f)
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

            Output = new()
            {
                DesiredLatency = AudioStandards.StandardLatency
            };

            Output.Init(MasterMixer);
            Output.Play();

            AudioThread = new(() =>
            {
                while (!shutdownFunction())
                    Update();
            });

            AudioThread.Start();
        }

        public static void Update()
        {
            #region Sound Updating

            while (queuedSounds.TryDequeue(out SoundEffect sound))
            {
                if (sound is null)
                    continue;

                try { SoundMixer.AddInput(sound); }
                catch { }
            }

            var sounds = ActiveSounds;
            for (int i = 0; i < sounds.Count(); i++)
            {
                var sound = sounds.ElementAt(i);

                if (sound.PlaybackState == PlaybackState.Stopped)
                {
                    SoundMixer.Inputs.RemoveMixerInput(sound);
                    sound.Close();

                    i--;
                }
            }

            #endregion

            #region Song Updating

            while (queuedSongs.TryDequeue(out Song song))
            {
                if (song is null)
                    continue;

                try { MusicMixer.AddInput(song); }
                catch { }
            }

            var songs = ActiveSongs;
            for (int i = 0; i < songs.Count(); i++)
            {
                var song = songs.ElementAt(i);

                if (song.PlaybackState == PlaybackState.Stopped)
                {
                    MusicMixer.Inputs.RemoveMixerInput(song);
                    song.Close();

                    i--;
                }
            }

            #endregion
        }

        public static void QueuePlay(SoundEffect sound) => queuedSounds.Enqueue(sound);

        public static void QueuePlay(Song song) => queuedSongs.Enqueue(song);

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
