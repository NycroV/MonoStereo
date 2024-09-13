namespace MonoStereo.AudioSources
{
    public interface ILoopableSongSource : ISongSource
    {
        public bool IsLooped { get; set; }

        public long LoopStart { get; }

        public long LoopEnd { get; }
    }
}
