# NCcsds.Encoding

Data encoding/decoding library supporting CCSDS data types, time formats, and packet processing.

## Roadmap

### Phase 1: Primitive Types (v0.1.0)
- [x] Implement integer encoder/decoder (arbitrary bit width)
- [x] Implement unsigned integer encoder/decoder
- [x] Implement IEEE 754 float encoder/decoder (32/64 bit)
- [x] Implement MIL-STD-1750A float support (optional)
- [x] Create bit buffer for sub-byte operations

### Phase 2: Complex Types (v0.2.0)
- [x] Implement enumeration encoder/decoder
- [x] Implement fixed-length string encoder/decoder
- [x] Implement variable-length string encoder/decoder
- [x] Implement octet stream encoder/decoder
- [x] Implement bit stream encoder/decoder

### Phase 3: CCSDS Time Codes (v0.3.0)
- [x] Implement CUC (Unsegmented Code) encoder/decoder
- [x] Implement CDS (Day Segmented) encoder/decoder
- [x] Implement CCS (Calendar Segmented) encoder/decoder
- [x] Add relative time encoding
- [x] Create time utilities (epoch conversion, etc.)

### Phase 4: Space Packets (v0.4.0)
- [x] Implement CCSDS Space Packet primary header
- [x] Add packet identification framework
- [x] Implement packet assembly from frames
- [x] Create packet extraction utilities

### Phase 5: PUS Support (v0.5.0)
- [x] Implement PUS-A header encoding/decoding
- [x] Implement PUS-C header encoding/decoding
- [x] Add service type identification
- [x] Create PUS packet builder

### Phase 6: Definition-Based Processing (v0.6.0)
- [x] Define packet definition schema (JSON/XML)
- [x] Implement definition parser
- [x] Create dynamic encoder from definition
- [x] Create dynamic decoder from definition
- [x] Add parameter extraction by name

### Phase 7: Polish (v1.0.0)
- [x] Complete XML documentation
- [x] Add comprehensive unit tests
- [x] Performance optimization
- [x] Source generator for definitions (optional)
- [x] Release stable API
