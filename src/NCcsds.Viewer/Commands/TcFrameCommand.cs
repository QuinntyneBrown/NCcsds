using NCcsds.TmTc.Frames;
using NCcsds.Viewer.Display;

namespace NCcsds.Viewer.Commands;

/// <summary>
/// TC Frame viewer command.
/// </summary>
public static class TcFrameCommand
{
    public static int Execute(string[] args)
    {
        var options = CommandBase.ParseOptions(args);
        var data = CommandBase.ReadInputData(options);

        if (!options.Quiet)
            ConsoleDisplay.WriteSection("TC Transfer Frame");

        try
        {
            var frame = TcFrame.Decode(data);

            // Display header fields
            ConsoleDisplay.WriteField("Transfer Frame Version", frame.VersionNumber);
            ConsoleDisplay.WriteField("Bypass Flag", frame.BypassFlag);
            ConsoleDisplay.WriteField("Control Command Flag", frame.ControlCommandFlag);
            ConsoleDisplay.WriteFieldHex("Spacecraft ID", frame.SpacecraftId, 10);
            ConsoleDisplay.WriteFieldHex("Virtual Channel ID", frame.VirtualChannelId, 6);
            ConsoleDisplay.WriteFieldHex("Frame Length", frame.FrameLength, 10);
            ConsoleDisplay.WriteFieldHex("Frame Sequence Number", frame.FrameSequenceNumber, 8);

            Console.WriteLine();
            ConsoleDisplay.WriteField("Data Field Length", $"{frame.DataField.Length} bytes");

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
                    (0, 5, ConsoleColor.Cyan, "Primary Header"),
                    (5, frame.DataField.Length, ConsoleColor.White, "Data Field"),
                    (5 + frame.DataField.Length, frame.Fecf != null ? 2 : 0, ConsoleColor.Magenta, "FECF"));
            }

            return 0;
        }
        catch (Exception ex)
        {
            ConsoleDisplay.WriteError($"Failed to decode TC frame: {ex.Message}");
            Console.WriteLine();
            ConsoleDisplay.WriteInfo("Raw data:");
            ConsoleDisplay.WriteHexDump(data);
            return 1;
        }
    }
}
