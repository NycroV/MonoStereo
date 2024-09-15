using NAudio.Dsp;

namespace MonoStereo.Filters
{
    public class TempoChangeFilter : AudioFilter
    {
        public TempoChangeFilter(float tempo = 1f)
        {
            resampler.SetMode(true, 2, false);
            resampler.SetFilterParms();
            resampler.SetFeedMode(false); // output driven
            Tempo = tempo;
        }

        public override FilterPriority Priority => FilterPriority.ApplyLast;
        private readonly WdlResampler resampler = new();

        public float Tempo
        {
            get
            {
                return speed;
            }
            set
            {
                if (speed == value)
                    return;

                lock (pitchLock)
                {
                    speed = value;
                    resampler.SetRates(AudioStandards.SampleRate, AudioStandards.SampleRate / speed);

                    if (value != 0)
                        pitch = 1f / value;

                    else
                        pitch = 1f;
                }
            }
        }

        private float speed = float.NaN;
        private float pitch = float.NaN;
        private readonly object pitchLock = new();

        private float speedCache = float.NaN;
        private float pitchCache = float.NaN;

        private readonly SmbPitchShifter shifterLeft = new();
        private readonly SmbPitchShifter shifterRight = new();

        public override int ModifyRead(float[] buffer, int offset, int count)
        {
            lock (pitchLock)
            {
                speedCache = speed;
                pitchCache = pitch;
            }

            if (speedCache == 1f)
                return base.ModifyRead(buffer, offset, count);

            if (speedCache == 0f)
            {
                for (int i = 0; i < count; i++)
                    buffer[offset + i] = 0f;

                return count;
            }

            int framesRequested = count / AudioStandards.ChannelCount;
            int inNeeded = resampler.ResamplePrepare(framesRequested, AudioStandards.ChannelCount, out float[] inBuffer, out int inBufferOffset);

            int inAvailable = Provider.Read(inBuffer, inBufferOffset, inNeeded * AudioStandards.ChannelCount) / AudioStandards.ChannelCount;
            int outAvailable = resampler.ResampleOut(buffer, offset, inAvailable, framesRequested, AudioStandards.ChannelCount);

            return outAvailable * AudioStandards.ChannelCount;
        }

        public override void PostProcess(float[] buffer, int offset, int samplesRead) => PitchShiftFilter.PitchShift(pitchCache, shifterLeft, shifterRight, buffer, offset, samplesRead);
    }
}