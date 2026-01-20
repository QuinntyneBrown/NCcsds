# NCcsds Implementation Roadmap

## Document Information
- **Version:** 1.0
- **Date:** 2026-01-19
- **Related Document:** CCSDS_Requirements.md

---

## Overview

This roadmap outlines the phased implementation plan for the NCcsds library suite. The implementation is organized into 6 phases, prioritizing foundational components first and building toward more complex functionality.

### Project Dependencies

```
NCcsds.Core (Foundation)
    ├── NCcsds.Encoding (depends on Core)
    │   └── NCcsds.TmTc (depends on Core, Encoding)
    │       └── NCcsds.Sle (depends on Core, TmTc)
    │   └── NCcsds.Cfdp (depends on Core, Encoding)
    └── NCcsds.Viewer (depends on all libraries)
```

---

## Phase 1: Foundation (NCcsds.Core)

**Objective:** Establish core infrastructure, shared types, and common utilities.

### Milestone 1.1: Project Setup
| Task | Requirements | Deliverables |
|------|--------------|--------------|
| Create solution structure | REQ-COMPAT-001 | Solution file, project files |
| Configure .NET 8.0 targets | REQ-COMPAT-001 | Project configurations |
| Set up CI/CD pipeline | REQ-INT-001 | GitHub Actions workflows |
| Configure code analysis | REQ-QUAL-001 | Analyzers, style rules |
| Set up dependency management | REQ-INT-002 | Package references, lock files |

### Milestone 1.2: Core Infrastructure
| Task | Requirements | Deliverables |
|------|--------------|--------------|
| Implement logging framework | REQ-ERR-002 | ILogger abstraction, log levels |
| Create exception hierarchy | REQ-ERR-001 | Custom exception types |
| Define common interfaces | - | IFrame, IPacket, IPdu interfaces |
| Implement bit manipulation utilities | REQ-ENC-006 | BitReader, BitWriter classes |
| Create CRC calculation utilities | REQ-TM-004 | CRC-16-CCITT implementation |

### Milestone 1.3: Common Types
| Task | Requirements | Deliverables |
|------|--------------|--------------|
| Define spacecraft identifiers | REQ-TM-001 | SCID, VCID types |
| Create frame quality enumerations | REQ-SLE-004 | Quality indicator types |
| Implement state machine base | REQ-COP-001 | State machine infrastructure |
| Define configuration models | - | Configuration DTOs |

**Phase 1 Exit Criteria:**
- [ ] All core projects compile successfully
- [ ] Unit test framework operational
- [ ] CI/CD pipeline running
- [ ] Code coverage reporting active
- [ ] Core utilities have 90%+ test coverage

---

## Phase 2: Encoding/Decoding (NCcsds.Encoding)

**Objective:** Implement data encoding/decoding capabilities and CCSDS time formats.

### Milestone 2.1: Basic Data Types
| Task | Requirements | Deliverables |
|------|--------------|--------------|
| Integer encoding/decoding | REQ-ENC-001 | Signed/unsigned integer support |
| Real number encoding/decoding | REQ-ENC-002 | IEEE 754 float/double support |
| Enumeration encoding/decoding | REQ-ENC-003 | Enum value mapping |
| String encoding/decoding | REQ-ENC-004 | ASCII/UTF-8 string support |
| Octet stream encoding/decoding | REQ-ENC-005 | Byte array handling |
| Bit stream encoding/decoding | REQ-ENC-006 | Arbitrary bit-length support |

### Milestone 2.2: CCSDS Time Formats
| Task | Requirements | Deliverables |
|------|--------------|--------------|
| CUC time encoding/decoding | REQ-ENC-007 | CUC format implementation |
| CDS time encoding/decoding | REQ-ENC-008 | CDS format implementation |
| Relative time encoding | REQ-ENC-009 | Duration encoding support |
| Time epoch management | REQ-ENC-007, REQ-ENC-008 | Configurable epoch handling |

