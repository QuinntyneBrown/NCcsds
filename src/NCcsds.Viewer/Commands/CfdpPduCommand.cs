using NCcsds.Cfdp.Pdu;
using NCcsds.Viewer.Display;

namespace NCcsds.Viewer.Commands;

/// <summary>
/// CFDP PDU viewer command.
/// </summary>
public static class CfdpPduCommand
{
    public static int Execute(string[] args)
    {
        var options = CommandBase.ParseOptions(args);
        var data = CommandBase.ReadInputData(options);

        if (!options.Quiet)
            ConsoleDisplay.WriteSection("CFDP PDU");

        try
        {
            var header = PduHeader.Decode(data, out var headerLength);

            // Display header fields
            ConsoleDisplay.WriteField("PDU Version", header.Version);
            ConsoleDisplay.WriteField("PDU Type", header.Type == PduType.FileDirective ? "File Directive" : "File Data");
            ConsoleDisplay.WriteField("Direction", header.Direction == PduDirection.TowardReceiver ? "Toward Receiver" : "Toward Sender");
            ConsoleDisplay.WriteField("Transmission Mode", header.TransmissionMode == TransmissionMode.Acknowledged ? "Acknowledged (Class 2)" : "Unacknowledged (Class 1)");
            ConsoleDisplay.WriteField("CRC Present", header.CrcPresent);
            ConsoleDisplay.WriteField("Large File Flag", header.LargeFileFlag);
            ConsoleDisplay.WriteFieldHex("Data Field Length", header.DataFieldLength, 16);
            ConsoleDisplay.WriteField("Segmentation Control", header.SegmentationControl);
            ConsoleDisplay.WriteField("Entity ID Length", $"{header.EntityIdLength} bytes");
            ConsoleDisplay.WriteField("Sequence Number Length", $"{header.SequenceNumberLength} bytes");
            ConsoleDisplay.WriteFieldHex("Source Entity ID", (long)header.SourceEntityId, header.EntityIdLength * 8);
            ConsoleDisplay.WriteFieldHex("Transaction Sequence Number", (long)header.TransactionSequenceNumber, header.SequenceNumberLength * 8);
            ConsoleDisplay.WriteFieldHex("Destination Entity ID", (long)header.DestinationEntityId, header.EntityIdLength * 8);

            Console.WriteLine();

            // Decode specific PDU type
            var pduData = data.AsSpan(headerLength);

            if (header.Type == PduType.FileDirective)
            {
                var directiveCode = (DirectiveCode)pduData[0];
                ConsoleDisplay.WriteField("Directive Code", FormatDirectiveCode(directiveCode));

                switch (directiveCode)
                {
                    case DirectiveCode.Metadata:
                        DisplayMetadataPdu(header, pduData);
                        break;
                    case DirectiveCode.Eof:
                        DisplayEofPdu(header, pduData);
                        break;
                    case DirectiveCode.Finished:
                        DisplayFinishedPdu(header, pduData);
                        break;
                    case DirectiveCode.Ack:
                        DisplayAckPdu(header, pduData);
                        break;
                    case DirectiveCode.Nak:
                        DisplayNakPdu(header, pduData);
                        break;
                }
            }
            else
            {
                var fileDataPdu = FileDataPdu.Decode(header, pduData);
                ConsoleDisplay.WriteFieldHex("File Offset", (long)fileDataPdu.Offset, header.LargeFileFlag ? 64 : 32);
                ConsoleDisplay.WriteField("Data Length", $"{fileDataPdu.Data.Length} bytes");
            }

            if (options.Verbose)
            {
                Console.WriteLine();
                ConsoleDisplay.WriteSection("Raw PDU Data");
                ConsoleDisplay.WriteHighlightedHexDump(data,
                    (0, headerLength, ConsoleColor.Cyan, "PDU Header"),
                    (headerLength, header.DataFieldLength, ConsoleColor.White, "PDU Data"));
            }

            return 0;
        }
        catch (Exception ex)
        {
            ConsoleDisplay.WriteError($"Failed to decode CFDP PDU: {ex.Message}");
            Console.WriteLine();
            ConsoleDisplay.WriteInfo("Raw data:");
            ConsoleDisplay.WriteHexDump(data);
            return 1;
        }
    }

