using NCcsds.Viewer.Commands;
using NCcsds.Viewer.Display;

namespace NCcsds.Viewer;

/// <summary>
/// NCcsds.Viewer - CCSDS data visualization tool.
/// </summary>
public class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelp();
            return 0;
        }

        var command = args[0].ToLowerInvariant();
        var remainingArgs = args.Skip(1).ToArray();

        try
        {
            return command switch
            {
                "hex" => HexDumpCommand.Execute(remainingArgs),
                "tm" => TmFrameCommand.Execute(remainingArgs),
                "tc" => TcFrameCommand.Execute(remainingArgs),
                "aos" => AosFrameCommand.Execute(remainingArgs),
                "packet" => SpacePacketCommand.Execute(remainingArgs),
                "pus" => PusPacketCommand.Execute(remainingArgs),
                "cfdp" => CfdpPduCommand.Execute(remainingArgs),
                "sle" => SlePduCommand.Execute(remainingArgs),
                "export" => ExportCommand.Execute(remainingArgs),
                "help" or "--help" or "-h" => PrintHelp(),
                "version" or "--version" or "-v" => PrintVersion(),
                _ => UnknownCommand(command)
            };
        }
        catch (Exception ex)
        {
            ConsoleDisplay.WriteError($"Error: {ex.Message}");
            return 1;
        }
    }

    private static int PrintHelp()
    {
        Console.WriteLine("""
            NCcsds.Viewer - CCSDS Data Visualization Tool

            Usage: nccsds-viewer <command> [options] [file]

            Commands:
              hex       Display raw hex dump of file or stdin
              tm        Parse and display TM Transfer Frame
              tc        Parse and display TC Transfer Frame
              aos       Parse and display AOS Transfer Frame
              packet    Parse and display CCSDS Space Packet
              pus       Parse and display PUS Packet
              cfdp      Parse and display CFDP PDU
              sle       Parse and display SLE PDU
              export    Export data in various formats
              help      Show this help message
              version   Show version information

            Options:
              -f, --file <path>    Input file path
              -o, --offset <n>     Start offset in bytes
              -l, --length <n>     Number of bytes to read
              -c, --color          Enable color output (default: auto)
              --no-color           Disable color output
              -v, --verbose        Verbose output
              -q, --quiet          Quiet output

            Examples:
              nccsds-viewer hex data.bin
              nccsds-viewer tm --file frame.bin
              nccsds-viewer packet -f packets.bin -v
              cat data.bin | nccsds-viewer hex
            """);
        return 0;
    }

    private static int PrintVersion()
    {
        Console.WriteLine("NCcsds.Viewer v1.0.0");
        Console.WriteLine("CCSDS Data Visualization Tool");
        return 0;
    }

    private static int UnknownCommand(string command)
    {
        ConsoleDisplay.WriteError($"Unknown command: {command}");
        Console.WriteLine("Use 'nccsds-viewer help' for usage information.");
        return 1;
    }
}
