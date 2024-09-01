using MonoStereo.Pipeline;
using OggVorbisEncoder;
using System;
using System.IO;

namespace MonoStereo.Encoding
{
    public class OggWriter
    {
        public string FileName { get; set; }

        public AudioFileReader Reader { get; set; }

        public int Quality { get; set; }

        public void WriteToOgg(NAudio.Wave.ISampleProvider inputStream, Stream outputStream)
        {
            // Stores all the static vorbis bitstream settings
            float quality = int.Clamp(Quality, 1, 10) / 10f;
            var info = VorbisInfo.InitVariableBitRate(AudioStandards.ChannelCount, AudioStandards.SampleRate, quality);

            // set up our packet->stream encoder
            var serial = new Random().Next();
            var oggStream = new OggStream(serial);

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

            // Flush to force audio data onto its own page per the spec
            FlushPages(oggStream, outputStream, true);

            // =========================================================
            // BODY (Audio Data)
            // =========================================================
            var processingState = ProcessingState.Create(info);
            int samplesRead;

            float[][] outBuffer = new float[AudioStandards.ChannelCount][];
            float[] inBuffer = new float[AudioStandards.ReadBufferSize];

            for (int i = 0; i < outBuffer.Length; i++)
                outBuffer[i] = new float[AudioStandards.WriteBufferSize];

            do
            {
                samplesRead = inputStream.Read(inBuffer, 0, AudioStandards.ReadBufferSize);

                for (int i = 0; i < samplesRead; i++)
                    outBuffer[i % AudioStandards.ChannelCount][i / AudioStandards.ChannelCount] = inBuffer[i];

                if (samplesRead == 0)
                    processingState.WriteEndOfStream();

                else
                    processingState.WriteData(outBuffer, samplesRead / AudioStandards.ChannelCount);

                while (!oggStream.Finished && processingState.PacketOut(out OggPacket packet))
                {
                    oggStream.PacketIn(packet);
                    FlushPages(oggStream, outputStream, false);
                }
            }
            while (samplesRead > 0);

            FlushPages(oggStream, outputStream, true);
        }

        private static void FlushPages(OggStream oggStream, Stream output, bool force)
        {
            while (oggStream.PageOut(out OggPage page, force))
            {
                output.Write(page.Header, 0, page.Header.Length);
                output.Write(page.Body, 0, page.Body.Length);
            }
        }
    }
}
