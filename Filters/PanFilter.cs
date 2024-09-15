namespace MonoStereo.Filters
{
    public class PanFilter(float pan = 0f) : AudioFilter
    {
        public float Panning { get; set; } = pan;

        public override void PostProcess(float[] buffer, int offset, int samplesRead)
        {
            if (Panning == 0f)
                return;

            // The below panning strategy is the same panning strategy used by FAudio.
            // Volume is not only adjusted on left/right channels, but channels are mixed
            // in accordance with where the sound should actually be coming from.

            float leftChannelLeftMultiplier;
            float leftChannelRightMultiplier;

            float rightChannelLeftMultiplier;
            float rightChannelRightMultiplier;

            // On the left...
            if (Panning < 0f)
            {
                leftChannelLeftMultiplier = 0.5f * Panning + 1f;
                leftChannelRightMultiplier = 0.5f * -Panning;

                rightChannelLeftMultiplier = 0f;
                rightChannelRightMultiplier = Panning + 1f;
            }

            // On the right...
            else
            {
                leftChannelLeftMultiplier = -Panning + 1f;
                leftChannelRightMultiplier = 0f;

                rightChannelLeftMultiplier = 0.5f * Panning;
                rightChannelRightMultiplier = 0.5f * -Panning + 1f;
            }

            for (int i = 0; i < samplesRead; i += 2)
            {
                float leftChannel = buffer[offset + i];
                float rightChannel = buffer[offset + i + 1];

                buffer[offset + i] = (leftChannel * leftChannelLeftMultiplier) + (rightChannel * leftChannelRightMultiplier);
                buffer[offset + i + 1] = (leftChannel * rightChannelLeftMultiplier) + (rightChannel * rightChannelRightMultiplier);
            }
        }
    }
}
