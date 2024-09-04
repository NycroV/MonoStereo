using NAudio.Wave;

namespace MonoStereo.SampleProviders
{
    public class AudioMixer : MonoStereoProvider
    {
        public MixerSampleProvider Inputs { get; private set; }

        public override WaveFormat WaveFormat => Inputs.WaveFormat;

        public override PlaybackState PlaybackState { get => PlaybackState.Playing; set { } }

        public AudioMixer(float volume, params ISampleProvider[] initialInputs)
        {
            Inputs = new(WaveFormat.CreateIeeeFloatWaveFormat(AudioStandards.SampleRate, AudioStandards.ChannelCount)) { ReadFully = true };

            Volume = volume;

            Inputs.SetMixerInputs(initialInputs);
        }

        public override void Play() => AudioManager.MasterMixer.AddInput(this);

        public void AddInput(ISampleProvider sampleProvider) => Inputs.AddMixerInput(sampleProvider);

        public void RemoveInput(ISampleProvider sampleProvider) => Inputs.RemoveMixerInput(sampleProvider);

        public override int ReadSource(float[] buffer, int offset, int count) => Inputs.Read(buffer, offset, count);

        public override void Close() => Inputs.Dispose();
    }
}
