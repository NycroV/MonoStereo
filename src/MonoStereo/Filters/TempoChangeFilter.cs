﻿using MonoStereo.Structures;
using NAudio.Dsp;
using System.Collections.Generic;

namespace MonoStereo.Filters
{
    public class TempoChangeFilter : AudioFilter
    {
        public TempoChangeFilter(float tempo = 1f)
        {
            Tempo = tempo;
        }

        public override FilterPriority Priority => FilterPriority.ApplyLast;
        private readonly Dictionary<MonoStereoProvider, WdlResampler> resamplers = [];
        private readonly Dictionary<MonoStereoProvider, PitchShiftFilter.Set> shifters = [];
        private readonly QueuedLock pitchLock = new();

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

                pitchLock.Execute(() =>
                {
                    speed = value;

                    foreach (var sampler in resamplers)
                        sampler.Value.SetRates(AudioStandards.SampleRate, AudioStandards.SampleRate / speed);

                    if (value != 0)
                        pitch = 1f / value;

                    else
                        pitch = 1f;
                });
            }
        }

        private float speed = float.NaN;
        private float pitch = float.NaN;
        private float pitchCache = float.NaN;

        public override void Apply(MonoStereoProvider provider)
        {
            var resampler = new WdlResampler();
            resampler.SetMode(true, 2, false);
            resampler.SetFilterParms();
            resampler.SetFeedMode(false); // output driven

            pitchLock.Execute(() =>
            {
                resampler.SetRates(AudioStandards.SampleRate, AudioStandards.SampleRate / speed);
                resamplers.Add(provider, resampler);
                shifters.Add(provider, new());
            });
        }

        public override void Unapply(MonoStereoProvider provider)
        {
            pitchLock.Execute(() =>
            {
                resamplers.Remove(provider);
                shifters.Remove(provider);
            });
        }

        public override int ModifyRead(float[] buffer, int offset, int count)
        {
            if (speed == 1f)
                return base.ModifyRead(buffer, offset, count);

            if (speed == 0f)
            {
                for (int i = 0; i < count; i++)
                    buffer[offset + i] = 0f;

                return count;
            }

            int read = 0;

            pitchLock.Execute(() =>
            {
                if (!resamplers.TryGetValue(Source, out var resampler))
                {
                    read = base.ModifyRead(buffer, offset, count);
                    return;
                }

                int framesRequested = count / AudioStandards.ChannelCount;
                int inNeeded = resampler.ResamplePrepare(framesRequested, AudioStandards.ChannelCount, out float[] inBuffer, out int inBufferOffset);

                int inAvailable = base.ModifyRead(inBuffer, inBufferOffset, inNeeded * AudioStandards.ChannelCount) / AudioStandards.ChannelCount;
                int outAvailable = resampler.ResampleOut(buffer, offset, inAvailable, framesRequested, AudioStandards.ChannelCount);

                read = outAvailable * AudioStandards.ChannelCount;
            });

            return read;
        }

        public override void PostProcess(float[] buffer, int offset, int samplesRead) => PitchShiftFilter.PitchShift(pitchCache, shifters[Source], buffer, offset, samplesRead);
    }
}