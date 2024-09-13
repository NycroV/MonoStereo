namespace MonoStereo.AudioSources
{
    public interface ILoopableSoundEffectSource : ISoundEffectSource
    {
        public bool IsLooped { get; set; }

        public long LoopStart { get; }

        public long LoopEnd { get; }
    }
}
