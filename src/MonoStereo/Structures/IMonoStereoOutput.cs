using MonoStereo.Structures.SampleProviders;

namespace MonoStereo.Structures
{
    /// <summary>
    /// Represents a custom audio output that is usable by MonoStereo.
    /// </summary>
    public interface IMonoStereoOutput
    {
        /// <summary>
        /// Sets up your output to get ready to start playing. You should not start the actual playback here, only prepare for playback.
        /// </summary>
        /// <param name="masterMixer">This is <see cref="AudioManager.MasterMixer"/>. All of your audio reading calls<br/>
        /// should use this, calling the Read() method whenever you need to fill buffers.</param>
        void Init(AudioMixer masterMixer);

        /// <summary>
        /// Starts the actual playback of your output.
        /// </summary>
        void Play();

        /// <summary>
        /// Use this method to update anything your output may need that is dependent on your game logic.<br/>
        /// You can also use this method to throw any errors that were thrown by the playback thread.<br/>
        /// Throwing from the <see cref="Update"/> loop will cause a safe shutdown, preventing deadlocks and making debugging easier.
        /// </summary>
        void Update();

        /// <summary>
        /// This should close and complete shut down your output, releasing all resources it may be using.
        /// </summary>
        void Dispose();
    }
}
