using System;
using System.Collections.Generic;
using System.IO;
using ATL;
using MonoStereo.Decoding;
using MonoStereo.Sources.Songs;
using MonoStereo.Sources.Sounds;
using MonoStereo.Structures;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace MonoStereo.Sources;

public class UniversalAudioSource : ISeekableSongSource, ISeekableSoundEffectSource, ILoopTags
{
    #region Source Access
    
    private readonly WaveStream _waveStream;
    private readonly LoopingReader _loopingReader;
    private ISampleProvider _source;

    #endregion

    #region  Public Data
    
    public WaveFormat WaveFormat => _source.WaveFormat;

    public PlaybackState PlaybackState { get; set; }
    public Dictionary<string, string> Comments { get; }
    public bool IsLooped { get; set; }
    
    #endregion

    public UniversalAudioSource(string fileName, bool useSoundEffectDecoderForXnb = false)
    {
        
        if (string.IsNullOrEmpty(Path.GetExtension(fileName)))
            fileName += ".xnb";

        string fileExtension = Path.GetExtension(fileName);
        Stream fileStream = File.OpenRead(fileName);

        switch (fileExtension)
        {
            case ".xnb":
                _waveStream = useSoundEffectDecoderForXnb ?
                    new SoundEffectReader(fileStream) :
                    new OggReader(fileStream);
            
            case ".ogg":
                var stream = new OggReader(fileStream);
                _waveStream = stream;
                Comments = stream.Comments.ComposeComments();
                break;

            case ".wav":
                Comments = ReadComments(fileStream);
                _waveStream = new WaveFileReader(fileStream);
                break;

            case ".mp3":
                Comments = ReadComments(fileStream);
                _waveStream = new Mp3Reader(fileStream);
                break;

            default:
                throw new FileLoadException($"Unknown audio extension {fileExtension}");
        }
        
        Comments.ParseLoop(out long loopStart, out long loopEnd, AudioStandards.ChannelCount);

        _loopingReader = new(_waveStream, loopStart, loopEnd);
        long sourceLength = _loopingReader.Length;

        // Resampled reading
        long scaledLoopStart = loopStart;
        long scaledLoopEnd = loopEnd;
        long scaledLength = sourceLength;

        _source = Reformat(_loopingReader, ref scaledLoopStart, ref scaledLoopEnd, ref scaledLength);
        _sampleScalar = (float)_loopingReader.WaveFormat.SampleRate / _source.WaveFormat.SampleRate;

        LoopStart = scaledLoopStart;
        LoopEnd = scaledLoopEnd;
        Length = scaledLength;
    }
    
    public long Position
    {
        get
        {
            long samplePos = (long)(_loopingReader.Position / _sampleScalar);
            samplePos -= samplePos % AudioStandards.ChannelCount;

            return samplePos;
        }

        set
        {
            long samplePos = (long)(value * _sampleScalar);
            samplePos -= samplePos % AudioStandards.ChannelCount;

            _loopingReader.Position = samplePos;
        }
    }
    
    public long Length { get; }
    
    public long LoopStart { get; }
    public long LoopEnd { get; }

    private readonly float _sampleScalar;
    
    public int Read(float[] buffer, int offset, int count) => _source.Read(buffer, offset, count);

    public void Dispose()
    {
        _source = null;
        _waveStream?.Dispose();
        GC.SuppressFinalize(this);
    }
    
    #region Provider Standardization
    
