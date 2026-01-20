# NCcsds.Core

Core library providing shared types, interfaces, and common utilities for the NCcsds CCSDS implementation.

## Requirements

### REQ-CORE-001: Common Data Types
**Description:** Provide common data types used across all CCSDS libraries.

### REQ-CORE-002: Binary Data Handling
**Description:** Provide efficient binary data handling utilities.

### REQ-CORE-003: Result Types
**Description:** Provide result types for error handling without exceptions.

### REQ-CORE-004: Logging Abstraction
**Description:** Provide logging abstraction compatible with Microsoft.Extensions.Logging.

### REQ-CORE-005: Configuration Abstractions
**Description:** Provide configuration abstractions for library settings.

## Roadmap

### Phase 1: Foundation (v0.1.0)
- [x] Define core identifier types (SpacecraftId, VirtualChannelId, ApId, etc.)
- [x] Implement BitBuffer for bit-level operations
- [x] Implement ByteBuffer for byte-level operations
- [x] Create Result<T, TError> type for error handling
- [x] Define common interfaces (IParser, IEncoder, etc.)

### Phase 2: Utilities (v0.2.0)
- [x] Implement CRC-16-CCITT calculator
- [x] Implement CRC-32 calculator
- [x] Add big-endian/little-endian conversion utilities
- [x] Create binary reader/writer extensions for CCSDS types
- [x] Implement pseudo-random sequence generator (for frame randomization)

### Phase 3: Abstractions (v0.3.0)
- [x] Define logging abstractions
- [x] Create configuration option classes
- [x] Implement validation framework for configurations
- [x] Add dependency injection extensions
- [x] Create common exception types

### Phase 4: Polish (v1.0.0)
- [x] Complete XML documentation
- [x] Add usage examples
- [x] Performance optimization
- [x] AOT/Trimming compatibility verification
- [x] Release stable API
