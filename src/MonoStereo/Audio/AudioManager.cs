using MonoStereo.SampleProviders;
using MonoStereo.Outputs;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using JetBrains.Annotations;
using MonoStereo.Structures;

namespace MonoStereo
{
    public static class AudioManager
    {
        private static Thread AudioThread { get; set; }
        
        public static bool IsRunning { get; private set; }
        
        // Used to forward errors from the playback thread.
        private static Exception _playbackError = null;
        
        /// <summary>
        /// If an <see cref="Exception"/> was thrown by the playback thread, this method will throw that error again.<br/>
        /// This allows you to check for audio errors on the main thread.
        /// </summary>
        [UsedImplicitly]
        public static void ThrowIfErrored()
        {
            if (_playbackError != null)
                throw _playbackError;
        }

        public static IMonoStereoOutput Output { get; set; }

        private static readonly Dictionary<Type, AudioMixer> audioMixers = [];

        public static AudioMixer<T> AudioMixers<T>()
            where T : MonoStereoProvider
        {
            if (audioMixers.TryGetValue(typeof(T), out var mixer))
                return (AudioMixer<T>)mixer;

            return null;
        }

        public static void AddAudioMixer<T>(float defaultVolume)
            where T : MonoStereoProvider
        {
            Type inputType = typeof(T);
            AudioMixer<T> mixer = new(defaultVolume);
            audioMixers[inputType] = mixer;
            MasterMixer.AddInput(mixer);
        }

        public static void RemoveAudioMixer<T>()
            where T : MonoStereoProvider
        {
            Type inputType = typeof(T);
            var mixer = audioMixers[inputType];
            MasterMixer.RemoveInput(mixer);
            mixer.Dispose();
        }

        public static ReadOnlyCollection<T> ActiveInputs<T>()
            where T : MonoStereoProvider 
            => AudioMixers<T>().Inputs;

        public static AudioMixer<AudioMixer> MasterMixer { get; private set; }

        /// <summary>
        /// Initializes the audio engine with basic mixers for <see cref="Song"/>s and <see cref="SoundEffect"/>s, and the default PortAudio output.
        /// </summary>
        /// <param name="shouldShutdown">The function to determine when the audio engine should shut down. Have the delegate return true when your game is being/has been closed.</param>
        /// <param name="masterVolume">The master mixer volume.</param>
        /// <param name="musicVolume">The song mixer volume.</param>
        /// <param name="soundEffectVolume">The sound effect mixer volume.</param>
        /// <param name="deviceIndex">The index of the output device you would like to play to.</param>
        /// <param name="latency">The desired latency, in ms.<br/>
        /// Higher latency will create slightly delayed audio playback, but can help reduce laggy/choppy audio when lots of post-processing or filters are involved.<br/>
        /// 100ms-150ms is typically fine for most purposes.</param>
        public static void Initialize(
            Func<bool> shouldShutdown,
            float masterVolume = 1f,
            float musicVolume = 1f,
            float soundEffectVolume = 1f,
            int? deviceIndex = null,
            double? latency = null)
        {
            Initialize(
                shouldShutdown,
                masterVolume,
                new() {[typeof(Song)] = musicVolume, [typeof(SoundEffect)] = soundEffectVolume},
                deviceIndex,
                latency);
        }

        /// <summary>
        /// Initializes the MonoStereo Audio Engine. Note that using this method overload will default to PortAudio as an output source.<br/>
        /// If you would like to supply your own output source, use <see cref="InitializeCustomOutput"/> instead.
        /// </summary>
        /// <param name="shouldShutdown">The function to determine when the audio engine should shut down. Have the delegate return true when your game is being/has been closed.</param>
        /// <param name="masterVolume">The master mixer volume</param>
        /// /// <param name="audioMixerTypesAndVolumes">The types for which you want to set up mixers in <see cref="AudioMixers{T}"/>. Must all inherit from <see cref="MonoStereoProvider"/>.<br/>
        /// Correct format is: <code> {
        ///     [Type] = volume,
        ///     [Type] = volume,
        ///     ....
        /// }
        /// </code> where all types inherit from <see cref="MonoStereoProvider"/></param>
        /// /// <param name="deviceIndex">The index of the output device you would like to play to.</param>
        /// <param name="latency">The desired latency, in ms.<br/>
        /// Higher latency will create slightly delayed audio playback, but can help reduce laggy/choppy audio when lots of post-processing or filters are involved.<br/>
        /// 100ms-150ms is typically fine for most purposes.</param>
        public static void Initialize(
            Func<bool> shouldShutdown,
            float masterVolume,
            Dictionary<Type, float> audioMixerTypesAndVolumes,
            int? deviceIndex = null,
            double? latency = null)
        {
            // Initialize to PortAudio output by default.
            // Passing in null utilizes the system defaults.
            PortAudioOutput output = DefaultOutput(deviceIndex, latency);
            InitializeCustomOutput(output, shouldShutdown, masterVolume, audioMixerTypesAndVolumes);
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
        /// <param name="audioMixerTypesAndVolumes">The types for which you want to set up mixers in <see cref="AudioMixers{T}"/>. Must all inherit from <see cref="MonoStereoProvider"/>.<br/>
        /// Correct format is: <code> {
        ///     [Type] = volume,
        ///     [Type] = volume,
        ///     ....
        /// }
        /// </code> where all types inherit from <see cref="MonoStereoProvider"/></param>
        public static void InitializeCustomOutput(
            IMonoStereoOutput customOutput,
            Func<bool> shouldShutdown,
            float masterVolume,
            [NotNull] Dictionary<Type, float> audioMixerTypesAndVolumes)
        {
            MasterMixer = new(masterVolume);
            Output = customOutput;
            
            var addMixerMethod = typeof(AudioManager).GetMethod(nameof(AddAudioMixer), BindingFlags.Static | BindingFlags.Public, [typeof(float)]);
            
            foreach ((Type type, float volume) in audioMixerTypesAndVolumes)
            {
                var addMixerGeneric = addMixerMethod!.MakeGenericMethod(type);
                addMixerGeneric.Invoke(null, [volume]);
            }
            
            _playbackError = null;
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
                _playbackError = ex;
            }

            finally
            {
                // After the `shouldShutdown` function has returned true,
                // commence shutdown. This is done in a way such that the engine
                // COULD be started again without the need for program reload.
                Output.Dispose();
                MasterMixer.Dispose();
                
                foreach (var mixer in audioMixers.Values)
                    mixer.Dispose();
                
                IsRunning = false;
            }
        }

        internal static void Update()
        {
            Output.Update();

            lock (audioMixers)
            {
                foreach (var mixer in audioMixers.Values)
                {
                    lock (mixer.MixerSources)
                    {
                        var inputs = mixer.MixerSources.Cast<MonoStereoProvider>().ToList();

                        foreach (MonoStereoProvider input in inputs.Where(input => input.PlaybackState == PlaybackState.Stopped))
                            input.RemoveInput();
                    }
                }
            }
        }
    }
}