    // Converts an ISampleProvider of any formatting (sample rate or channels) to
    // a standardized, 44.1kHz 2 channel stream. Also modifies loopStart and loopEnd
    // tags to account for sample size adjustment.
    public static ISampleProvider Reformat(ISampleProvider provider, ref long loopStart, ref long loopEnd, ref long length)
    {
        if (provider.WaveFormat.SampleRate != AudioStandards.SampleRate)
        {
            float scalar = AudioStandards.SampleRate / (float)provider.WaveFormat.SampleRate;
            provider = new WdlResamplingSampleProvider(provider, AudioStandards.SampleRate);

            if (loopStart > 0)
            {
                loopStart = (long)(loopStart * scalar);
                loopStart -= loopStart % provider.WaveFormat.Channels;
            }

            if (loopEnd > 0)
            {
                loopEnd = (long)(loopEnd * scalar);
                loopEnd -= loopEnd % provider.WaveFormat.Channels;
            }

            length = (long)(length * scalar);
            length -= length % provider.WaveFormat.Channels;
        }

        if (provider.WaveFormat.Channels != AudioStandards.ChannelCount)
        {
            if (provider.WaveFormat.Channels == 1)
                provider = new MonoToStereoSampleProvider(provider);

            else
                throw new ArgumentException("Audio file must be in either mono or stereo!", nameof(provider));

            length *= AudioStandards.ChannelCount;
        }

        return provider;
    }
    
    // Reads comments of a track that is NOT a .ogg file.
    // We use ATL for this.
    public static Dictionary<string, string> ReadComments(Stream stream)
    {
        long position = stream.Position;
        Dictionary<string, string> comments = [];

        Track track = new(stream);

        AddComment("Artist", track.Artist);
        AddComment("Title", track.Title);
        AddComment("Album", track.Album);
        AddComment("Comments", track.Comment);

        foreach (var keyValuePair in track.AdditionalFields)
            AddComment(keyValuePair.Key, keyValuePair.Value);

        if (stream.CanSeek)
            stream.Position = position;

        return comments;

        void AddComment(string name, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                comments.Add(name, value);
        }
    }
    
    #endregion
    
    /// <summary>
    /// Loops the reading of a provided source dependent on looping tags.
    /// </summary>
    private class LoopingReader(WaveStream waveStream, long loopStart, long loopEnd) : ISampleProvider, ISeekable, ILoopTags
    {
        public readonly WaveStream WaveSource = waveStream;

        // Some classes implement both WaveStream and ISampleProvider (like the Ogg reader.)
        // We want to avoid as much unnecessary conversion as possible.
        public readonly ISampleProvider OutputSource = waveStream is ISampleProvider output ? output : waveStream.ToSampleProvider();

        public WaveFormat WaveFormat => OutputSource.WaveFormat;
        
        private readonly QueuedLock _seekLock = new();

        public long LoopStart { get; set; } = loopStart;

        public long LoopEnd { get; set; } = loopEnd;

        public bool IsLooped { get; set; } = false;

        public long Position
        {
            get
            {
                long value = 0;
                _seekLock.Execute(() =>
                {
                    value = WaveSource.Position / WaveSource.WaveFormat.BlockAlign * WaveSource.WaveFormat.Channels;
                });
                return value;
            }

            set
            {
                _seekLock.Execute(() =>
                {
                    WaveSource.Position = value / WaveSource.WaveFormat.Channels * WaveSource.WaveFormat.BlockAlign;
                });
            }
        }

        public long Length => WaveSource.Length / WaveSource.WaveFormat.BlockAlign * WaveSource.WaveFormat.Channels;

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesCopied = 0;

            _seekLock.Execute(() => {
                do
                {
                    long endIndex = Length;

                    if (IsLooped && LoopEnd != -1 && Position < LoopEnd)
                        endIndex = LoopEnd;

                    long samplesAvailable = endIndex - Position;
                    long samplesRemaining = count - samplesCopied;

                    int samplesToCopy = (int)Math.Min(samplesAvailable, samplesRemaining);

                    if (samplesToCopy > 0)
                        samplesCopied += OutputSource.Read(buffer, offset + samplesCopied, samplesToCopy);

                    if (IsLooped && Position >= endIndex)
                    {
                        long startIndex = Math.Max(0, LoopStart);
                        Position = startIndex;
                    }
                } while (IsLooped && samplesCopied < count);
            });

            return samplesCopied;
        }
    }
}