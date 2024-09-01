using ATL;
using MonoStereo.Encoding;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;

namespace MonoStereo.Pipeline
{
    public class AudioFileReader : WaveStream, ISampleProvider
    {
        private bool vorbisBased { get; set; }

        private OggReader vorbisReader { get; set; }

        private NAudio.Wave.AudioFileReader fileReader { get; set; }

        public string FileName { get; }

        public Dictionary<string, string> Comments { get; set; }

        public override WaveFormat WaveFormat
        {
            get
            {
                if (vorbisBased)
                    return vorbisReader.WaveFormat;

                else
                    return fileReader.WaveFormat;
            }
        }

        public override long Length
        {
            get
            {
                if (vorbisBased)
                    return vorbisReader.Length;

                else
                    return fileReader.Length;
            }
        }

        public override long Position
        {
            get
            {
                if (vorbisBased)
                    return vorbisReader.Position;

                else
                    return fileReader.Position;
            }

            set
            {
                if (vorbisBased)
                    vorbisReader.Position = value;

                else
                    fileReader.Position = value;
            }
        }

        public AudioFileReader(string fileName)
        {
            FileName = fileName;

            if (Path.GetExtension(fileName).Equals(".ogg", StringComparison.OrdinalIgnoreCase))
            {
                vorbisBased = true;
                vorbisReader = new OggReader(fileName);
                Comments = vorbisReader.Comments.ComposeComments();
            }

            else
            {
                Comments = [];
                void AddComment(string name, string value)
                {
                    if (!string.IsNullOrWhiteSpace(value))
                        Comments.Add(name, value);
                }

                Track track = new(fileName);

                AddComment("Artist", track.Artist);
                AddComment("Title", track.Title);
                AddComment("Album", track.Album);
                AddComment("Comments", track.Comment);

                foreach (var keyValuePair in track.AdditionalFields)
                    AddComment(keyValuePair.Key, keyValuePair.Value);

                vorbisBased = false;
                fileReader = new NAudio.Wave.AudioFileReader(fileName);
            }
        }

        public int Read(float[] buffer, int offset, int count) => vorbisBased ? vorbisReader.Read(buffer, offset, count) : fileReader.Read(buffer, offset, count);

        public override int Read(byte[] buffer, int offset, int count) => vorbisBased ? vorbisReader.Read(buffer, offset, count) : fileReader.Read(buffer, offset, count);
    }
}
