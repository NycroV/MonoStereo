﻿using Microsoft.Xna.Framework.Content.Pipeline;
using NAudio.Wave;
using System.IO;

namespace MonoStereo.Pipeline
{
    public class WavWriter
    {
        public string FileName { get; set; }
        public AudioFileReader Reader { get; set; }
        public ContentBuildLogger Logger { get; set; }

        public void WriteToWav(ISampleProvider inputStream, Stream outputStream)
        {
            int writeBufferSize = inputStream.WaveFormat.SampleRate;
            float[] buffer = new float[writeBufferSize];
            BinaryWriter writer = new(outputStream);
            int samplesRead;

            writer.Write(Reader.Comments.Count);
            foreach (var comment in Reader.Comments)
            {
                writer.Write(comment.Key);
                writer.Write(comment.Value);
            }

            do
            {
                samplesRead = inputStream.Read(buffer, 0, writeBufferSize);

                for (int i = 0; i < samplesRead; i++)
                    writer.Write(buffer[i]);
            }
            while (samplesRead == writeBufferSize);
        }
    }
}