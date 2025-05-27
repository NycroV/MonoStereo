using MonoStereo.Structures;
using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Collections.Generic;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using JetBrains.Annotations;

namespace MonoStereo.Structures.SampleProviders
{
    public class AudioMixer<T> : AudioMixer
        where T : ISampleProvider
    {
        public AudioMixer(float volume, params T[] initialInputs) : base(volume, initialInputs.Cast<ISampleProvider>())
        {
            Source.MixerInputEnded += (sender, args) =>
            {
                foreach (var callback in _inputEndedCallbacks)
                    callback(sender, (T)args.SampleProvider);
            };
        }

        public ReadOnlyCollection<T> Inputs
        {
            get
            {
                lock (MixerSources)
                { return Source.MixerInputs.Cast<T>().ToList().AsReadOnly(); }
            }
        }

        public void AddInput(T input) => Source.AddMixerInput(input);
        
        public void RemoveInput(T input) => Source.RemoveMixerInput(input);

        public void SetMixerInputs(IEnumerable<T> inputs) => Source.SetMixerInputs(inputs.Cast<ISampleProvider>());
        
        public void ClearMixerInputs() => Source.ClearMixerInputs();
        
        /// <summary>
        /// Raised when a mixer input has finished playback
        /// </summary>
        public event EventHandler<T> MixerInputEnded
        {
            add => _inputEndedCallbacks.Add(value);
            remove => _inputEndedCallbacks.Remove(value);
        }

        private readonly List<EventHandler<T>> _inputEndedCallbacks = [];
    }

    public class AudioMixer : MonoStereoProvider
    {
        protected MixerSampleProvider Source { get; }
        
        public List<ISampleProvider> MixerSources => Source.MixerInputs;

        public override WaveFormat WaveFormat => Source.WaveFormat;

        public override PlaybackState PlaybackState { get; set; } = PlaybackState.Playing;

        protected AudioMixer(float volume, IEnumerable<ISampleProvider> initialInputs)
        {
            Source = new(WaveFormat.CreateIeeeFloatWaveFormat(AudioStandards.SampleRate, AudioStandards.ChannelCount)) { ReadFully = true };

            Volume = volume;

            Source.SetMixerInputs(initialInputs);
        }

        public override void Play() => Resume();
        
        public override int ReadSource(float[] buffer, int offset, int count) => Source.Read(buffer, offset, count);

        public override void RemoveInput() => MonoStereoEngine.MasterMixer.RemoveInput(this);

        public override void Dispose() => Source.Dispose();

        /// <summary>
        /// A sample provider mixer, allowing inputs to be added and removed
        /// </summary>
        protected class MixerSampleProvider : ISampleProvider, IDisposable
        {
            [NotNull] private readonly List<ISampleProvider> _sources = [];
            private float[] _sourceBuffer;
            
            /// <summary>
            /// Returns the mixer inputs (read-only - use AddMixerInput to add an input
            /// </summary>
            // ReSharper disable once InconsistentlySynchronizedField
            // Intentional direct object reference.
            public List<ISampleProvider> MixerInputs => _sources;

