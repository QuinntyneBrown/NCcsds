using NCcsds.Core.Processing;

namespace NCcsds.TmTc.Processing;

/// <summary>
/// Applies CCSDS pseudo-randomization to frames.
/// </summary>
public static class FrameRandomizer
{
    /// <summary>
    /// Attached Sync Marker for TM frames.
    /// </summary>
    public static readonly byte[] TmAsm = { 0x1A, 0xCF, 0xFC, 0x1D };

    /// <summary>
    /// Applies randomization to a frame in-place.
    /// Randomization starts after the ASM (if present) or from the beginning.
    /// </summary>
    /// <param name="frame">The frame data to randomize.</param>
    /// <param name="skipBytes">Number of bytes to skip (e.g., ASM length).</param>
    public static void Randomize(Span<byte> frame, int skipBytes = 0)
    {
        if (skipBytes >= frame.Length)
            return;

        PseudoRandomSequence.Apply(frame[skipBytes..]);
    }

    /// <summary>
    /// Applies derandomization to a frame in-place.
    /// Same operation as randomization (XOR is symmetric).
    /// </summary>
    /// <param name="frame">The frame data to derandomize.</param>
    /// <param name="skipBytes">Number of bytes to skip.</param>
    public static void Derandomize(Span<byte> frame, int skipBytes = 0)
    {
        Randomize(frame, skipBytes);
    }

    /// <summary>
    /// Applies randomization to a frame, writing to a new buffer.
    /// </summary>
    /// <param name="input">The input frame.</param>
    /// <param name="output">The output buffer.</param>
    /// <param name="skipBytes">Number of bytes to skip.</param>
    public static void Randomize(ReadOnlySpan<byte> input, Span<byte> output, int skipBytes = 0)
    {
        if (output.Length < input.Length)
            throw new ArgumentException("Output buffer too small.", nameof(output));

        // Copy header bytes as-is
        input[..skipBytes].CopyTo(output);

        // Randomize the rest
        PseudoRandomSequence.Apply(input[skipBytes..], output[skipBytes..]);
    }

    /// <summary>
    /// Checks if data starts with the TM ASM.
    /// </summary>
    public static bool StartsWithTmAsm(ReadOnlySpan<byte> data)
    {
        if (data.Length < TmAsm.Length)
            return false;

        return data[..TmAsm.Length].SequenceEqual(TmAsm);
    }

    /// <summary>
    /// Finds the next TM ASM in the data.
    /// </summary>
    /// <param name="data">The data to search.</param>
    /// <param name="startIndex">Starting index.</param>
    /// <returns>Index of ASM, or -1 if not found.</returns>
    public static int FindTmAsm(ReadOnlySpan<byte> data, int startIndex = 0)
    {
        for (int i = startIndex; i <= data.Length - TmAsm.Length; i++)
        {
            if (data.Slice(i, TmAsm.Length).SequenceEqual(TmAsm))
                return i;
        }
        return -1;
    }
}
