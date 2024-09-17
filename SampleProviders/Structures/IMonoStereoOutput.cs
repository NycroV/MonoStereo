using MonoStereo.SampleProviders;

namespace MonoStereo
{
    public interface IMonoStereoOutput
    {
        public int DesiredLatency { get; }

        void Init(AudioMixer waveProvider);

        void Play();

        void Update();

        void Dispose();
    }
}
