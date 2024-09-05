using NAudio.Wave;
using System;

public class CircularSampleBuffer
{
    private readonly float[] buffer;

    private readonly object lockObject;

    private int writePosition;

    private int readPosition;

    private int sampleCount;

    //
    // Summary:
    //     Maximum length of this circular buffer
    public int MaxLength => buffer.Length;

    //
    // Summary:
    //     Number of samples currently stored in the circular buffer
    public int Count
    {
        get
        {
            lock (lockObject)
            {
                return sampleCount;
            }
        }
    }

    //
    // Summary:
    //     Create a new circular buffer
    //
    // Parameters:
    //   size:
    //     Max buffer size in bytes
    public CircularSampleBuffer(int size)
    {
        buffer = new float[size];
        lockObject = new object();
    }

    //
    // Summary:
    //     Write data to the buffer
    //
    // Parameters:
    //   data:
    //     Data to write
    //
    //   offset:
    //     Offset into data
    //
    //   count:
    //     Number of bytes to write
    //
    // Returns:
    //     number of bytes written
    public int Write(float[] data, int offset, int count)
    {
        lock (lockObject)
        {
            int num = 0;
            if (count > buffer.Length - sampleCount)
            {
                count = buffer.Length - sampleCount;
            }

            int num2 = Math.Min(buffer.Length - writePosition, count);
            Array.Copy(data, offset, buffer, writePosition, num2);
            writePosition += num2;
            writePosition %= buffer.Length;
            num += num2;
            if (num < count)
            {
                Array.Copy(data, offset + num, buffer, writePosition, count - num);
                writePosition += count - num;
                num = count;
            }

            sampleCount += num;
            return num;
        }
    }

    //
    // Summary:
    //     Read from the buffer
    //
    // Parameters:
    //   data:
    //     Buffer to read into
    //
    //   offset:
    //     Offset into read buffer
    //
    //   count:
    //     Bytes to read
    //
    // Returns:
    //     Number of bytes actually read
    public int Read(float[] data, int offset, int count)
    {
        lock (lockObject)
        {
            if (count > sampleCount)
            {
                count = sampleCount;
            }

            int num = 0;
            int num2 = Math.Min(buffer.Length - readPosition, count);
            Array.Copy(buffer, readPosition, data, offset, num2);
            num += num2;
            readPosition += num2;
            readPosition %= buffer.Length;
            if (num < count)
            {
                Array.Copy(buffer, readPosition, data, offset + num, count - num);
                readPosition += count - num;
                num = count;
            }

            sampleCount -= num;
            return num;
        }
    }

    //
    // Summary:
    //     Resets the buffer
    public void Reset()
    {
        lock (lockObject)
        {
            ResetInner();
        }
    }

    private void ResetInner()
    {
        sampleCount = 0;
        readPosition = 0;
        writePosition = 0;
    }

    //
    // Summary:
    //     Advances the buffer, discarding bytes
    //
    // Parameters:
    //   count:
    //     Bytes to advance
    public void Advance(int count)
    {
        lock (lockObject)
        {
            if (count >= sampleCount)
            {
                ResetInner();
                return;
            }

            sampleCount -= count;
            readPosition += count;
            readPosition %= MaxLength;
        }
    }
}