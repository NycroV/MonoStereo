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
                resampler.SetRates(AudioStandards.SampleRate, AudioStandards.SampleRate / speed);
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
            int num = count / AudioStandards.ChannelCount;
            int num2 = resampler.ResamplePrepare(num, AudioStandards.ChannelCount, out float[] inbuffer, out int inbufferOffset);

            int nsamples_in = Provider.Read(inbuffer, inbufferOffset, num2 * AudioStandards.ChannelCount) / AudioStandards.ChannelCount;
            return resampler.ResampleOut(buffer, offset, nsamples_in, num, AudioStandards.ChannelCount) * AudioStandards.ChannelCount;
        }
    }
}
