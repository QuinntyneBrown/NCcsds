using System.Text;
using System.Text.Json;
using NCcsds.Viewer.Display;

namespace NCcsds.Viewer.Commands;

/// <summary>
/// Export command for various output formats.
/// </summary>
public static class ExportCommand
{
    public static int Execute(string[] args)
    {
        var options = CommandBase.ParseOptions(args);
        var data = CommandBase.ReadInputData(options);

        var format = options.Format?.ToLowerInvariant() ?? "hex";
        var outputPath = options.OutputPath;

        if (!options.Quiet)
            ConsoleDisplay.WriteInfo($"Exporting {data.Length} bytes in {format} format");

        try
        {
            var output = format switch
            {
                "hex" => ExportHex(data),
                "bin" or "binary" => ExportBinary(data, outputPath),
                "c" or "carray" => ExportCArray(data),
                "json" => ExportJson(data),
                "base64" => ExportBase64(data),
                "csv" => ExportCsv(data),
                _ => throw new ArgumentException($"Unknown format: {format}")
            };

            if (!string.IsNullOrEmpty(outputPath))
            {
                if (format == "bin" || format == "binary")
                {
                    File.WriteAllBytes(outputPath, data);
                }
                else
                {
                    File.WriteAllText(outputPath, output);
                }
                if (!options.Quiet)
                    ConsoleDisplay.WriteSuccess($"Exported to {outputPath}");
            }
            else
            {
                Console.WriteLine(output);
            }

            return 0;
        }
        catch (Exception ex)
        {
            ConsoleDisplay.WriteError($"Export failed: {ex.Message}");
            return 1;
        }
    }

    private static string ExportHex(byte[] data)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < data.Length; i++)
        {
            sb.Append(data[i].ToString("X2"));
            if (i < data.Length - 1)
                sb.Append(' ');
            if ((i + 1) % 16 == 0)
                sb.AppendLine();
        }
        return sb.ToString();
    }

    private static string ExportBinary(byte[] data, string? outputPath)
    {
        if (string.IsNullOrEmpty(outputPath))
            throw new ArgumentException("Binary export requires --output path");
        return ""; // Binary written directly to file
    }

    private static string ExportCArray(byte[] data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("const unsigned char data[] = {");

        for (int i = 0; i < data.Length; i++)
        {
            if (i % 12 == 0)
                sb.Append("    ");

            sb.Append($"0x{data[i]:X2}");

            if (i < data.Length - 1)
                sb.Append(", ");

            if ((i + 1) % 12 == 0)
                sb.AppendLine();
        }

        if (data.Length % 12 != 0)
            sb.AppendLine();

        sb.AppendLine("};");
        sb.AppendLine($"const size_t data_len = {data.Length};");

        return sb.ToString();
    }

    private static string ExportJson(byte[] data)
    {
        var obj = new
        {
            length = data.Length,
            hex = BitConverter.ToString(data).Replace("-", ""),
            base64 = Convert.ToBase64String(data),
            bytes = data
        };

        return JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private static string ExportBase64(byte[] data)
    {
        return Convert.ToBase64String(data, Base64FormattingOptions.InsertLineBreaks);
    }

    private static string ExportCsv(byte[] data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Offset,Hex,Decimal,Binary,ASCII");

        for (int i = 0; i < data.Length; i++)
        {
            var b = data[i];
            var ascii = b >= 32 && b < 127 ? ((char)b).ToString() : "";
            sb.AppendLine($"{i},0x{b:X2},{b},{Convert.ToString(b, 2).PadLeft(8, '0')},{ascii}");
        }

        return sb.ToString();
    }
}
