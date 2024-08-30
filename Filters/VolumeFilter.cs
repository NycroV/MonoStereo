namespace MonoStereo.Filters
{
    public class VolumeFilter(float volume) : AudioFilter
    {
        public float VolumeLevel { get; set; } = volume;

        public override void PostProcess(float[] buffer, int offset, int samplesRead)
        {
            if (VolumeLevel == 1f)
                return;

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] *= VolumeLevel;
        }
    }
}
