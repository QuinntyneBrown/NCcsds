# NCcsds

A comprehensive .NET 8.0 implementation of CCSDS (Consultative Committee for Space Data Systems) protocols and standards for space communication systems.

## Overview

NCcsds provides a complete suite of libraries for building ground systems, mission control software, and spacecraft simulators that need to process CCSDS-compliant data formats.

## Libraries

| Library | Description |
|---------|-------------|
| **NCcsds.Core** | Core types, identifiers, checksums, and utilities |
| **NCcsds.Encoding** | Data encoding/decoding, time formats, and packet structures |
| **NCcsds.TmTc** | Telemetry and telecommand frame processing with COP-1 |
| **NCcsds.Sle** | Space Link Extension (SLE) service implementations |
| **NCcsds.Cfdp** | CCSDS File Delivery Protocol (Class 1 & 2) |
| **NCcsds.Viewer** | Command-line tool for viewing CCSDS data |

## Features

### NCcsds.Core
- **Identifiers**: Spacecraft ID, Virtual Channel ID, APID, Master Channel ID
- **Buffers**: Bit-level and byte-level readers/writers with big-endian support
- **Checksums**: CRC-16-CCITT, CRC-32, and CCSDS checksum utilities
- **Result Types**: Functional error handling without exceptions
- **Frame Randomization**: Pseudo-random sequence generation per CCSDS standard

### NCcsds.Encoding
- **Primitives**: Integer, real, string, and octet string encoders
- **Time Formats**: CUC (CCSDS Unsegmented Code) and CDS (Day Segmented)
- **Space Packets**: Primary header encoding/decoding per CCSDS 133.0-B
- **PUS Packets**: Packet Utilization Standard TM/TC packets per ECSS-E-ST-70-41C

### NCcsds.TmTc
- **TM Frames**: Telemetry transfer frames with OCF and FECF support
- **TC Frames**: Telecommand transfer frames with bypass/control modes
- **AOS Frames**: Advanced Orbiting Systems frames with insert zones
- **COP-1**: Command Operation Procedure with FOP and FARM state machines
- **CLCW**: Command Link Control Word generation and parsing

### NCcsds.Sle
- **RAF Service**: Return All Frames with frame quality filtering
- **RCF Service**: Return Channel Frames with virtual channel selection
- **ROCF Service**: Return Operational Control Field extraction
- **CLTU Service**: Command Link Transmission Unit upload and radiation
- **Transport**: TCP/TLS transport with automatic reconnection
- **Authentication**: SHA-1 and SHA-256 credential encoding

### NCcsds.Cfdp
- **Class 1**: Unreliable file transfer (send and forget)
- **Class 2**: Reliable file transfer with NAK-based retransmission
- **PDUs**: Metadata, File Data, EOF, ACK, NAK, Finished
- **Transport**: TCP and UDP transport abstractions
- **Filestore**: Local filesystem operations with security controls

### NCcsds.Viewer
- **Hex Dump**: Colorized hex and ASCII display
- **Frame Viewers**: TM, TC, and AOS frame parsing and display
- **Packet Viewers**: Space Packet and PUS packet decoding
- **PDU Viewers**: CFDP and SLE PDU analysis
- **Export**: CSV, JSON, base64, C array, and binary formats

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022, VS Code, or JetBrains Rider

### Building

```bash
# Clone the repository
git clone https://github.com/QuinntyneBrown/NCcsds.git
cd NCcsds

# Build all projects
dotnet build

# Run tests
dotnet test
```

### Basic Usage

#### Processing TM Frames

```csharp
using NCcsds.TmTc.Frames;

// Decode a TM frame
byte[] frameData = /* received frame bytes */;
var frame = TmFrame.Decode(frameData);

Console.WriteLine($"Spacecraft ID: {frame.SpacecraftId}");
Console.WriteLine($"Virtual Channel: {frame.VirtualChannelId}");
Console.WriteLine($"Frame Count: {frame.VirtualChannelFrameCount}");
```

