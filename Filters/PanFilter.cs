using System;

namespace MonoStereo.Filters
{
    public class PanFilter(float pan = 0f) : AudioFilter
    {
        public float Panning { get; set; } = pan;

        public override void PostProcess(float[] buffer, int offset, int samplesRead)
        {
            if (Panning == 0f)
                return;

            float normPan = (-Panning + 1f) / 2f;
            float leftChannel = (float)Math.Sqrt(normPan);
            float rightChannel = (float)Math.Sqrt(1 - normPan);

            for (int i = 0; i < samplesRead; i++)
            {
                if (i % 2 == 0)
                {
                    buffer[offset + i] *= leftChannel;
                }

                else
                {
                    buffer[offset + i] *= rightChannel;
                }
            }
        }
    }
}
