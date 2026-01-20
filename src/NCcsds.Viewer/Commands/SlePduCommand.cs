using NCcsds.Sle.Asn1;
using NCcsds.Viewer.Display;

namespace NCcsds.Viewer.Commands;

/// <summary>
/// SLE PDU viewer command.
/// </summary>
public static class SlePduCommand
{
    public static int Execute(string[] args)
    {
        var options = CommandBase.ParseOptions(args);
        var data = CommandBase.ReadInputData(options);

        if (!options.Quiet)
            ConsoleDisplay.WriteSection("SLE PDU");

        try
        {
            // Try to decode as BER
            DecodeAsn1Structure(data, 0);

            if (options.Verbose)
            {
                Console.WriteLine();
                ConsoleDisplay.WriteSection("Raw PDU Data");
                ConsoleDisplay.WriteHexDump(data);
            }

            return 0;
        }
        catch (Exception ex)
        {
            ConsoleDisplay.WriteError($"Failed to decode SLE PDU: {ex.Message}");
            Console.WriteLine();
            ConsoleDisplay.WriteInfo("Raw data:");
            ConsoleDisplay.WriteHexDump(data);
            return 1;
        }
    }

    private static void DecodeAsn1Structure(ReadOnlySpan<byte> data, int indent)
    {
        int offset = 0;

        while (offset < data.Length)
        {
            var tag = data[offset];
            var isConstructed = (tag & 0x20) != 0;
            var tagClass = (tag >> 6) & 0x03;
            var tagNumber = tag & 0x1F;

            var length = BerDecoder.DecodeLength(data[(offset + 1)..], out var lengthBytes);
            var valueStart = offset + 1 + lengthBytes;

            if (valueStart + length > data.Length)
            {
                ConsoleDisplay.WriteWarning($"Truncated data at offset {offset}");
                break;
            }

            var prefix = new string(' ', indent * 2);
            var tagClassName = tagClass switch
            {
                0 => "UNIVERSAL",
                1 => "APPLICATION",
                2 => "CONTEXT",
                3 => "PRIVATE",
                _ => "UNKNOWN"
            };

            var tagTypeName = GetUniversalTagName(tag);

            ConsoleDisplay.WriteColored($"{prefix}[{offset:X4}] ", ConsoleColor.DarkGray);
            ConsoleDisplay.WriteColored($"Tag: 0x{tag:X2} ", ConsoleColor.Cyan);
            ConsoleDisplay.WriteColored($"({tagClassName}[{tagNumber}] {tagTypeName}) ", ConsoleColor.Gray);
            ConsoleDisplay.WriteColored($"Length: {length}", ConsoleColor.Yellow);
            Console.WriteLine();

            var valueData = data.Slice(valueStart, length);

            if (isConstructed && length > 0)
            {
                // Recursively decode constructed types
                DecodeAsn1Structure(valueData, indent + 1);
            }
            else if (length > 0)
            {
                // Display primitive value
                DisplayValue(tag, valueData, indent);
            }

            offset = valueStart + length;
        }
    }

