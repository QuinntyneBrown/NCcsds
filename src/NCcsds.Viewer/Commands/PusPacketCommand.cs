using NCcsds.Encoding.Packets;
using NCcsds.Viewer.Display;

namespace NCcsds.Viewer.Commands;

/// <summary>
/// PUS Packet viewer command.
/// </summary>
public static class PusPacketCommand
{
    public static int Execute(string[] args)
    {
        var options = CommandBase.ParseOptions(args);
        var data = CommandBase.ReadInputData(options);

        if (!options.Quiet)
            ConsoleDisplay.WriteSection("PUS Packet");

        try
        {
            // First decode as space packet
            var spacePacket = SpacePacket.Decode(data);

            ConsoleDisplay.WriteInfo($"Space Packet: {(spacePacket.Type == PacketType.Telemetry ? "Telemetry" : "Telecommand")}");
            ConsoleDisplay.WriteFieldHex("APID", spacePacket.Apid, 11);
            ConsoleDisplay.WriteFieldHex("Sequence Count", spacePacket.SequenceCount, 14);

            Console.WriteLine();

            if (spacePacket.Type == PacketType.Telemetry)
            {
                var pus = PusTmPacket.Decode(data);

                ConsoleDisplay.WriteSection("PUS TM Header");
                ConsoleDisplay.WriteField("PUS Version", pus.PusVersion);
                ConsoleDisplay.WriteField("Spacecraft Time Ref Status", pus.SpacecraftTimeRefStatus);
                ConsoleDisplay.WriteFieldHex("Service Type", pus.ServiceType, 8);
                ConsoleDisplay.WriteFieldHex("Service Subtype", pus.ServiceSubtype, 8);
                ConsoleDisplay.WriteFieldHex("Message Type Counter", pus.MessageTypeCounter, 16);
                ConsoleDisplay.WriteFieldHex("Destination ID", pus.DestinationId, 16);

                if (pus.Time != null)
                {
                    Console.WriteLine();
                    ConsoleDisplay.WriteField("Timestamp", pus.Time.Value);
                }

                Console.WriteLine();
                ConsoleDisplay.WriteField("Source Data Length", $"{pus.SourceData.Length} bytes");

                Console.WriteLine();
                ConsoleDisplay.WriteInfo($"Service: {GetServiceName(pus.ServiceType)}");
            }
            else
            {
                var pus = PusTcPacket.Decode(data);

                ConsoleDisplay.WriteSection("PUS TC Header");
                ConsoleDisplay.WriteField("PUS Version", pus.PusVersion);
                ConsoleDisplay.WriteField("Ack Flags", FormatAckFlags(pus.AckFlags));
                ConsoleDisplay.WriteFieldHex("Service Type", pus.ServiceType, 8);
                ConsoleDisplay.WriteFieldHex("Service Subtype", pus.ServiceSubtype, 8);
                ConsoleDisplay.WriteFieldHex("Source ID", pus.SourceId, 16);

                Console.WriteLine();
                ConsoleDisplay.WriteField("Application Data Length", $"{pus.ApplicationData.Length} bytes");

                Console.WriteLine();
                ConsoleDisplay.WriteInfo($"Service: {GetServiceName(pus.ServiceType)}");
            }

            if (options.Verbose)
            {
                Console.WriteLine();
                ConsoleDisplay.WriteSection("Raw Packet Data");
                ConsoleDisplay.WriteHexDump(data);
            }

            return 0;
        }
        catch (Exception ex)
        {
            ConsoleDisplay.WriteError($"Failed to decode PUS packet: {ex.Message}");
            Console.WriteLine();
            ConsoleDisplay.WriteInfo("Raw data:");
            ConsoleDisplay.WriteHexDump(data);
            return 1;
        }
    }

    private static string FormatAckFlags(byte flags)
    {
        var parts = new List<string>();
        if ((flags & 0x01) != 0) parts.Add("Acceptance");
        if ((flags & 0x02) != 0) parts.Add("Start");
        if ((flags & 0x04) != 0) parts.Add("Progress");
        if ((flags & 0x08) != 0) parts.Add("Completion");
        return parts.Count > 0 ? string.Join(", ", parts) : "None";
    }

    private static string GetServiceName(byte serviceType)
    {
        return serviceType switch
        {
            1 => "Request Verification",
            2 => "Device Access",
            3 => "Housekeeping",
            4 => "Parameter Statistics Reporting",
            5 => "Event Reporting",
            6 => "Memory Management",
            8 => "Function Management",
            9 => "Time Management",
            11 => "Time-based Scheduling",
            12 => "On-board Monitoring",
            13 => "Large Packet Transfer",
            14 => "Real-time Forwarding Control",
            15 => "On-board Storage and Retrieval",
            17 => "Test",
            18 => "On-board Control Procedure",
            19 => "Event-action",
            20 => "Parameter Management",
            21 => "Request Sequencing",
            _ => $"Service {serviceType}"
        };
    }
}
