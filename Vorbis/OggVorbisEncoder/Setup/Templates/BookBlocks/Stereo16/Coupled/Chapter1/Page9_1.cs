namespace OggVorbisEncoder.Setup.Templates.BookBlocks.Stereo16.Coupled.Chapter1;

public class Page9_1 : IStaticCodeBook
{
    public int Dimensions { get; } = 2;

    public byte[] LengthList { get; } = {
         1, 4, 4, 4, 4, 8, 8,12,13,14,14,14,14,14,14, 6,
         6, 6, 6, 6,10, 9,14,14,14,14,14,14,14,14, 7, 6,
         5, 6, 6,10, 9,12,13,13,13,13,13,13,13,13, 7, 7,
         9, 9,11,11,12,13,13,13,13,13,13,13,13, 7, 7, 8,
         8,11,12,13,13,13,13,13,13,13,13,13,12,12,10,10,
        13,12,13,13,13,13,13,13,13,13,13,12,12,10,10,13,
        13,13,13,13,13,13,13,13,13,13,13,13,13,12,13,12,
        13,13,13,13,13,13,13,13,13,13,13,13,12,13,13,13,
        13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,
        13,13,13,13,13,13,13,13,13,13,13,13,12,13,13,13,
        13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,
        13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,
        13,13,13,13,13,13,13,13,13,12,13,13,13,13,13,13,
        13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,
        13,
    };

    public CodeBookMapType MapType { get; } = (CodeBookMapType)1;
    public int QuantMin { get; } = -520986624;
    public int QuantDelta { get; } = 1620377600;
    public int Quant { get; } = 4;
    public int QuantSequenceP { get; } = 0;

    public int[] QuantList { get; } = {
        7,
        6,
        8,
        5,
        9,
        4,
        10,
        3,
        11,
        2,
        12,
        1,
        13,
        0,
        14,
    };
}