using NCcsds.TmTc.Frames;
using NCcsds.Viewer.Display;

namespace NCcsds.Viewer.Commands;

/// <summary>
/// TM Frame viewer command.
/// </summary>
public static class TmFrameCommand
{
    public static int Execute(string[] args)
    {
        var options = CommandBase.ParseOptions(args);
        var data = CommandBase.ReadInputData(options);

        if (!options.Quiet)
            ConsoleDisplay.WriteSection("TM Transfer Frame");

        try
        {
            var frame = TmFrame.Decode(data);

            // Display header fields
            ConsoleDisplay.WriteField("Transfer Frame Version", frame.VersionNumber);
            ConsoleDisplay.WriteFieldHex("Spacecraft ID", frame.SpacecraftId, 10);
            ConsoleDisplay.WriteFieldHex("Virtual Channel ID", frame.VirtualChannelId, 3);
            ConsoleDisplay.WriteField("OCF Flag", frame.OcfFlag);
            ConsoleDisplay.WriteFieldHex("Master Channel Frame Count", frame.MasterChannelFrameCount, 8);
            ConsoleDisplay.WriteFieldHex("Virtual Channel Frame Count", frame.VirtualChannelFrameCount, 8);
            ConsoleDisplay.WriteField("Secondary Header Flag", frame.SecondaryHeaderFlag);
            ConsoleDisplay.WriteField("Sync Flag", frame.SyncFlag);
            ConsoleDisplay.WriteField("Packet Order Flag", frame.PacketOrderFlag);
            ConsoleDisplay.WriteFieldHex("Segment Length ID", frame.SegmentLengthId, 2);
            ConsoleDisplay.WriteFieldHex("First Header Pointer", frame.FirstHeaderPointer, 11);

            Console.WriteLine();
            ConsoleDisplay.WriteField("Data Field Length", $"{frame.DataField.Length} bytes");

            if (frame.OcfFlag && frame.Ocf != null)
            {
                Console.WriteLine();
                ConsoleDisplay.WriteInfo("OCF (Operational Control Field) present");
                ConsoleDisplay.WriteFieldHex("OCF", BitConverter.ToUInt32(frame.Ocf.Reverse().ToArray()), 32);
            }

            if (frame.Fecf != null)
            {
                Console.WriteLine();
                ConsoleDisplay.WriteInfo("FECF (Frame Error Control Field) present");
                ConsoleDisplay.WriteFieldHex("FECF", frame.Fecf.Value, 16);
            }

            if (options.Verbose)
            {
                Console.WriteLine();
                ConsoleDisplay.WriteSection("Frame Data");
                ConsoleDisplay.WriteHighlightedHexDump(data,
                    (0, 6, ConsoleColor.Cyan, "Primary Header"),
                    (6, frame.DataField.Length, ConsoleColor.White, "Data Field"),
                    (6 + frame.DataField.Length, frame.OcfFlag ? 4 : 0, ConsoleColor.Yellow, "OCF"),
                    (6 + frame.DataField.Length + (frame.OcfFlag ? 4 : 0), frame.Fecf != null ? 2 : 0, ConsoleColor.Magenta, "FECF"));
            }

            return 0;
        }
        catch (Exception ex)
        {
            ConsoleDisplay.WriteError($"Failed to decode TM frame: {ex.Message}");
            Console.WriteLine();
            ConsoleDisplay.WriteInfo("Raw data:");
            ConsoleDisplay.WriteHexDump(data);
            return 1;
        }
    }
}
