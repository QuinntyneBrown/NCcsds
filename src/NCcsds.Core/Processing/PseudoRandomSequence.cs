namespace NCcsds.Core.Processing;

/// <summary>
/// CCSDS pseudo-random sequence generator for frame randomization/derandomization.
/// Uses the polynomial h(x) = x^8 + x^7 + x^5 + x^3 + 1.
/// </summary>
public static class PseudoRandomSequence
{
    /// <summary>
    /// Length of one complete period of the pseudo-random sequence.
    /// </summary>
    public const int SequenceLength = 255;

    /// <summary>
    /// Pre-computed pseudo-random sequence (255 bytes).
    /// </summary>
    private static readonly byte[] Sequence = GenerateSequence();

    private static byte[] GenerateSequence()
    {
        var sequence = new byte[SequenceLength];
        byte register = 0xFF; // Initial all-ones state

        for (int i = 0; i < SequenceLength; i++)
        {
            sequence[i] = register;

            // Shift register with feedback polynomial x^8 + x^7 + x^5 + x^3 + 1
            byte feedback = (byte)(
                ((register >> 0) & 1) ^
                ((register >> 2) & 1) ^
                ((register >> 4) & 1) ^
                ((register >> 6) & 1)
            );
            register = (byte)((register >> 1) | (feedback << 7));
        }

        return sequence;
    }

    /// <summary>
    /// Gets the pseudo-random sequence byte at the specified index.
    /// </summary>
    /// <param name="index">The index (wraps around at 255).</param>
    /// <returns>The sequence byte.</returns>
    public static byte GetByte(int index) => Sequence[index % SequenceLength];

    /// <summary>
    /// Applies randomization/derandomization to the data in-place.
    /// The operation is symmetric (XOR).
    /// </summary>
    /// <param name="data">The data to randomize/derandomize.</param>
    /// <param name="startIndex">Starting index in the pseudo-random sequence.</param>
    public static void Apply(Span<byte> data, int startIndex = 0)
    {
        int seqIndex = startIndex % SequenceLength;
        for (int i = 0; i < data.Length; i++)
        {
            data[i] ^= Sequence[seqIndex];
            seqIndex = (seqIndex + 1) % SequenceLength;
        }
    }

    /// <summary>
    /// Applies randomization/derandomization to the data, writing to an output buffer.
    /// </summary>
    /// <param name="input">The input data.</param>
    /// <param name="output">The output buffer.</param>
    /// <param name="startIndex">Starting index in the pseudo-random sequence.</param>
    public static void Apply(ReadOnlySpan<byte> input, Span<byte> output, int startIndex = 0)
    {
        if (output.Length < input.Length)
            throw new ArgumentException("Output buffer is too small.", nameof(output));

        int seqIndex = startIndex % SequenceLength;
        for (int i = 0; i < input.Length; i++)
        {
            output[i] = (byte)(input[i] ^ Sequence[seqIndex]);
            seqIndex = (seqIndex + 1) % SequenceLength;
        }
    }

    /// <summary>
    /// Gets a span of the pseudo-random sequence.
    /// </summary>
    public static ReadOnlySpan<byte> GetSequence() => Sequence;

    /// <summary>
    /// Fills a buffer with pseudo-random bytes starting from the specified index.
    /// </summary>
    /// <param name="buffer">The buffer to fill.</param>
    /// <param name="startIndex">Starting index in the pseudo-random sequence.</param>
    public static void Fill(Span<byte> buffer, int startIndex = 0)
    {
        int seqIndex = startIndex % SequenceLength;
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = Sequence[seqIndex];
            seqIndex = (seqIndex + 1) % SequenceLength;
        }
    }
}
