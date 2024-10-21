using MonoStereo.SampleProviders;
using NAudio.Dsp;
using System.Collections.Generic;

namespace MonoStereo.Filters
{
    public class ReverbFilter : AudioFilter
    {
        /// <summary>
        /// Creates a reverb filter where an echo is heard once every <paramref name="echoDelay"/> milliseconds, decaying by <paramref name="volumeDropoff"/> every time,<br/>
        /// until the total volume reaches 0. Applies <paramref name="allPassCount"/> all pass filters (which "smear" the sound) to the end result.
        /// </summary>
        /// <param name="echoDelay">The delay, in milliseconds, before each echo is heard.</param>
        /// <param name="volumeDropoff">The volume strength which should be lost with each subsequent echo.</param>
        /// <param name="allPassCount">The number of all pass filters to apply to the final result. This "smears" the audio.</param>
        public ReverbFilter(float echoDelay, float volumeDropoff, int allPassCount = 2) : this(BuildCombs(echoDelay, volumeDropoff), allPassCount) { }

        /// <summary>
        /// Creates a reverb filter with echo parameters specified in <paramref name="combs"/>.<br/>
        /// Applies <paramref name="allPassCount"/> all pass filters (which "smear" the sound) to the end result.
        /// </summary>
        /// <param name="combs">All of the comb filters which should be applied.</param>
        /// <param name="allPassCount">The number of all pass filters to apply to the final result. This "smears" the audio.</param>
        public ReverbFilter(Comb[] combs, int allPassCount = 2) : this(combs, BuildAllPassFilters(allPassCount)) { }

        /// <summary>
        /// Creates a reverb filter with echo parameters specified in <paramref name="combs"/>, and all pass filter parameters specified in <paramref name="allPassFilters"/>.
        /// </summary>
        /// <param name="combs">All of the comb filters which should be applied.</param>
        /// <param name="allPassFilters">All of the all pass filters which should be applied.</param>
        public ReverbFilter(Comb[] combs, AllPass[] allPassFilters)
        {
            _combs = combs;
            _allPassFilters = allPassFilters;
        }

        // Stores "recordings" of audio that are queued for playback again to create echos.
        //
        // SmartSampleQueue allows us to place samples at a future index, lining them up to be read back after configurable delay.
        // If the designated placement index is greater than the current queue's length, the queue is filled with silence until it is long enough.
        private readonly Dictionary<MonoStereoProvider, SmartSampleQueue> Echos = [];

        // Stores the collection of filters to be applied to audio
        private readonly Dictionary<MonoStereoProvider, BiQuadFilter[]> Filters = [];

        // Used to make sure filters are not modified mid-read
        private readonly QueuedLock filterLock = new();

        public override void Apply(MonoStereoProvider provider)
        {
            filterLock.Execute(() =>
            {
                Echos.Add(provider, new());
                Filters.Add(provider, BuildBiQuads(allPassFilters));
            });
        }

        public override void Unapply(MonoStereoProvider provider)
        {
            filterLock.Execute(() =>
            {
                Echos[provider].Clear();
                Echos.Remove(provider);
                Filters.Remove(provider);
            });
        }

        public override int ModifyRead(float[] buffer, int offset, int count)
        {
            if (!Echos.TryGetValue(Source, out var echo))
                return base.ModifyRead(buffer, offset, count);

            // Even if the source has reached the end, we want to let the echos "ride out" to completion.
            int read = base.ModifyRead(buffer, offset, count);
            int echoRead = 0;

            filterLock.Execute(() =>
            {
                // The number of samples in one millisecond
                float msOffset = AudioStandards.SampleRate / 1000f;
                
                for (int i = 0; i < read; i++)
                {
                    foreach (Comb comb in combs)
                    {
                        // How many samples we need to go forward to achieve the desired millisecond delay.
                        // Aligning with channel count will be the same for every read unless filter
                        // parameters change - no need to worry about overlapped queueing.
                        int sampleOffset = (int)(msOffset * comb.MillisecondDelay * AudioStandards.ChannelCount);
                        sampleOffset -= sampleOffset % AudioStandards.ChannelCount;

                        // Adds the decayed echo to the queue for later playback again
                        echo[sampleOffset + i] += buffer[offset + i] * (1f - comb.Decay);
                    }
                }

                // Apply the queued echos to the buffer
                for (int i = 0; i < count; i++)
                {
                    // If we've run out of queued samples, that means both the source has reached
                    // its end, and all the echos have fully ridden out.
                    if (!echo.TryDequeue(out float sample))
                        break;

                    buffer[offset + i] += sample;
                    echoRead++;
                }
            });

            // Even if the source has reached its end, it's possible we've
            // added some echos past that.
            return int.Max(read, echoRead);
        }

