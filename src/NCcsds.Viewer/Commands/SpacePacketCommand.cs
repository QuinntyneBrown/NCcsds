using NCcsds.Encoding.Packets;
using NCcsds.Viewer.Display;

namespace NCcsds.Viewer.Commands;

/// <summary>
/// Space Packet viewer command.
/// </summary>
public static class SpacePacketCommand
{
    public static int Execute(string[] args)
    {
        var options = CommandBase.ParseOptions(args);
        var data = CommandBase.ReadInputData(options);

        if (!options.Quiet)
            ConsoleDisplay.WriteSection("CCSDS Space Packet");

        try
        {
            var packet = SpacePacket.Decode(data);

            // Display header fields
            ConsoleDisplay.WriteField("Packet Version Number", packet.VersionNumber);
            ConsoleDisplay.WriteField("Packet Type", packet.Type == PacketType.Telemetry ? "Telemetry (TM)" : "Telecommand (TC)");
            ConsoleDisplay.WriteField("Secondary Header Flag", packet.SecondaryHeaderFlag);
            ConsoleDisplay.WriteFieldHex("Application Process ID (APID)", packet.Apid, 11);
            ConsoleDisplay.WriteField("Sequence Flags", FormatSequenceFlags(packet.SequenceFlags));
            ConsoleDisplay.WriteFieldHex("Packet Sequence Count", packet.SequenceCount, 14);
            ConsoleDisplay.WriteFieldHex("Packet Data Length", packet.DataLength, 16);

            Console.WriteLine();
            ConsoleDisplay.WriteField("User Data Length", $"{packet.UserData.Length} bytes");

            if (options.Verbose)
            {
                Console.WriteLine();
                ConsoleDisplay.WriteSection("Packet Data");
                ConsoleDisplay.WriteHighlightedHexDump(data,
                    (0, 6, ConsoleColor.Cyan, "Primary Header"),
                    (6, packet.UserData.Length, ConsoleColor.White, "User Data"));

                Console.WriteLine();
                ConsoleDisplay.WriteSection("User Data (Decoded)");
                ConsoleDisplay.WriteHexDump(packet.UserData);
            }

            return 0;
        }
        catch (Exception ex)
        {
            ConsoleDisplay.WriteError($"Failed to decode Space Packet: {ex.Message}");
            Console.WriteLine();
            ConsoleDisplay.WriteInfo("Raw data:");
            ConsoleDisplay.WriteHexDump(data);
            return 1;
        }
    }

    private static string FormatSequenceFlags(SequenceFlags flags)
    {
        return flags switch
        {
            SequenceFlags.Continuation => "Continuation segment",
            SequenceFlags.FirstSegment => "First segment",
            SequenceFlags.LastSegment => "Last segment",
            SequenceFlags.Unsegmented => "Unsegmented",
            _ => $"Unknown ({(int)flags})"
        };
    }
}
