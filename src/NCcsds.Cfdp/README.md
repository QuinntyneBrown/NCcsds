# NCcsds.Cfdp

CCSDS File Delivery Protocol (CFDP) implementation supporting Class 1 and Class 2 file transfers.

## Requirements

### REQ-CFDP-001: CFDP Entity Initialization
**Description:** The system shall support initialization of CFDP entity instances.

**Acceptance Criteria:**
```gherkin
Given CFDP entity configuration parameters
When a CFDP entity is initialized
Then the entity ID shall be assigned
And the filestore shall be configured
And the MIB (Management Information Base) shall be loaded
And the entity shall be ready to process transactions
```

### REQ-CFDP-002: Multiple Entity Support
**Description:** The system shall support multiple concurrent CFDP entity instances.

**Acceptance Criteria:**
```gherkin
Given system resources for multiple entities
When multiple CFDP entities are created
Then each entity shall have a unique identifier
And entities shall operate independently
And inter-entity communication shall be possible
And resource conflicts shall be prevented
```

### REQ-CFDP-003: Class 1 File Transmission
**Description:** The system shall support Class 1 (unreliable) file transmission.

**Acceptance Criteria:**
```gherkin
Given a source file and destination entity ID
When Class 1 transmission is initiated
Then metadata PDU shall be sent first
And file data PDUs shall be sent sequentially
And EOF PDU shall be sent upon completion
And no acknowledgment shall be expected
And transaction shall complete after EOF sent
```

### REQ-CFDP-004: Class 1 File Reception
**Description:** The system shall support Class 1 (unreliable) file reception.

**Acceptance Criteria:**
```gherkin
Given incoming Class 1 CFDP PDUs
When metadata PDU is received
Then a new receive transaction shall be created
And file parameters shall be extracted

When file data PDUs are received
Then data shall be written to local filestore
And gaps shall be recorded but not requested

When EOF PDU is received
Then file shall be closed
And checksum shall be verified
And transaction shall be marked complete
```

### REQ-CFDP-005: Class 2 File Transmission
**Description:** The system shall support Class 2 (reliable) file transmission.

**Acceptance Criteria:**
```gherkin
Given a source file and destination entity ID
When Class 2 transmission is initiated
Then metadata PDU shall be sent first
And file data PDUs shall be sent sequentially
And EOF PDU shall be sent upon initial completion
And sender shall wait for Finished PDU
And NAK (Negative Acknowledgment) PDUs shall trigger retransmission
And transaction shall complete upon Finished PDU receipt
```

### REQ-CFDP-006: Class 2 File Reception
**Description:** The system shall support Class 2 (reliable) file reception.

**Acceptance Criteria:**
```gherkin
Given incoming Class 2 CFDP PDUs
When file data PDUs are received with gaps
Then NAK PDUs shall be generated for missing data
And NAK timer shall be managed per MIB

When all data is received
Then checksum verification shall be performed
And Finished PDU shall be sent to source
And transaction shall complete upon ACK receipt
```

### REQ-CFDP-007: Retransmission Handling
**Description:** The system shall handle retransmission requests in Class 2 transfers.

**Acceptance Criteria:**
```gherkin
Given an active Class 2 send transaction
When a NAK PDU is received
Then the requested file segments shall be identified
And the missing data PDUs shall be retransmitted
And retransmission count shall be tracked
And excessive retransmissions shall trigger fault

Given an active Class 2 receive transaction
When NAK timer expires with outstanding gaps
Then NAK PDU shall be generated
And NAK shall list all outstanding gaps
And NAK limit shall be enforced
```

### REQ-CFDP-008: TCP Transport Support
**Description:** The system shall support TCP as a transport layer for CFDP.

**Acceptance Criteria:**
```gherkin
Given TCP transport configuration
When CFDP entity uses TCP transport
Then PDUs shall be sent via TCP connection
And connection management shall be handled
And PDU framing shall be correct
And connection failures shall be reported
```

### REQ-CFDP-009: UDP Transport Support
**Description:** The system shall support UDP as a transport layer for CFDP.

