# NCcsds.Viewer

Data visualization console application for viewing CCSDS frames, packets, and PDUs.

## Requirements

### REQ-VIEW-001: TM Frame Display
**Description:** The system shall display telemetry frame contents in human-readable format.

**Acceptance Criteria:**
```gherkin
Given a telemetry transfer frame
When the frame is loaded in the viewer
Then frame header fields shall be displayed
And field values shall be labeled
And data field shall be shown in hex and ASCII
And virtual channel shall be highlighted
```

### REQ-VIEW-002: TC Frame Display
**Description:** The system shall display telecommand frame contents.

**Acceptance Criteria:**
```gherkin
Given a telecommand transfer frame
When the frame is loaded in the viewer
Then frame header fields shall be displayed
And sequence number shall be shown
And bypass/control flag shall be indicated
And FECF validity shall be displayed
```

### REQ-VIEW-003: AOS Frame Display
**Description:** The system shall display AOS frame contents.

**Acceptance Criteria:**
```gherkin
Given an AOS transfer frame
When the frame is loaded in the viewer
Then AOS-specific header fields shall be displayed
And insert zone shall be shown if present
And frame data zone shall be displayed
And signaling field shall be decoded
```

### REQ-VIEW-004: Space Packet Display
**Description:** The system shall display CCSDS Space Packet contents.

**Acceptance Criteria:**
```gherkin
Given a CCSDS Space Packet
When the packet is loaded in the viewer
Then primary header shall be decoded and displayed
And APID shall be shown
And sequence count shall be displayed
And packet data shall be shown in configurable format
```

### REQ-VIEW-005: PUS Packet Display
**Description:** The system shall display PUS packet structure.

**Acceptance Criteria:**
```gherkin
Given a PUS telemetry or telecommand packet
When the packet is loaded in the viewer
Then PUS header fields shall be decoded
And service type/subtype shall be displayed
And source data shall be shown
And spare fields shall be indicated
```

### REQ-VIEW-006: CFDP PDU Display
**Description:** The system shall display CFDP PDU contents.

**Acceptance Criteria:**
```gherkin
Given a CFDP PDU
When the PDU is loaded in the viewer
Then PDU header shall be decoded
And PDU type shall be identified
And directive/file data shall be displayed
And entity IDs shall be shown
```

### REQ-VIEW-007: SLE PDU Display
**Description:** The system shall display SLE PDU contents.

**Acceptance Criteria:**
```gherkin
Given an SLE PDU
When the PDU is loaded in the viewer
Then ASN.1 structure shall be decoded
And operation type shall be identified
And parameters shall be displayed
And credential information shall be indicated
```

### REQ-VIEW-008: Hex/Binary View
**Description:** The system shall provide hex and binary data views.

**Acceptance Criteria:**
```gherkin
Given any data structure
When hex view is selected
Then data shall be displayed in hexadecimal format
And byte offsets shall be shown
And ASCII representation shall be displayed alongside

When binary view is selected
Then data shall be displayed in binary format
And bit positions shall be indicated
```

### REQ-VIEW-009: Field Highlighting
**Description:** The system shall highlight selected fields in data view.

**Acceptance Criteria:**
```gherkin
Given a decoded data structure displayed in viewer
When a field is selected
Then the corresponding bytes shall be highlighted
And field boundaries shall be clearly indicated
And field details shall be shown in info panel
```

### REQ-VIEW-010: Export Functionality
**Description:** The system shall support exporting viewed data.

**Acceptance Criteria:**
```gherkin
Given data displayed in the viewer
When export is requested
Then data shall be exportable in multiple formats (CSV, JSON, binary)
And field names shall be included in export
And export shall preserve data fidelity
```

## Roadmap

### Phase 1: Core Viewer Framework (v0.1.0)
- [x] Create console application structure
- [x] Implement hex dump display
- [x] Add binary display mode
- [x] Create ASCII sidebar
- [x] Implement color formatting

### Phase 2: Frame Display (v0.2.0)
- [x] Implement TM frame viewer
- [x] Implement AOS frame viewer
- [x] Implement TC frame viewer
- [x] Add frame header field display
- [x] Create FECF validation display

### Phase 3: Packet Display (v0.3.0)
- [x] Implement Space Packet viewer
- [x] Add APID display
- [x] Implement PUS header viewer
- [x] Add service type/subtype display
- [x] Create packet data viewer

### Phase 4: PDU Display (v0.4.0)
- [x] Implement CFDP PDU viewer
- [x] Implement SLE PDU viewer
- [x] Add ASN.1 decoding display
- [x] Create PDU type identification

### Phase 5: Interactive Features (v0.5.0)
- [x] Add file input support
- [x] Implement stdin/pipe support
- [x] Add field selection
- [x] Create field highlighting
- [x] Implement navigation

### Phase 6: Export & Integration (v0.6.0)
- [x] Implement CSV export
- [x] Implement JSON export
- [x] Add binary export
- [x] Create report generation
- [x] Add batch processing

### Phase 7: Polish (v1.0.0)
- [x] Complete help documentation
- [x] Add configuration file support
- [x] Improve error messages
- [x] Add verbose/quiet modes
- [x] Release stable version
