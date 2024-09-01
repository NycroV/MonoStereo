namespace MonoStereo
{
    public static class AudioStandards
    {
        public const int SampleRate = 44100;

        public const int ChannelCount = 2;

        public const int WriteBufferSize = SampleRate;

        public const int ReadBufferSize = SampleRate * ChannelCount;

        public const int BytesPerSample = sizeof(float);

        public const int MaxMixerInputs = 1024;
    }
}
