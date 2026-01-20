using NCcsds.Viewer.Display;

namespace NCcsds.Viewer.Commands;

/// <summary>
/// Base class for commands with common option parsing.
/// </summary>
public abstract class CommandBase
{
    protected static CommandOptions ParseOptions(string[] args)
    {
        var options = new CommandOptions();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-f":
                case "--file":
                    if (i + 1 < args.Length)
                        options.FilePath = args[++i];
                    break;

                case "-o":
                case "--offset":
                    if (i + 1 < args.Length)
                        options.Offset = ParseNumber(args[++i]);
                    break;

                case "-l":
                case "--length":
                    if (i + 1 < args.Length)
                        options.Length = (int)ParseNumber(args[++i]);
                    break;

                case "-c":
                case "--color":
                    options.ColorEnabled = true;
                    break;

                case "--no-color":
                    options.ColorEnabled = false;
                    break;

                case "-v":
                case "--verbose":
                    options.Verbose = true;
                    break;

                case "-q":
                case "--quiet":
                    options.Quiet = true;
                    break;

                case "--format":
                    if (i + 1 < args.Length)
                        options.Format = args[++i];
                    break;

                case "--output":
                    if (i + 1 < args.Length)
                        options.OutputPath = args[++i];
                    break;

                default:
                    if (!args[i].StartsWith("-") && string.IsNullOrEmpty(options.FilePath))
                        options.FilePath = args[i];
                    break;
            }
        }

        if (options.ColorEnabled.HasValue)
            ConsoleDisplay.ColorEnabled = options.ColorEnabled.Value;

        return options;
    }

    protected static byte[] ReadInputData(CommandOptions options)
    {
        byte[] data;

        if (!string.IsNullOrEmpty(options.FilePath))
        {
            if (!File.Exists(options.FilePath))
                throw new FileNotFoundException($"File not found: {options.FilePath}");

            data = File.ReadAllBytes(options.FilePath);
        }
        else if (Console.IsInputRedirected)
        {
            using var ms = new MemoryStream();
            using var stdin = Console.OpenStandardInput();
            stdin.CopyTo(ms);
            data = ms.ToArray();
        }
        else
        {
            throw new InvalidOperationException("No input file specified and stdin is not redirected.");
        }

        // Apply offset and length
        if (options.Offset > 0 || options.Length > 0)
        {
            var offset = (int)Math.Min(options.Offset, data.Length);
            var length = options.Length > 0 ? Math.Min(options.Length, data.Length - offset) : data.Length - offset;
            data = data.AsSpan(offset, length).ToArray();
        }

        return data;
    }

    private static long ParseNumber(string value)
    {
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return Convert.ToInt64(value[2..], 16);
        return long.Parse(value);
    }
}

/// <summary>
/// Command options.
/// </summary>
public class CommandOptions
{
    public string? FilePath { get; set; }
    public long Offset { get; set; }
    public int Length { get; set; }
    public bool? ColorEnabled { get; set; }
    public bool Verbose { get; set; }
    public bool Quiet { get; set; }
    public string? Format { get; set; }
    public string? OutputPath { get; set; }
}
