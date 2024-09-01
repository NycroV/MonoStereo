using Microsoft.Xna.Framework;
using System;

namespace MonoStereo.Filters
{
    public class PositionFilter(Vector2 soundPosition, float listeningRange = 400f) : AudioFilter
    {
        public Vector2 SoundPosition { get; set; } = soundPosition;

        public float ListeningRange { get; set; } = listeningRange;

        const float Pi = 3.1415927f;

        public override void PostProcess(float[] buffer, int offset, int samplesRead)
        {
            if (SoundPosition == Vector2.Zero)
                return;

            Vector2 listener = Vector2.Zero;
            float dist = Vector2.Distance(listener, SoundPosition);

            if (dist > ListeningRange)
            {
                for (int i = offset; i < samplesRead; i++)
                    buffer[i] = 0f;

                return;
            }

            float volume = (float)Math.Cos(dist * Pi / (ListeningRange * 2f));
            float pan = (SoundPosition.X - listener.X) / ListeningRange;

            if (volume != 1f)
            {
                for (int i = 0; i < samplesRead; i++)
                    buffer[offset + i] *= volume;
            }

            if (pan != 0f)
            {
                float normPan = (-pan + 1f) / 2f;
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
}
