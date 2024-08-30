using NAudio.Wave;
using OggVorbisEncoder;
using System.IO;
using System;
using Microsoft.Xna.Framework.Content.Pipeline;

namespace MonoStereo.Pipeline
{
    public class OggWriter
    {
        public string FileName { get; set; }
        public AudioFileReader Reader { get; set; }
        public ContentBuildLogger Logger { get; set; }

        #region Vorbis Stuff

        public void WriteToOgg(ISampleProvider inputStream, Stream outputStream)
        {
            int outputSampleRate = AudioStandards.StandardSampleRate;
            int outputChannels = AudioStandards.StandardChannelCount;
            int pcmChannels = inputStream.WaveFormat.Channels;

            int writeBufferSize = inputStream.WaveFormat.SampleRate;
            float[] pcm = new float[writeBufferSize];

            InitOggStream(outputSampleRate, outputChannels, out OggStream oggStream, out ProcessingState processingState);

            while (true)
            {
                int samplesRead = inputStream.Read(pcm, 0, writeBufferSize);
                if (samplesRead <= 0)
                    break;

                float[][] outSamples = new float[outputChannels][];

                for (int ch = 0; ch < outputChannels; ch++)
                    outSamples[ch] = new float[samplesRead / pcmChannels];

                for (int sampleNumber = 0; sampleNumber < samplesRead; sampleNumber++)
                {
                    if (pcmChannels == 2)
                        outSamples[sampleNumber % 2][sampleNumber / 2] = pcm[sampleNumber];

                    else
                    {
                        for (int ch = 0; ch < outputChannels; ch++)
                            outSamples[ch][sampleNumber] = pcm[sampleNumber];
                    }
                }

                FlushPages(oggStream, outputStream, false);
                ProcessChunk(outSamples, processingState, oggStream, samplesRead);
            }

            FlushPages(oggStream, outputStream, true);
        }

        private static void ProcessChunk(float[][] floatSamples, ProcessingState processingState, OggStream oggStream, int writeBufferSize)
        {
            processingState.WriteData(floatSamples, Math.Min(writeBufferSize, floatSamples[0].Length), 0);

            while (!oggStream.Finished && processingState.PacketOut(out OggPacket packet))
                oggStream.PacketIn(packet);
        }

        private void InitOggStream(int sampleRate, int channels, out OggStream oggStream, out ProcessingState processingState)
        {
            // Stores all the static vorbis bitstream settings
            var info = VorbisInfo.InitVariableBitRate(channels, sampleRate, 0.5f);

            // set up our packet->stream encoder
            var serial = new Random().Next();
            oggStream = new OggStream(serial);

            // =========================================================
            // HEADER
            // =========================================================
            // Vorbis streams begin with three headers; the initial header (with
            // most of the codec setup parameters) which is mandated by the Ogg
            // bitstream spec.  The second header holds any comment fields.  The
            // third header holds the bitstream codebook.
            var comments = new Comments();
            foreach (var comment in Reader.Comments)
                comments.AddTag(comment.Key, comment.Value);

            var infoPacket = HeaderPacketBuilder.BuildInfoPacket(info);
            var commentsPacket = HeaderPacketBuilder.BuildCommentsPacket(comments);
            var booksPacket = HeaderPacketBuilder.BuildBooksPacket(info);

            oggStream.PacketIn(infoPacket);
            oggStream.PacketIn(commentsPacket);
            oggStream.PacketIn(booksPacket);

            // =========================================================
            // BODY (Audio Data)
            // =========================================================
            processingState = ProcessingState.Create(info);
        }

        private static void FlushPages(OggStream oggStream, Stream output, bool force)
        {
            while (oggStream.PageOut(out OggPage page, force))
            {
                output.Write(page.Header, 0, page.Header.Length);
                output.Write(page.Body, 0, page.Body.Length);
            }
        }

        #endregion
    }
}
