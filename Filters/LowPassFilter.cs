﻿using NAudio.Dsp;

namespace MonoStereo.Filters
{
    public class LowPassFilter(float cutoffFrequency = 500f, float q = 0.7f) : AudioFilter
    {
        public float CutoffFrequency
        {
            get => _cutoffFrequency;
            set
            {
                filter.SetLowPassFilter(AudioStandards.SampleRate, value, Q);
                _cutoffFrequency = value;
            }
        }

        public float Q
        {
            get => _q;
            set
            {
                filter.SetLowPassFilter(AudioStandards.SampleRate, CutoffFrequency, value);
                _q = value;
            }
        }

        private float _cutoffFrequency = cutoffFrequency;

        private float _q = q;

        private readonly BiQuadFilter filter = BiQuadFilter.LowPassFilter(AudioStandards.SampleRate, cutoffFrequency, q);

        public override void PostProcess(float[] buffer, int offset, int samplesRead)
        {
            for (int i = 0; i < samplesRead; i++)
                buffer[i] = filter.Transform(buffer[i]);
        }
    }
}
