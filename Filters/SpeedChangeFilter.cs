using NAudio.Dsp;

namespace MonoStereo.Filters
{
    public class SpeedChangeFilter : AudioFilter
    {
        private float speed;

        public float Speed
        {
            get => speed;
            set
            {
                speed = value;
                resampler.SetRates(AudioStandards.StandardSampleRate, AudioStandards.StandardSampleRate / speed);
            }
        }

        private readonly WdlResampler resampler = new();

        public SpeedChangeFilter(float speed)
        {
            resampler = new WdlResampler();
            resampler.SetMode(interp: true, 2, sinc: false);
            resampler.SetFilterParms();
            resampler.SetFeedMode(wantInputDriven: false);
            Speed = speed;
        }

        public override int ModifyRead(float[] buffer, int offset, int count)
        {
            int num = count / AudioStandards.StandardChannelCount;
            int num2 = resampler.ResamplePrepare(num, AudioStandards.StandardChannelCount, out float[] inbuffer, out int inbufferOffset);

            int nsamples_in = Provider.Read(inbuffer, inbufferOffset, num2 * AudioStandards.StandardChannelCount) / AudioStandards.StandardChannelCount;
            return resampler.ResampleOut(buffer, offset, nsamples_in, num, AudioStandards.StandardChannelCount) * AudioStandards.StandardChannelCount;
        }
    }
}