### Milestone 2.3: Packet Processing
| Task | Requirements | Deliverables |
|------|--------------|--------------|
| Packet identification | REQ-ENC-010 | Packet type identification |
| ECSS PUS header support | REQ-ENC-011 | PUS header encoding/decoding |
| External definition support | REQ-ENC-012 | XML/JSON definition parser |
| Definition-based encoding | REQ-ENC-012 | Dynamic encoding from definitions |

**Phase 2 Exit Criteria:**
- [ ] All encoding/decoding operations verified against CCSDS specifications
- [ ] Time format conversions validated with reference implementations
- [ ] PUS packet support operational
- [ ] External definition loading functional
- [ ] 90%+ test coverage achieved

---

## Phase 3: TM/TC Processing (NCcsds.TmTc)

**Objective:** Implement telemetry and telecommand frame processing with COP-1 support.

### Milestone 3.1: Telemetry Frame Processing
| Task | Requirements | Deliverables |
|------|--------------|--------------|
| TM frame parsing | REQ-TM-001 | TM frame parser |
| TM frame construction | REQ-TM-002 | TM frame builder |
| Virtual channel demultiplexing | REQ-TM-003 | VC demultiplexer |
| Frame error detection (FECF) | REQ-TM-004 | CRC validation |

### Milestone 3.2: AOS Frame Processing
| Task | Requirements | Deliverables |
|------|--------------|--------------|
| AOS frame parsing | REQ-TM-005 | AOS frame parser |
| Insert zone extraction | REQ-TM-006 | Insert zone handler |
| Signaling field parsing | REQ-TM-005 | Signaling field decoder |

### Milestone 3.3: Telecommand Frame Processing
| Task | Requirements | Deliverables |
|------|--------------|--------------|
| TC frame parsing | REQ-TC-001 | TC frame parser |
| TC frame construction | REQ-TC-002 | TC frame builder |
| TC segment assembly | REQ-TC-003 | Segment assembler |

### Milestone 3.4: COP-1 Implementation
| Task | Requirements | Deliverables |
|------|--------------|--------------|
| FOP state machine | REQ-COP-001 | FOP implementation |
| FARM state machine | REQ-COP-002 | FARM implementation |
| CLCW generation | REQ-COP-003 | CLCW generator |
| AD/BD mode support | REQ-COP-004 | Mode handling |

### Milestone 3.5: Frame Processing Operations
| Task | Requirements | Deliverables |
|------|--------------|--------------|
| Frame randomization | REQ-PROC-001 | Randomizer/derandomizer |
| Reed-Solomon verification | REQ-PROC-002 | RS syndrome calculation |

**Phase 3 Exit Criteria:**
- [ ] TM/TC frames interoperable with reference implementations
- [ ] COP-1 state machines validated against CCSDS 232.1-B
- [ ] Frame processing meets 10 Mbps throughput requirement (REQ-QUAL-002)
- [ ] AOS frame support complete
- [ ] 90%+ test coverage achieved

---

## Phase 4: SLE Services (NCcsds.Sle)

**Objective:** Implement Space Link Extension user services.

### Milestone 4.1: SLE Infrastructure
| Task | Requirements | Deliverables |
|------|--------------|--------------|
| ASN.1 encoding/decoding | REQ-SLE-014 | BER/DER encoder/decoder |
| SLE PDU framework | REQ-SLE-014 | PDU base classes |
| Version negotiation | REQ-SLE-014 | Multi-version support |
| Connection management | - | TCP connection handler |

### Milestone 4.2: SLE Security
| Task | Requirements | Deliverables |
|------|--------------|--------------|
| SHA-1 credential encoding | REQ-SLE-012 | SHA-1 authentication |
| SHA-256 credential encoding | REQ-SLE-013 | SHA-256 authentication |
| Credential management | REQ-SLE-012, REQ-SLE-013 | Credential store |

### Milestone 4.3: RAF Service
| Task | Requirements | Deliverables |
|------|--------------|--------------|
| RAF initialization | REQ-SLE-001 | RAF service instance |
| RAF binding | REQ-SLE-002 | Bind/unbind operations |
| RAF data transfer | REQ-SLE-003 | Start/stop operations |
| RAF frame quality filtering | REQ-SLE-004 | Quality filter |

