using NCcsds.TmTc.Frames;
using NCcsds.Viewer.Display;

namespace NCcsds.Viewer.Commands;

/// <summary>
/// AOS Frame viewer command.
/// </summary>
public static class AosFrameCommand
{
    public static int Execute(string[] args)
    {
        var options = CommandBase.ParseOptions(args);
        var data = CommandBase.ReadInputData(options);

        if (!options.Quiet)
            ConsoleDisplay.WriteSection("AOS Transfer Frame");

        try
        {
            var frame = AosFrame.Decode(data);

            // Display header fields
            ConsoleDisplay.WriteField("Transfer Frame Version", frame.VersionNumber);
            ConsoleDisplay.WriteFieldHex("Spacecraft ID", frame.SpacecraftId, 8);
            ConsoleDisplay.WriteFieldHex("Virtual Channel ID", frame.VirtualChannelId, 6);
            ConsoleDisplay.WriteFieldHex("Virtual Channel Frame Count", frame.VirtualChannelFrameCount, 24);

            // Signaling field
            Console.WriteLine();
            ConsoleDisplay.WriteInfo("Signaling Field");
            ConsoleDisplay.WriteField("  Replay Flag", frame.ReplayFlag);
            ConsoleDisplay.WriteField("  Virtual Channel Frame Count Cycle", frame.VirtualChannelFrameCountCycle);
            ConsoleDisplay.WriteFieldHex("  Virtual Channel Frame Count", frame.VirtualChannelFrameCount, 24);

            // Frame header error control (optional)
            if (frame.FrameHeaderErrorControl != null)
            {
                Console.WriteLine();
                ConsoleDisplay.WriteFieldHex("Frame Header Error Control", frame.FrameHeaderErrorControl.Value, 16);
            }

            // Insert zone (optional)
            if (frame.InsertZone != null && frame.InsertZone.Length > 0)
            {
                Console.WriteLine();
                ConsoleDisplay.WriteInfo($"Insert Zone: {frame.InsertZone.Length} bytes");
            }

            Console.WriteLine();
            ConsoleDisplay.WriteField("Data Field Length", $"{frame.DataField.Length} bytes");

            // OCF (optional)
            if (frame.Ocf != null)
            {
                Console.WriteLine();
                ConsoleDisplay.WriteInfo("OCF (Operational Control Field) present");
            }

            // FECF (optional)
            if (frame.Fecf != null)
            {
                Console.WriteLine();
                ConsoleDisplay.WriteFieldHex("FECF", frame.Fecf.Value, 16);
            }

            if (options.Verbose)
            {
                Console.WriteLine();
                ConsoleDisplay.WriteSection("Frame Data");
                var insertZoneLen = frame.InsertZone?.Length ?? 0;
                ConsoleDisplay.WriteHighlightedHexDump(data,
                    (0, 6, ConsoleColor.Cyan, "Primary Header"),
                    (6, insertZoneLen, ConsoleColor.Green, "Insert Zone"),
                    (6 + insertZoneLen, frame.DataField.Length, ConsoleColor.White, "Data Field"),
                    (6 + insertZoneLen + frame.DataField.Length, frame.Ocf != null ? 4 : 0, ConsoleColor.Yellow, "OCF"),
                    (6 + insertZoneLen + frame.DataField.Length + (frame.Ocf != null ? 4 : 0), frame.Fecf != null ? 2 : 0, ConsoleColor.Magenta, "FECF"));
            }

            return 0;
        }
        catch (Exception ex)
        {
            ConsoleDisplay.WriteError($"Failed to decode AOS frame: {ex.Message}");
            Console.WriteLine();
            ConsoleDisplay.WriteInfo("Raw data:");
            ConsoleDisplay.WriteHexDump(data);
            return 1;
        }
    }
}
