# NCcsds.TmTc

Telemetry and Telecommand frame processing library implementing TM, TC, and AOS frame handling with COP-1 support.

## Roadmap

### Phase 1: TM Frames (v0.1.0)
- [x] Define TM frame structure types
- [x] Implement TM frame parser
- [x] Implement TM frame builder
- [x] Add FECF (CRC-16-CCITT) calculation
- [x] Create frame validation

### Phase 2: AOS Frames (v0.2.0)
- [x] Define AOS frame structure types
- [x] Implement AOS frame parser
- [x] Implement AOS frame builder
- [x] Add insert zone handling
- [x] Add signaling field parsing

### Phase 3: TC Frames (v0.3.0)
- [x] Define TC frame structure types
- [x] Implement TC frame parser
- [x] Implement TC frame builder
- [x] Add segment assembly
- [x] Create TC validation

### Phase 4: Virtual Channel Processing (v0.4.0)
- [x] Implement VC demultiplexer for TM
- [x] Implement VC multiplexer for TC
- [x] Add sequence counting
- [x] Implement gap detection
- [x] Create VC statistics

### Phase 5: COP-1 (v0.5.0)
- [x] Implement FOP state machine
- [x] Implement FARM state machine
- [x] Add CLCW generation/parsing
- [x] Implement retransmission logic
- [x] Add AD/BD mode support

### Phase 6: Frame Operations (v0.6.0)
- [x] Implement frame randomization
- [x] Add Reed-Solomon verification
- [x] Implement ASM handling
- [x] Add frame synchronization utilities

### Phase 7: Polish (v1.0.0)
- [x] Complete XML documentation
- [x] Add comprehensive unit tests
- [x] Performance optimization (Span<T>, SIMD)
- [x] Add streaming APIs
- [x] Release stable API