### Milestone 4.4: RCF Service
| Task | Requirements | Deliverables |
|------|--------------|--------------|
| RCF virtual channel selection | REQ-SLE-005 | VC selector |
| RCF master channel selection | REQ-SLE-006 | MC mode support |

### Milestone 4.5: ROCF Service
| Task | Requirements | Deliverables |
|------|--------------|--------------|
| ROCF OCF extraction | REQ-SLE-007 | OCF extractor |
| ROCF control word filtering | REQ-SLE-008 | Control word filter |

### Milestone 4.6: CLTU Service
| Task | Requirements | Deliverables |
|------|--------------|--------------|
| CLTU upload | REQ-SLE-009 | CLTU transfer |
| CLTU radiation notification | REQ-SLE-010 | Async notifications |
| CLTU production status | REQ-SLE-011 | Status reporting |

**Phase 4 Exit Criteria:**
- [ ] All SLE services bindable to test provider
- [ ] Multi-version protocol support verified
- [ ] Security mechanisms validated
- [ ] Interoperability tested with reference SLE provider
- [ ] 90%+ test coverage achieved

---

## Phase 5: CFDP (NCcsds.Cfdp)

**Objective:** Implement CCSDS File Delivery Protocol.

### Milestone 5.1: CFDP Infrastructure
| Task | Requirements | Deliverables |
|------|--------------|--------------|
| Entity initialization | REQ-CFDP-001 | CFDP entity class |
| Multiple entity support | REQ-CFDP-002 | Entity manager |
| MIB implementation | REQ-CFDP-001 | MIB configuration |
| Filestore abstraction | REQ-CFDP-001 | Filestore interface |

### Milestone 5.2: Class 1 Transfer
| Task | Requirements | Deliverables |
|------|--------------|--------------|
| Class 1 transmission | REQ-CFDP-003 | Unreliable sender |
| Class 1 reception | REQ-CFDP-004 | Unreliable receiver |
| PDU encoding/decoding | REQ-CFDP-003, REQ-CFDP-004 | PDU codec |

### Milestone 5.3: Class 2 Transfer
| Task | Requirements | Deliverables |
|------|--------------|--------------|
| Class 2 transmission | REQ-CFDP-005 | Reliable sender |
| Class 2 reception | REQ-CFDP-006 | Reliable receiver |
| Retransmission handling | REQ-CFDP-007 | NAK processor |
| Timer management | REQ-CFDP-005, REQ-CFDP-006 | Transaction timers |

### Milestone 5.4: Transport Layer
| Task | Requirements | Deliverables |
|------|--------------|--------------|
| TCP transport | REQ-CFDP-008 | TCP transport layer |
| UDP transport | REQ-CFDP-009 | UDP transport layer |
| Transport abstraction | REQ-CFDP-008, REQ-CFDP-009 | Transport interface |

### Milestone 5.5: CFDP Operations
| Task | Requirements | Deliverables |
|------|--------------|--------------|
| Put request processing | REQ-CFDP-010 | Put operation handler |
| Filestore operations | REQ-CFDP-011 | Directive processor |
| Transaction monitoring | REQ-CFDP-012 | Transaction monitor |

### Milestone 5.6: Fault Handling
| Task | Requirements | Deliverables |
|------|--------------|--------------|
| Fault handler configuration | REQ-CFDP-013 | Fault handler framework |
| Transaction suspension | REQ-CFDP-014 | Suspend/resume support |
| Transaction cancellation | REQ-CFDP-013 | Cancel operation |

**Phase 5 Exit Criteria:**
- [ ] Class 1 and Class 2 transfers functional
- [ ] Interoperability tested with reference CFDP implementation
- [ ] Fault handling scenarios validated
- [ ] Both TCP and UDP transports operational
- [ ] 90%+ test coverage achieved

---

## Phase 6: Data Viewer (NCcsds.Viewer)

**Objective:** Create console application for data visualization.

