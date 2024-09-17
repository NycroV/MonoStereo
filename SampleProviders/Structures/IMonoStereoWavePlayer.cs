using NAudio.Wave;

namespace MonoStereo
{
    public interface IMonoStereoWavePlayer : IWavePlayer
    {
        public int DesiredLatency { get; }
    }
}
