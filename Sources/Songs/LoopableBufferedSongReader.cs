namespace MonoStereo.AudioSources.Songs
{
    public class LoopableBufferedSongReader(ILoopableSongSource source, float secondsToHold = 5) : BufferedSongReader(source, secondsToHold), ILoopableSongSource
    {
        private readonly ILoopableSongSource LoopedSource = source;

        public long LoopStart => LoopedSource.LoopStart;

        public long LoopEnd => LoopedSource.LoopEnd;

        public bool IsLooped
        {
            get => LoopedSource.IsLooped;
            set => LoopedSource.IsLooped = value;
        }
    }
}