### Milestone 6.1: Viewer Infrastructure
| Task | Requirements | Deliverables |
|------|--------------|--------------|
| Console UI framework | - | Terminal UI setup |
| Data loading | - | File/stream input |
| View management | - | View controller |

### Milestone 6.2: Frame Visualization
| Task | Requirements | Deliverables |
|------|--------------|--------------|
| TM frame display | REQ-VIEW-001 | TM frame view |
| TC frame display | REQ-VIEW-002 | TC frame view |
| AOS frame display | REQ-VIEW-003 | AOS frame view |

### Milestone 6.3: Packet Visualization
| Task | Requirements | Deliverables |
|------|--------------|--------------|
| Space packet display | REQ-VIEW-004 | Packet view |
| PUS packet display | REQ-VIEW-005 | PUS decoder view |

### Milestone 6.4: PDU Visualization
| Task | Requirements | Deliverables |
|------|--------------|--------------|
| CFDP PDU display | REQ-VIEW-006 | CFDP PDU view |
| SLE PDU display | REQ-VIEW-007 | SLE PDU view |

### Milestone 6.5: General Features
| Task | Requirements | Deliverables |
|------|--------------|--------------|
| Hex/binary view | REQ-VIEW-008 | Hex viewer |
| Field highlighting | REQ-VIEW-009 | Selection highlighting |
| Export functionality | REQ-VIEW-010 | CSV/JSON/binary export |

**Phase 6 Exit Criteria:**
- [ ] All data types viewable
- [ ] Export functionality verified
- [ ] Usability validated
- [ ] Documentation complete

---

## Cross-Phase Activities

These activities run parallel to the main phases:

### Documentation (Continuous)
| Activity | Requirements | Deliverables |
|----------|--------------|--------------|
| XML documentation | REQ-DOC-001 | API docs on all public members |
| User guide creation | REQ-DOC-002 | User documentation |
| Example code | REQ-DOC-001 | Code samples |

### Quality Assurance (Continuous)
| Activity | Requirements | Deliverables |
|----------|--------------|--------------|
| Unit testing | REQ-QUAL-001 | 90%+ coverage |
| Integration testing | REQ-QUAL-001 | End-to-end tests |
| Performance testing | REQ-QUAL-002 | Benchmark suite |
| Standards compliance | REQ-COMPAT-002 | Compliance reports |

---

## Requirements Traceability Matrix

