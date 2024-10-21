namespace MonoStereo.Filters
{
    public class PanFilter(float pan = 0f) : AudioFilter
    {
        public float Panning { get; set; } = pan;

        public override void PostProcess(float[] buffer, int offset, int samplesRead) => Pan(buffer, offset, samplesRead, Panning);

        public static void Pan(float[] buffer, int offset, int count, float panning)
        {
            if (panning == 0f)
                return;

            // The below panning strategy is the same panning strategy used by FAudio.
            // Volume is not only adjusted on left/right channels, but channels are mixed
            // in accordance with where the sound should actually be coming from.

            float leftChannelLeftMultiplier;
            float leftChannelRightMultiplier;

            float rightChannelLeftMultiplier;
            float rightChannelRightMultiplier;

            // On the left...
            if (panning < 0f)
            {
                leftChannelLeftMultiplier = 0.5f * panning + 1f;
                leftChannelRightMultiplier = 0.5f * -panning;

                rightChannelLeftMultiplier = 0f;
                rightChannelRightMultiplier = panning + 1f;
            }

            // On the right...
            else
            {
                leftChannelLeftMultiplier = -panning + 1f;
                leftChannelRightMultiplier = 0f;

                rightChannelLeftMultiplier = 0.5f * panning;
                rightChannelRightMultiplier = 0.5f * -panning + 1f;
            }

            for (int i = 0; i < count; i += 2)
            {
                float leftChannel = buffer[offset + i];
                float rightChannel = buffer[offset + i + 1];

                buffer[offset + i] = (leftChannel * leftChannelLeftMultiplier) + (rightChannel * leftChannelRightMultiplier);
                buffer[offset + i + 1] = (leftChannel * rightChannelLeftMultiplier) + (rightChannel * rightChannelRightMultiplier);
            }
        }
    }
}