        // "Smears" the sound by applying all pass filters
        public override void PostProcess(float[] buffer, int offset, int samplesRead)
        {
            if (!Filters.TryGetValue(Source, out var filters))
                return;

            // Apply each BiQuad (all pass) filter in succession
            foreach (var filter in filters)
            {
                for (int i = 0; i < samplesRead; i++)
                    buffer[offset + i] = filter.Transform(buffer[offset + i]);
            }
        }

        private Comb[] combs;
        private AllPass[] allPassFilters;

        #region Filtering

        public Comb[] Combs
        {
            get => _combs;
            set => _combs = value;
        }

        public AllPass[] AllPassFilters
        {
            get => _allPassFilters;
            set => _allPassFilters = value;
        }

        // Setting this value preserves standardized volume dropoff
        public float EchoDelay
        {
            get => _combs.Length > 0 ? _combs[0].MillisecondDelay : 0.0f;
            set => _combs = BuildCombs(value, VolumeDropoff);
        }

        // Setting this value preserves standardized echo delay
        public float VolumeDropoff
        {
            get => _combs.Length > 0 ? _combs[0].Decay : 1.0f;
            set => _combs = BuildCombs(EchoDelay, value);
        }

        // Changes the number of all pass filters to be applied
        public int AllPassCount
        {
            get => _allPassFilters.Length;
            set => _allPassFilters = BuildAllPassFilters(value);
        }

        // Don't need to do any work here as echos are handled through "recording"
        private Comb[] _combs
        {
            get => combs;
            set => filterLock.Execute(() => combs = value);
        }

        // This makes sure all BiQuad filters are properly set
        private AllPass[] _allPassFilters
        {
            get => allPassFilters;
            set
            {
                filterLock.Execute(() =>
                {
                    allPassFilters = value;

                    foreach (var source in Filters.Keys)
                    {
                        var filters = BuildBiQuads(allPassFilters);
                        Filters[source] = filters;
                    }
                });
            }
        }

        // No actual data is stored here, just information about HOW we should store data.
        public struct Comb(float echoDelay, float volumeDropoff)
        {
            public float MillisecondDelay = echoDelay;
            public float Decay = volumeDropoff;
        }

        // Same as above.
        public struct AllPass(float centerFreq, float q)
        {
            public float CenterFreq = centerFreq;
            public float Q = q;
        }

        // Builds an array of comb filter data where each filter is delayed
        // by an increasingly incremented multiple of echoDelay milliseconds, and
        // has a decay (volume reduction) that is increasingly incremented by volumeDropoff.
        //
        // For example:
        //
        // echoDelay - 10ms
        // volumeDropoff - 0.2
        //
        // Comb 1: 10ms delay, 0.8 volume
        // Comb 2: 20ms delay, 0.6 volume
        // Comb 3: 30ms delay, 0.4 volume
        // Comb 4: 40ms delay, 0.2 volume
        //
        // Once volume would reach 0 or below, no more combs are added.
        //
        private static Comb[] BuildCombs(float echoDelay, float volumeDropoff)
        {
            List<Comb> combs = [];

            float delay = 0f;
            float decay = 0f;

            for (; decay < 1f; decay += volumeDropoff)
            {
                delay += echoDelay;
                combs.Add(new(delay, decay));
            }

            return combs.ToArray();
        }

        // Constructs an array of all pass data where each filter
        // has a center frequency of 8.5kHz (just below midtones) and
        // a Q (curve factor) of 0.7
        private static AllPass[] BuildAllPassFilters(int allPassCount)
        {
            List<AllPass> passes = [];

            for (int i = 0; i < allPassCount; i++)
                passes.Add(new(8500f, 0.7f));

            return passes.ToArray();
        }

        // Turns a collection of filter parameters into actual filter objects.
        private static BiQuadFilter[] BuildBiQuads(AllPass[] allPassFilters)
        {
            List<BiQuadFilter> filters = [];

            for (int i = 0; i < allPassFilters.Length; i++)
            {
                AllPass pass = allPassFilters[i];
                filters.Add(BiQuadFilter.AllPassFilter(AudioStandards.SampleRate, pass.CenterFreq, pass.Q));
            }

            return filters.ToArray();
        }

        // Provides easy access for queueing echos for future playback.
        // For more info on the specifics of what this actually does,
        // read above where the SmartSampleQueue is instantiated.
        private class SmartSampleQueue
        {
            private readonly List<float> samples = [];

            public float this[int index]
            {
                get => samples.Count <= index ? 0f : samples[index];
                set
                {
                    int slotsNeeded = index + 1;

                    while (samples.Count < slotsNeeded)
                        samples.Add(0f);

                    samples[index] = value;
                }
            }

            public bool TryDequeue(out float sample)
            {
                if (samples.Count > 0)
                {
                    sample = samples[0];
                    samples.RemoveAt(0);
                    return true;
                }

                sample = default;
                return false;
            }

            public void Clear() => samples.Clear();
        }

        #endregion
    }
}