            /// <summary>
            /// Creates a new MixingSampleProvider, with no inputs, but a specified WaveFormat
            /// </summary>
            /// <param name="waveFormat">The WaveFormat of this mixer. All inputs must be in this format</param>
            public MixerSampleProvider(WaveFormat waveFormat)
            {
                if (waveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
                {
                    throw new ArgumentException("Mixer wave format must be IEEE float");
                }

                WaveFormat = waveFormat;
            }

            /// <summary>
            /// Creates a new MixingSampleProvider, based on the given inputs
            /// </summary>
            /// <param name="sources">Mixer inputs - must all have the same waveformat, and must
            /// all be of the same WaveFormat. There must be at least one input</param>
            public MixerSampleProvider(IEnumerable<ISampleProvider> sources)
            {
                _sources = [];
                SetMixerInputs(sources);
            }

            /// <summary>
            /// When set to true, the Read method always returns the number
            /// of samples requested, even if there are no inputs, or if the
            /// current inputs reach their end. Setting this to true effectively
            /// makes this a never-ending sample provider, so take care if you plan
            /// to write it out to a file.
            /// </summary>
            public bool ReadFully { get; init; }

            /// <summary>
            /// Adds a new mixer input
            /// </summary>
            /// <param name="mixerInput">Mixer input</param>
            public void AddMixerInput(ISampleProvider mixerInput)
            {
                if (WaveFormat == null)
                {
                    WaveFormat = mixerInput.WaveFormat;
                }
                else
                {
                    if (WaveFormat.SampleRate != mixerInput.WaveFormat.SampleRate ||
                        WaveFormat.Channels != mixerInput.WaveFormat.Channels)
                    {
                        throw new ArgumentException("All mixer inputs must have the same WaveFormat");
                    }
                }
                
                // we'll just call the lock around add since we are protecting against an AddMixerInput at
                // the same time as a Read, rather than two AddMixerInput calls at the same time
                lock (_sources)
                {
                    if (_sources.Count >= AudioStandards.MaxMixerInputs)
                    {
                        return;
                    }

                    _sources.Add(mixerInput);
                }
            }
            
            /// <summary>
            /// Removes a mixer input
            /// </summary>
            /// <param name="mixerInput">Mixer input</param>
            public void RemoveMixerInput(ISampleProvider mixerInput)
            {
                lock (_sources)
                {
                    _sources.Remove(mixerInput);
                }
            }
            
            /// <summary>
            /// Sets the samples providers to be used by this mixer
            /// </summary>
            /// <param name="sources">Mixer inputs</param>
            public void SetMixerInputs(IEnumerable<ISampleProvider> sources)
            {
                lock (_sources)
                {
                    _sources.Clear();
                    _sources.AddRange(sources);
                }
            }

            /// <summary>
            /// Removes all mixer inputs
            /// </summary>
            public void ClearMixerInputs()
            {
                lock (_sources)
                {
                    _sources.Clear();
                }
            }

            /// <summary>
            /// Raised when a mixer input has finished playback
            /// </summary>
            public event EventHandler<SampleProviderEventArgs> MixerInputEnded;
            
            /// <summary>
            /// The output WaveFormat of this sample provider
            /// </summary>
            public WaveFormat WaveFormat { get; private set; }

            /// <summary>
            /// Reads samples from this sample provider
            /// </summary>
            /// <param name="buffer">Sample buffer</param>
            /// <param name="offset">Offset into sample buffer</param>
            /// <param name="count">Number of samples required</param>
            /// <returns>Number of samples read</returns>
            public int Read(float[] buffer, int offset, int count)
            {
                int outputSamples = 0;
                _sourceBuffer = BufferHelpers.Ensure(_sourceBuffer, count);
                lock (_sources)
                {
                    int index = _sources.Count - 1;
                    while (index >= 0)
                    {
                        var source = _sources[index];
                        int samplesRead = source.Read(_sourceBuffer, 0, count);
                        int outIndex = offset;
                        for (int n = 0; n < samplesRead; n++)
                        {
                            if (n >= outputSamples)
                            {
                                buffer[outIndex++] = _sourceBuffer[n];
                            }
                            else
                            {
                                buffer[outIndex++] += _sourceBuffer[n];
                            }
                        }
                        outputSamples = Math.Max(samplesRead, outputSamples);
                        if (samplesRead == 0)
                        {
                            MixerInputEnded?.Invoke(this, new SampleProviderEventArgs(source));
                        }
                        index--;
                    }
                }
                // optionally ensure we return a full buffer
                if (!ReadFully || outputSamples >= count)
                    return outputSamples;
                
                int outputIndex = offset + outputSamples;
                while (outputIndex < offset + count)
                    buffer[outputIndex++] = 0;
                
                outputSamples = count;
                return outputSamples;
            }

            public void Dispose()
            {
                lock (_sources)
                {
                    _sources.Clear();
                    _sourceBuffer = null;
                    WaveFormat = null;
                }
                
                GC.SuppressFinalize(this);
            }
        }
    }
}
