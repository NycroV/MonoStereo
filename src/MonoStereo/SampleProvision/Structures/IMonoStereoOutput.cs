using MonoStereo.SampleProviders;

namespace MonoStereo
{
    public interface IMonoStereoOutput
    {
        void Init(AudioMixer waveProvider);

        void Play();

        void Update();

        void Dispose();
    }
}