    private static void DisplayMetadataPdu(PduHeader header, ReadOnlySpan<byte> data)
    {
        var metadata = MetadataPdu.Decode(header, data);
        Console.WriteLine();
        ConsoleDisplay.WriteInfo("Metadata PDU Details");
        ConsoleDisplay.WriteField("Closure Requested", metadata.ClosureRequested);
        ConsoleDisplay.WriteField("Checksum Type", metadata.ChecksumType);
        ConsoleDisplay.WriteField("File Size", $"{metadata.FileSize} bytes");
        ConsoleDisplay.WriteField("Source File Name", metadata.SourceFileName);
        ConsoleDisplay.WriteField("Destination File Name", metadata.DestinationFileName);
    }

    private static void DisplayEofPdu(PduHeader header, ReadOnlySpan<byte> data)
    {
        var eof = EofPdu.Decode(header, data);
        Console.WriteLine();
        ConsoleDisplay.WriteInfo("EOF PDU Details");
        ConsoleDisplay.WriteField("Condition Code", eof.ConditionCode);
        ConsoleDisplay.WriteFieldHex("File Checksum", eof.Checksum, 32);
        ConsoleDisplay.WriteField("File Size", $"{eof.FileSize} bytes");
    }

    private static void DisplayFinishedPdu(PduHeader header, ReadOnlySpan<byte> data)
    {
        var finished = FinishedPdu.Decode(header, data);
        Console.WriteLine();
        ConsoleDisplay.WriteInfo("Finished PDU Details");
        ConsoleDisplay.WriteField("Condition Code", finished.ConditionCode);
        ConsoleDisplay.WriteField("Delivery Code", finished.DeliveryCode ? "Data complete" : "Data incomplete");
        ConsoleDisplay.WriteField("File Status", finished.FileStatus);
    }

    private static void DisplayAckPdu(PduHeader header, ReadOnlySpan<byte> data)
    {
        var ack = AckPdu.Decode(header, data);
        Console.WriteLine();
        ConsoleDisplay.WriteInfo("ACK PDU Details");
        ConsoleDisplay.WriteField("Acknowledged Directive", FormatDirectiveCode(ack.AcknowledgedDirective));
        ConsoleDisplay.WriteField("Directive Subtype Code", ack.DirectiveSubtypeCode);
        ConsoleDisplay.WriteField("Condition Code", ack.ConditionCode);
        ConsoleDisplay.WriteField("Transaction Status", ack.TransactionStatus);
    }

    private static void DisplayNakPdu(PduHeader header, ReadOnlySpan<byte> data)
    {
        var nak = NakPdu.Decode(header, data);
        Console.WriteLine();
        ConsoleDisplay.WriteInfo("NAK PDU Details");
        ConsoleDisplay.WriteField("Start of Scope", $"{nak.StartOfScope} bytes");
        ConsoleDisplay.WriteField("End of Scope", $"{nak.EndOfScope} bytes");
        ConsoleDisplay.WriteField("Segment Requests", $"{nak.SegmentRequests.Count} gaps");
        foreach (var segment in nak.SegmentRequests)
        {
            ConsoleDisplay.WriteField("  Gap", $"{segment.StartOffset} - {segment.EndOffset}");
        }
    }

    private static string FormatDirectiveCode(DirectiveCode code)
    {
        return code switch
        {
            DirectiveCode.Eof => "EOF (End of File)",
            DirectiveCode.Finished => "Finished",
            DirectiveCode.Ack => "ACK (Acknowledgment)",
            DirectiveCode.Metadata => "Metadata",
            DirectiveCode.Nak => "NAK (Negative Acknowledgment)",
            DirectiveCode.Prompt => "Prompt",
            DirectiveCode.KeepAlive => "Keep Alive",
            _ => $"Unknown ({(int)code})"
        };
    }
}