    private static void DisplayValue(byte tag, ReadOnlySpan<byte> data, int indent)
    {
        var prefix = new string(' ', (indent + 1) * 2);
        var universalTag = tag & 0x1F;

        // Only interpret UNIVERSAL tags
        if ((tag & 0xC0) == 0)
        {
            switch (universalTag)
            {
                case 0x01: // BOOLEAN
                    ConsoleDisplay.WriteColored($"{prefix}Value: ", ConsoleColor.DarkGray);
                    ConsoleDisplay.WriteColored(data[0] != 0 ? "TRUE" : "FALSE", ConsoleColor.Green);
                    Console.WriteLine();
                    return;

                case 0x02: // INTEGER
                    long intValue = 0;
                    var isNegative = (data[0] & 0x80) != 0;
                    foreach (var b in data)
                        intValue = (intValue << 8) | b;
                    if (isNegative && data.Length < 8)
                    {
                        for (int i = data.Length; i < 8; i++)
                            intValue |= (long)0xFF << (i * 8);
                    }
                    ConsoleDisplay.WriteColored($"{prefix}Value: ", ConsoleColor.DarkGray);
                    ConsoleDisplay.WriteColored($"{intValue} (0x{intValue:X})", ConsoleColor.Green);
                    Console.WriteLine();
                    return;

                case 0x04: // OCTET STRING
                case 0x03: // BIT STRING
                    ConsoleDisplay.WriteColored($"{prefix}Value: ", ConsoleColor.DarkGray);
                    if (data.Length <= 16)
                    {
                        ConsoleDisplay.WriteColored(BitConverter.ToString(data.ToArray()).Replace("-", " "), ConsoleColor.Green);
                    }
                    else
                    {
                        ConsoleDisplay.WriteColored($"[{data.Length} bytes]", ConsoleColor.Green);
                    }
                    Console.WriteLine();
                    return;

                case 0x05: // NULL
                    ConsoleDisplay.WriteColored($"{prefix}Value: ", ConsoleColor.DarkGray);
                    ConsoleDisplay.WriteColored("NULL", ConsoleColor.Green);
                    Console.WriteLine();
                    return;

                case 0x06: // OBJECT IDENTIFIER
                    ConsoleDisplay.WriteColored($"{prefix}Value: ", ConsoleColor.DarkGray);
                    ConsoleDisplay.WriteColored(DecodeOid(data), ConsoleColor.Green);
                    Console.WriteLine();
                    return;

                case 0x0C: // UTF8String
                case 0x13: // PrintableString
                case 0x16: // IA5String
                case 0x1A: // VisibleString
                    var str = System.Text.Encoding.ASCII.GetString(data);
                    ConsoleDisplay.WriteColored($"{prefix}Value: ", ConsoleColor.DarkGray);
                    ConsoleDisplay.WriteColored($"\"{str}\"", ConsoleColor.Green);
                    Console.WriteLine();
                    return;
            }
        }

        // Default: show hex
        ConsoleDisplay.WriteColored($"{prefix}Value: ", ConsoleColor.DarkGray);
        if (data.Length <= 16)
        {
            ConsoleDisplay.WriteColored(BitConverter.ToString(data.ToArray()).Replace("-", " "), ConsoleColor.White);
        }
        else
        {
            ConsoleDisplay.WriteColored($"[{data.Length} bytes]", ConsoleColor.White);
        }
        Console.WriteLine();
    }

    private static string GetUniversalTagName(byte tag)
    {
        if ((tag & 0xC0) != 0)
            return tag switch
            {
                _ when (tag & 0x20) != 0 => "constructed",
                _ => "primitive"
            };

        return (tag & 0x1F) switch
        {
            0x01 => "BOOLEAN",
            0x02 => "INTEGER",
            0x03 => "BIT STRING",
            0x04 => "OCTET STRING",
            0x05 => "NULL",
            0x06 => "OBJECT IDENTIFIER",
            0x0C => "UTF8String",
            0x10 => "SEQUENCE",
            0x11 => "SET",
            0x13 => "PrintableString",
            0x16 => "IA5String",
            0x17 => "UTCTime",
            0x18 => "GeneralizedTime",
            0x1A => "VisibleString",
            _ => "UNKNOWN"
        };
    }

    private static string DecodeOid(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0)
            return "";

        var parts = new List<long>();

        // First byte encodes first two components
        parts.Add(data[0] / 40);
        parts.Add(data[0] % 40);

        long value = 0;
        for (int i = 1; i < data.Length; i++)
        {
            value = (value << 7) | (data[i] & 0x7F);
            if ((data[i] & 0x80) == 0)
            {
                parts.Add(value);
                value = 0;
            }
        }

        return string.Join(".", parts);
    }
}
