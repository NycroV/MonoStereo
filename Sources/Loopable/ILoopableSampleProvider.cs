using MonoStereo.SampleProviders;

namespace MonoStereo.AudioSources
{
    public interface ILoopableSampleProvider : ISeekableSampleProvider
    {
        public long LoopStart { get; }

        public long LoopEnd { get; }

        public bool IsLooped { get; set; }
    }
}
