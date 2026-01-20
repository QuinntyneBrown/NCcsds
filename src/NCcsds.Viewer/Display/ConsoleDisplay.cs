using System.Text;

namespace NCcsds.Viewer.Display;

/// <summary>
/// Console display utilities with color support.
/// </summary>
public static class ConsoleDisplay
{
    private static bool _colorEnabled = !Console.IsOutputRedirected;

    /// <summary>
    /// Enable or disable color output.
    /// </summary>
    public static bool ColorEnabled
    {
        get => _colorEnabled;
        set => _colorEnabled = value;
    }

    /// <summary>
    /// Writes a hex dump of the data.
    /// </summary>
    public static void WriteHexDump(ReadOnlySpan<byte> data, int bytesPerLine = 16, long startOffset = 0)
    {
        var sb = new StringBuilder();

        for (int i = 0; i < data.Length; i += bytesPerLine)
        {
            // Offset
            WriteColored($"{startOffset + i:X8}  ", ConsoleColor.DarkGray);

            // Hex bytes
            var lineLength = Math.Min(bytesPerLine, data.Length - i);
            for (int j = 0; j < bytesPerLine; j++)
            {
                if (j < lineLength)
                {
                    var b = data[i + j];
                    var color = GetByteColor(b);
                    WriteColored($"{b:X2} ", color);
                }
                else
                {
                    Console.Write("   ");
                }

                if (j == 7)
                    Console.Write(" ");
            }

            Console.Write(" ");

            // ASCII
            WriteColored("|", ConsoleColor.DarkGray);
            for (int j = 0; j < lineLength; j++)
            {
                var b = data[i + j];
                var c = b >= 32 && b < 127 ? (char)b : '.';
                WriteColored(c.ToString(), b >= 32 && b < 127 ? ConsoleColor.White : ConsoleColor.DarkGray);
            }
            for (int j = lineLength; j < bytesPerLine; j++)
                Console.Write(" ");
            WriteColored("|", ConsoleColor.DarkGray);

            Console.WriteLine();
        }
    }

    /// <summary>
    /// Writes a binary dump of the data.
    /// </summary>
    public static void WriteBinaryDump(ReadOnlySpan<byte> data, int bytesPerLine = 4, long startOffset = 0)
    {
        for (int i = 0; i < data.Length; i += bytesPerLine)
        {
            // Offset
            WriteColored($"{startOffset + i:X8}  ", ConsoleColor.DarkGray);

            var lineLength = Math.Min(bytesPerLine, data.Length - i);
            for (int j = 0; j < lineLength; j++)
            {
                var b = data[i + j];
                for (int bit = 7; bit >= 0; bit--)
                {
                    var bitValue = (b >> bit) & 1;
                    WriteColored(bitValue.ToString(), bitValue == 1 ? ConsoleColor.Yellow : ConsoleColor.DarkGray);
                }
                Console.Write(" ");
            }

            Console.WriteLine();
        }
    }

    /// <summary>
    /// Writes a field with label and value.
    /// </summary>
    public static void WriteField(string label, object value, int labelWidth = 30)
    {
        WriteColored(label.PadRight(labelWidth), ConsoleColor.Cyan);
        Console.Write(": ");
        WriteColored(value.ToString() ?? "", ConsoleColor.White);
        Console.WriteLine();
    }

    /// <summary>
    /// Writes a field with label, value, and hex representation.
    /// </summary>
    public static void WriteFieldHex(string label, long value, int bits, int labelWidth = 30)
    {
        WriteColored(label.PadRight(labelWidth), ConsoleColor.Cyan);
        Console.Write(": ");
        WriteColored(value.ToString(), ConsoleColor.White);
        WriteColored($" (0x{value:X})", ConsoleColor.DarkGray);
        Console.WriteLine();
    }

    /// <summary>
    /// Writes a section header.
    /// </summary>
    public static void WriteSection(string title)
    {
        Console.WriteLine();
        WriteColored($"═══ {title} ", ConsoleColor.Green);
        WriteColored(new string('═', Math.Max(0, 60 - title.Length - 5)), ConsoleColor.DarkGreen);
        Console.WriteLine();
        Console.WriteLine();
    }

    /// <summary>
    /// Writes an info message.
    /// </summary>
    public static void WriteInfo(string message)
    {
        WriteColored("[INFO] ", ConsoleColor.Blue);
        Console.WriteLine(message);
    }

    /// <summary>
    /// Writes a warning message.
    /// </summary>
    public static void WriteWarning(string message)
    {
        WriteColored("[WARN] ", ConsoleColor.Yellow);
        Console.WriteLine(message);
    }

    /// <summary>
    /// Writes an error message.
    /// </summary>
    public static void WriteError(string message)
    {
        WriteColored("[ERROR] ", ConsoleColor.Red);
        Console.WriteLine(message);
    }

    /// <summary>
    /// Writes success message.
    /// </summary>
    public static void WriteSuccess(string message)
    {
        WriteColored("[OK] ", ConsoleColor.Green);
        Console.WriteLine(message);
    }

    /// <summary>
    /// Writes a highlighted hex dump with specific regions highlighted.
    /// </summary>
    public static void WriteHighlightedHexDump(ReadOnlySpan<byte> data, params (int start, int length, ConsoleColor color, string label)[] highlights)
    {
        var highlightMap = new Dictionary<int, (ConsoleColor color, string label)>();
        foreach (var (start, length, color, label) in highlights)
        {
            for (int i = start; i < start + length && i < data.Length; i++)
                highlightMap[i] = (color, label);
        }

        const int bytesPerLine = 16;
        for (int i = 0; i < data.Length; i += bytesPerLine)
        {
            WriteColored($"{i:X8}  ", ConsoleColor.DarkGray);

            var lineLength = Math.Min(bytesPerLine, data.Length - i);
            for (int j = 0; j < bytesPerLine; j++)
            {
                if (j < lineLength)
                {
                    var idx = i + j;
                    var b = data[idx];
                    var color = highlightMap.TryGetValue(idx, out var hl) ? hl.color : ConsoleColor.Gray;
                    WriteColored($"{b:X2} ", color);
                }
                else
                {
                    Console.Write("   ");
                }

                if (j == 7)
                    Console.Write(" ");
            }

            Console.WriteLine();
        }

        // Print legend
        if (highlights.Length > 0)
        {
            Console.WriteLine();
            WriteColored("Legend: ", ConsoleColor.DarkGray);
            foreach (var (_, _, color, label) in highlights.DistinctBy(h => h.label))
            {
                WriteColored($"■ {label}  ", color);
            }
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Writes colored text.
    /// </summary>
    public static void WriteColored(string text, ConsoleColor color)
    {
        if (_colorEnabled)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = prev;
        }
        else
        {
            Console.Write(text);
        }
    }

    private static ConsoleColor GetByteColor(byte b)
    {
        return b switch
        {
            0x00 => ConsoleColor.DarkGray,
            0xFF => ConsoleColor.DarkRed,
            >= 0x20 and < 0x7F => ConsoleColor.White, // Printable ASCII
            _ => ConsoleColor.Gray
        };
    }
}
