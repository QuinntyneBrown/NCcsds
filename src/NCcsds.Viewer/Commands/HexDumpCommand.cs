using NCcsds.Viewer.Display;

namespace NCcsds.Viewer.Commands;

/// <summary>
/// Hex dump command.
/// </summary>
public static class HexDumpCommand
{
    public static int Execute(string[] args)
    {
        var options = CommandBase.ParseOptions(args);

        byte[] data;
        try
        {
            data = CommandBase.ReadInputData(options);
        }
        catch (Exception ex)
        {
            ConsoleDisplay.WriteError(ex.Message);
            return 1;
        }

        if (!options.Quiet)
        {
            ConsoleDisplay.WriteSection("Hex Dump");
            if (!string.IsNullOrEmpty(options.FilePath))
                ConsoleDisplay.WriteField("File", options.FilePath);
            ConsoleDisplay.WriteField("Size", $"{data.Length} bytes");
            Console.WriteLine();
        }

        ConsoleDisplay.WriteHexDump(data, startOffset: options.Offset);

        return 0;
    }
}
