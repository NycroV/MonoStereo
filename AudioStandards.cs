namespace MonoStereo
{
    public static class AudioStandards
    {
        public const int StandardSampleRate = 44100;

        public const int StandardChannelCount = 2;

        public const int StandardLatency = 100;

        public const int StandardSampleCount = StandardSampleRate / (1000 / StandardLatency);

        public const int BytesPerSample = 4;

        public const int MaxMixerInputs = 1024;
    }
}
