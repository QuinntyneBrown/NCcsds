# NCcsds.Sle

SLE (Space Link Extension) user services library implementing RAF, RCF, ROCF, and CLTU services.

## Requirements

### REQ-SLE-001: RAF Service Initialization
**Description:** The system shall support initialization of RAF (Return All Frames) service instances.

**Acceptance Criteria:**
```gherkin
Given a valid SLE service configuration with RAF parameters
When the user initializes a new RAF service instance
Then the service instance shall be created successfully
And the service shall be in UNBOUND state
And the service instance identifier shall be set correctly
```

### REQ-SLE-002: RAF Service Binding
**Description:** The system shall support binding to RAF service providers.

**Acceptance Criteria:**
```gherkin
Given an initialized RAF service instance in UNBOUND state
And valid provider credentials are configured
When the user requests to bind to the service provider
Then a BIND operation shall be sent to the provider
And upon successful response the service shall transition to READY state
And the bind return shall contain the provider version number
```

### REQ-SLE-003: RAF Data Transfer Start
**Description:** The system shall support starting RAF data transfer operations.

**Acceptance Criteria:**
```gherkin
Given a bound RAF service instance in READY state
And valid start/stop time parameters are specified
When the user requests to start data transfer
Then a START operation shall be sent to the provider
And upon successful response the service shall transition to ACTIVE state
And telemetry frames shall begin arriving
```

### REQ-SLE-004: RAF Frame Quality Filtering
**Description:** The system shall support filtering RAF frames by quality.

**Acceptance Criteria:**
```gherkin
Given an active RAF service instance receiving frames
When the user configures frame quality filter to "good frames only"
Then only frames with valid CRC shall be delivered
And frames with errors shall be discarded
And the frame count statistics shall reflect filtered frames
```

### REQ-SLE-005: RCF Service Virtual Channel Selection
**Description:** The system shall support selection of specific virtual channels for RCF service.

**Acceptance Criteria:**
```gherkin
Given an initialized RCF service instance
And a list of virtual channel identifiers is specified
When the user starts the RCF service
Then only frames from the specified virtual channels shall be delivered
And frames from other virtual channels shall be excluded
And the virtual channel ID shall be included in each delivered frame annotation
```

### REQ-SLE-006: RCF Master Channel Selection
**Description:** The system shall support master channel selection for RCF service.

**Acceptance Criteria:**
```gherkin
Given an initialized RCF service instance
When the user configures master channel mode
Then frames from all virtual channels on the master channel shall be delivered
And the spacecraft identifier shall match the configured value
And the transfer frame version number shall be validated
```

### REQ-SLE-007: ROCF Service OCF Extraction
**Description:** The system shall support extraction of Operational Control Fields via ROCF service.

**Acceptance Criteria:**
```gherkin
Given an active ROCF service instance
When telemetry frames containing OCF data are received
Then the OCF data shall be extracted from each frame
And the OCF shall be delivered to the user application
And the frame sequence number shall be preserved
```

### REQ-SLE-008: ROCF Control Word Type Selection
**Description:** The system shall support filtering by control word type in ROCF service.

**Acceptance Criteria:**
```gherkin
Given an initialized ROCF service instance
When the user specifies control word type filter (CLCW or Type-2)
Then only matching control word types shall be delivered
And non-matching control words shall be discarded
And delivery statistics shall reflect filtered count
```

### REQ-SLE-009: CLTU Upload Operation
**Description:** The system shall support uploading CLTUs to the service provider.

**Acceptance Criteria:**
```gherkin
Given a bound CLTU service instance in READY state
And a valid CLTU data buffer is prepared
When the user invokes the CLTU transfer operation
Then the CLTU shall be queued for transmission
And a CLTU identifier shall be assigned
And acknowledgment shall be received upon radiation
```

### REQ-SLE-010: CLTU Radiation Notification
**Description:** The system shall provide notification of CLTU radiation status.

**Acceptance Criteria:**
```gherkin
Given a CLTU has been uploaded and queued
When the CLTU is radiated by the ground station
Then an async notification shall be delivered to the user
And the notification shall include radiation start time
And the notification shall include radiation stop time
And the CLTU status shall indicate success or failure
```

### REQ-SLE-011: CLTU Production Status
**Description:** The system shall report CLTU production status.

**Acceptance Criteria:**
```gherkin
Given an active CLTU service instance
When the user queries production status
Then the current production status shall be returned
And the number of CLTUs received shall be reported
And the number of CLTUs processed shall be reported
And the number of CLTUs radiated shall be reported
```

### REQ-SLE-012: SHA-1 Credential Encoding
**Description:** The system shall support SHA-1 based credential encoding for SLE authentication.

**Acceptance Criteria:**
```gherkin
Given valid user credentials (username and password)
And SHA-1 authentication mode is configured
When an SLE operation requiring authentication is performed
Then the credentials shall be encoded using SHA-1 algorithm
And the encoded credentials shall be included in the PDU
And the provider shall successfully authenticate the user
```

### REQ-SLE-013: SHA-256 Credential Encoding
**Description:** The system shall support SHA-256 based credential encoding for SLE authentication.

**Acceptance Criteria:**
```gherkin
Given valid user credentials (username and password)
And SHA-256 authentication mode is configured
When an SLE operation requiring authentication is performed
Then the credentials shall be encoded using SHA-256 algorithm
And the encoded credentials shall be included in the PDU
And the provider shall successfully authenticate the user
```

### REQ-SLE-014: Multiple SLE Version Support
**Description:** The system shall support multiple versions of the SLE protocol.

**Acceptance Criteria:**
```gherkin
Given a service provider supporting SLE version 3, 4, or 5
When the user configures the desired SLE version
Then the system shall encode/decode PDUs according to that version
And version negotiation shall occur during bind operation
And incompatible versions shall be rejected with appropriate diagnostic
```

## Roadmap

### Phase 1: ASN.1 Foundation (v0.1.0)
- [x] Implement ASN.1 BER encoder/decoder
- [x] Define SLE PDU base types
- [x] Create SLE credentials encoding (SHA-1, SHA-256)
- [x] Implement time code utilities for SLE
- [x] Define service state machine base

### Phase 2: RAF Service (v0.2.0)
- [x] Implement RAF service user
- [x] Add RAF BIND/UNBIND operations
- [x] Implement RAF START/STOP operations
- [x] Add frame delivery handling
- [x] Implement RAF status reporting
- [x] Add frame quality filtering

### Phase 3: RCF Service (v0.3.0)
- [x] Implement RCF service user
- [x] Add virtual channel selection
- [x] Implement master channel mode
- [x] Add RCF-specific parameters
- [x] Implement RCF status reporting

### Phase 4: ROCF Service (v0.4.0)
- [x] Implement ROCF service user
- [x] Add OCF extraction
- [x] Implement control word type filtering
- [x] Add ROCF-specific parameters
- [x] Implement ROCF status reporting

### Phase 5: CLTU Service (v0.5.0)
- [x] Implement CLTU service user
- [x] Add CLTU upload operations
- [x] Implement radiation notifications
- [x] Add CLTU production status
- [x] Implement CLTU event handling

### Phase 6: Transport & Integration (v0.6.0)
- [x] Implement TCP transport layer
- [x] Add TLS support
- [x] Implement connection management
- [x] Add reconnection logic
- [x] Create service factory

### Phase 7: Polish (v1.0.0)
- [x] Complete XML documentation
- [x] Add integration tests with SLE simulator
- [x] Performance optimization
- [x] Add comprehensive logging
- [x] Release stable API