#### Creating Space Packets

```csharp
using NCcsds.Encoding.Packets;

// Create a telemetry packet
var packet = new SpacePacket
{
    Type = PacketType.Telemetry,
    Apid = 100,
    SequenceFlags = SequenceFlags.Unsegmented,
    SequenceCount = 1,
    UserData = new byte[] { 0x01, 0x02, 0x03 }
};

byte[] encoded = packet.Encode();
```

#### Using SLE Services

```csharp
using NCcsds.Sle.Raf;
using NCcsds.Sle.Factory;

// Create RAF service
var factory = new SleServiceFactory();
var config = new SleServiceConfiguration
{
    ServiceInstanceId = "sagr=1.spack=my-sc.raf=onlt1",
    Version = SleVersion.V5
};

var rafService = factory.CreateRafService(config);

// Bind and start
await rafService.BindAsync();
await rafService.StartAsync();

// Handle frames
rafService.FrameReceived += frame =>
{
    Console.WriteLine($"Received frame: {frame.FrameData.Length} bytes");
};
```

#### CFDP File Transfer

```csharp
using NCcsds.Cfdp.Entity;

// Create CFDP entity
var config = new CfdpEntityConfiguration
{
    EntityId = 1,
    FilestoreRoot = "/data/cfdp",
    DefaultTransmissionMode = TransmissionMode.Acknowledged
};

var entity = new CfdpEntity(config);

// Send a file
var request = new PutRequest
{
    DestinationEntityId = 2,
    SourceFileName = "telemetry.dat",
    DestinationFileName = "received/telemetry.dat"
};

var transactionId = entity.Put(request);
```

#### Using the Viewer CLI

```bash
# View a hex dump
nccsds-viewer hex data.bin

# Parse a TM frame
nccsds-viewer tm --file frame.bin --verbose

# View a Space Packet
nccsds-viewer packet -f packet.bin

# Export to JSON
nccsds-viewer export --format json --output data.json data.bin
```

## Project Structure

```
NCcsds/
├── src/
│   ├── NCcsds.Core/           # Core types and utilities
│   ├── NCcsds.Encoding/       # Encoding primitives and packets
│   ├── NCcsds.TmTc/           # Frame processing and COP-1
│   ├── NCcsds.Sle/            # SLE services
│   ├── NCcsds.Cfdp/           # File delivery protocol
│   └── NCcsds.Viewer/         # CLI visualization tool
├── tests/
│   ├── NCcsds.Core.Tests/
│   ├── NCcsds.Encoding.Tests/
│   ├── NCcsds.TmTc.Tests/
│   ├── NCcsds.Sle.Tests/
│   ├── NCcsds.Cfdp.Tests/
│   └── NCcsds.Viewer.Tests/
└── docs/
    ├── CCSDS_Requirements.md
    └── Implementation_Roadmap.md
```

## Standards Compliance

This library implements the following CCSDS standards:

| Standard | Title |
|----------|-------|
| CCSDS 131.0-B | TM Synchronization and Channel Coding |
| CCSDS 132.0-B | TM Space Data Link Protocol |
| CCSDS 133.0-B | Space Packet Protocol |
| CCSDS 231.0-B | TC Synchronization and Channel Coding |
| CCSDS 232.0-B | TC Space Data Link Protocol |
| CCSDS 232.1-B | COP-1 (Communications Operation Procedure) |
| CCSDS 727.0-B | CCSDS File Delivery Protocol (CFDP) |
| CCSDS 913.1-B | SLE Return All Frames Service |
| CCSDS 911.1-B | SLE Return Channel Frames Service |
| CCSDS 911.5-B | SLE Return Operational Control Field Service |
| CCSDS 912.1-B | SLE Forward CLTU Service |

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- CCSDS (Consultative Committee for Space Data Systems) for the protocol specifications
- ESA and NASA for reference implementations and documentation
