using NAudio.Wave;

namespace MonoStereo.SampleProviders
{
    public class AudioMixer : MonoStereoProvider
    {
        public MixerSampleProvider Inputs { get; private set; }

        public override WaveFormat WaveFormat => Inputs.WaveFormat;

        public AudioMixer(float volume, params ISampleProvider[] initialInputs)
        {
            Inputs = new(WaveFormat.CreateIeeeFloatWaveFormat(AudioStandards.StandardSampleRate, AudioStandards.StandardChannelCount)) { ReadFully = true };

            Volume = volume;

            foreach (var input in initialInputs)
                Inputs.AddMixerInput(input);
        }

        public void AddInput(ISampleProvider sampleProvider) => Inputs.AddMixerInput(sampleProvider);

        public void RemoveInput(ISampleProvider sampleProvider) => Inputs.RemoveMixerInput(sampleProvider);

        public override int ReadSource(float[] buffer, int offset, int count) => Inputs.Read(buffer, offset, count);
    }
}