**Acceptance Criteria:**
```gherkin
Given UDP transport configuration
When CFDP entity uses UDP transport
Then PDUs shall be sent via UDP datagrams
And PDU size shall respect MTU limits
And packet loss shall be tolerated by protocol
And out-of-order delivery shall be handled
```

### REQ-CFDP-010: Put Request Processing
**Description:** The system shall process CFDP Put requests.

**Acceptance Criteria:**
```gherkin
Given a Put request with source file and destination
When the Put request is submitted
Then a new send transaction shall be created
And transaction ID shall be assigned
And transmission shall begin per configured parameters
And transaction status shall be queryable
```

### REQ-CFDP-011: Filestore Operations
**Description:** The system shall support CFDP filestore directives.

**Acceptance Criteria:**
```gherkin
Given filestore directive in metadata
When directive specifies "create file"
Then the file shall be created at destination

When directive specifies "delete file"
Then the file shall be removed from filestore

When directive specifies "rename file"
Then the file shall be renamed as specified

When directive specifies "append file"
Then data shall be appended to existing file

When directive specifies "replace file"
Then existing file shall be replaced
```

### REQ-CFDP-012: Transaction Monitoring
**Description:** The system shall provide transaction monitoring capabilities.

**Acceptance Criteria:**
```gherkin
Given active CFDP transactions
When transaction status is queried
Then transaction ID shall be returned
And transaction state shall be reported
And bytes transferred shall be reported
And estimated completion shall be available
And all active transactions shall be listable
```

### REQ-CFDP-013: Fault Handler Configuration
**Description:** The system shall support configurable fault handlers.

**Acceptance Criteria:**
```gherkin
Given CFDP fault handler configuration
When a fault condition is detected
Then the configured handler shall be invoked
And handler actions (ignore/cancel/suspend/abandon) shall be supported
And fault shall be logged
And user notification shall be provided
```

### REQ-CFDP-014: Transaction Suspension/Resumption
**Description:** The system shall support transaction suspension and resumption.

**Acceptance Criteria:**
```gherkin
Given an active CFDP transaction
When suspension is requested
Then the transaction shall enter suspended state
And no further PDUs shall be transmitted
And state shall be preserved

When resumption is requested
Then the transaction shall resume from saved state
And transmission shall continue
And timers shall be restarted appropriately
```

## Roadmap

### Phase 1: PDU Definitions (v0.1.0)
- [x] Define PDU header structures
- [x] Implement Metadata PDU encoding/decoding
- [x] Implement File Data PDU encoding/decoding
- [x] Implement EOF PDU encoding/decoding
- [x] Implement ACK PDU encoding/decoding
- [x] Implement NAK PDU encoding/decoding
- [x] Implement Finished PDU encoding/decoding

### Phase 2: Entity Foundation (v0.2.0)
- [x] Define CFDP entity configuration (MIB)
- [x] Implement entity initialization
- [x] Create transaction ID generation
- [x] Implement filestore abstraction
- [x] Add timer management

### Phase 3: Class 1 Support (v0.3.0)
- [x] Implement Class 1 sender
- [x] Implement Class 1 receiver
- [x] Add checksum calculation
- [x] Create transaction state machine
- [x] Add transaction completion handling

### Phase 4: Class 2 Support (v0.4.0)
- [x] Implement Class 2 sender
- [x] Implement Class 2 receiver
- [x] Add NAK generation and processing
- [x] Implement retransmission logic
- [x] Add ACK handling

### Phase 5: Transport Layer (v0.5.0)
- [x] Define transport abstraction
- [x] Implement TCP transport
- [x] Implement UDP transport
- [x] Add PDU framing
- [x] Create connection management

### Phase 6: Filestore & Operations (v0.6.0)
- [x] Implement filestore operations
- [x] Add Put request handling
- [x] Implement transaction monitoring
- [x] Add fault handling
- [x] Create user primitives API

### Phase 7: Advanced Features (v0.7.0)
- [x] Implement transaction suspension/resumption
- [x] Add proxy operations (optional)
- [x] Implement directory listing (optional)
- [x] Add keep-alive handling
- [x] Create transaction persistence

### Phase 8: Polish (v1.0.0)
- [x] Complete XML documentation
- [x] Add integration tests
- [x] Performance optimization
- [x] Add comprehensive logging
- [x] Release stable API
