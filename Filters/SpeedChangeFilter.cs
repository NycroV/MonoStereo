using NAudio.Dsp;

namespace MonoStereo.Filters
{
    public class SpeedChangeFilter : AudioFilter
    {
        public SpeedChangeFilter(float speed = 1f)
        {
            _speed = speed;
            resampler.SetMode(true, 2, false);
            resampler.SetFilterParms();
            resampler.SetFeedMode(false); // output driven
            resampler.SetRates(AudioStandards.SampleRate, AudioStandards.SampleRate / speed);
        }

        public override FilterPriority Priority => FilterPriority.ApplyLast;
        private readonly WdlResampler resampler = new();

        private float _speed;
        public float Speed
        {
            get
            {
                return _speed;
            }
            set
            {
                if (_speed == value)
                    return;

                _speed = value;
                resampler.SetRates(AudioStandards.SampleRate, AudioStandards.SampleRate / _speed);
            }
        }

        public override int ModifyRead(float[] buffer, int offset, int count)
        {
            if (_speed != 1f)
            {
                int framesRequested = count / AudioStandards.ChannelCount;
                int inNeeded = resampler.ResamplePrepare(framesRequested, AudioStandards.ChannelCount, out float[] inBuffer, out int inBufferOffset);

                int inAvailable = Provider.Read(inBuffer, inBufferOffset, inNeeded * AudioStandards.ChannelCount) / AudioStandards.ChannelCount;
                int outAvailable = resampler.ResampleOut(buffer, offset, inAvailable, framesRequested, AudioStandards.ChannelCount);

                return outAvailable * AudioStandards.ChannelCount;
            }

            return base.ModifyRead(buffer, offset, count);
        }      
    }
}