| Requirement | Phase | Milestone |
|-------------|-------|-----------|
| REQ-SLE-001 | 4 | 4.3 |
| REQ-SLE-002 | 4 | 4.3 |
| REQ-SLE-003 | 4 | 4.3 |
| REQ-SLE-004 | 4 | 4.3 |
| REQ-SLE-005 | 4 | 4.4 |
| REQ-SLE-006 | 4 | 4.4 |
| REQ-SLE-007 | 4 | 4.5 |
| REQ-SLE-008 | 4 | 4.5 |
| REQ-SLE-009 | 4 | 4.6 |
| REQ-SLE-010 | 4 | 4.6 |
| REQ-SLE-011 | 4 | 4.6 |
| REQ-SLE-012 | 4 | 4.2 |
| REQ-SLE-013 | 4 | 4.2 |
| REQ-SLE-014 | 4 | 4.1 |
| REQ-TM-001 | 3 | 3.1 |
| REQ-TM-002 | 3 | 3.1 |
| REQ-TM-003 | 3 | 3.1 |
| REQ-TM-004 | 3 | 3.1 |
| REQ-TM-005 | 3 | 3.2 |
| REQ-TM-006 | 3 | 3.2 |
| REQ-TC-001 | 3 | 3.3 |
| REQ-TC-002 | 3 | 3.3 |
| REQ-TC-003 | 3 | 3.3 |
| REQ-COP-001 | 3 | 3.4 |
| REQ-COP-002 | 3 | 3.4 |
| REQ-COP-003 | 3 | 3.4 |
| REQ-COP-004 | 3 | 3.4 |
| REQ-PROC-001 | 3 | 3.5 |
| REQ-PROC-002 | 3 | 3.5 |
| REQ-ENC-001 | 2 | 2.1 |
| REQ-ENC-002 | 2 | 2.1 |
| REQ-ENC-003 | 2 | 2.1 |
| REQ-ENC-004 | 2 | 2.1 |
| REQ-ENC-005 | 2 | 2.1 |
| REQ-ENC-006 | 2 | 2.1 |
| REQ-ENC-007 | 2 | 2.2 |
| REQ-ENC-008 | 2 | 2.2 |
| REQ-ENC-009 | 2 | 2.2 |
| REQ-ENC-010 | 2 | 2.3 |
| REQ-ENC-011 | 2 | 2.3 |
| REQ-ENC-012 | 2 | 2.3 |
| REQ-CFDP-001 | 5 | 5.1 |
| REQ-CFDP-002 | 5 | 5.1 |
| REQ-CFDP-003 | 5 | 5.2 |
| REQ-CFDP-004 | 5 | 5.2 |
| REQ-CFDP-005 | 5 | 5.3 |
| REQ-CFDP-006 | 5 | 5.3 |
| REQ-CFDP-007 | 5 | 5.3 |
| REQ-CFDP-008 | 5 | 5.4 |
| REQ-CFDP-009 | 5 | 5.4 |
| REQ-CFDP-010 | 5 | 5.5 |
| REQ-CFDP-011 | 5 | 5.5 |
| REQ-CFDP-012 | 5 | 5.5 |
| REQ-CFDP-013 | 5 | 5.6 |
| REQ-CFDP-014 | 5 | 5.6 |
| REQ-VIEW-001 | 6 | 6.2 |
| REQ-VIEW-002 | 6 | 6.2 |
| REQ-VIEW-003 | 6 | 6.2 |
| REQ-VIEW-004 | 6 | 6.3 |
| REQ-VIEW-005 | 6 | 6.3 |
| REQ-VIEW-006 | 6 | 6.4 |
| REQ-VIEW-007 | 6 | 6.4 |
| REQ-VIEW-008 | 6 | 6.5 |
| REQ-VIEW-009 | 6 | 6.5 |
| REQ-VIEW-010 | 6 | 6.5 |
| REQ-QUAL-001 | 1-6 | Continuous |
| REQ-QUAL-002 | 3-5 | Phase exit |
| REQ-COMPAT-001 | 1 | 1.1 |
| REQ-COMPAT-002 | 2-5 | Phase exit |
| REQ-DOC-001 | 1-6 | Continuous |
| REQ-DOC-002 | 6 | Post-completion |
| REQ-INT-001 | 1 | 1.1 |
| REQ-INT-002 | 1 | 1.1 |
| REQ-ERR-001 | 1 | 1.2 |
| REQ-ERR-002 | 1 | 1.2 |

---

## Risk Mitigation

| Risk | Mitigation Strategy |
|------|---------------------|
| ASN.1 encoding complexity (SLE) | Evaluate existing .NET ASN.1 libraries; fallback to manual implementation |
| COP-1 state machine correctness | Extensive testing against CCSDS test vectors |
| Performance requirements | Early prototyping and benchmarking; profile-guided optimization |
| Standards interpretation | Reference existing Java implementation; consult CCSDS blue books |
| Interoperability | Plan integration testing with reference implementations |

---

## Success Metrics

| Metric | Target |
|--------|--------|
| Test Coverage | >= 90% line coverage, >= 85% branch coverage |
| Performance | 10 Mbps TM throughput, < 1ms packet encoding |
| Standards Compliance | Pass all applicable CCSDS conformance tests |
| Documentation | 100% public API documentation coverage |
| Build Health | Zero warnings, zero deprecated API usage |

---

## Appendix: Phase Summary

| Phase | Focus Area | Key Deliverables |
|-------|------------|------------------|
| 1 | Foundation | Core infrastructure, utilities, CI/CD |
| 2 | Encoding | Data types, time formats, packet processing |
| 3 | TM/TC | Frame processing, COP-1 |
| 4 | SLE | RAF, RCF, ROCF, CLTU services |
| 5 | CFDP | File delivery protocol |
| 6 | Viewer | Console visualization tool |
