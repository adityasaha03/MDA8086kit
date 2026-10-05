# MDA8086 Virtual Kit Implementation Checklist

## Phase 1: Infrastructure (Completed)
- [x] PH1-01: Skeleton and infrastructure setup
- [x] PH1-02: Core primitives and test doubles (`PortData`, `IPortSource`, `IPortMonitor`)
- [x] PH1-03: `KitConfig` and INI parser
- [x] PH1-04: `BusPoller`
- [x] PH1-05: `EmuIoPortFile`

## Phase 2: Hardware Modules (In Progress)
- [x] **PH2-01: 7-segment LEDs logic (`LedMatrix.cs`)**
  - [x] Implement `LedMatrix` (`IPortMonitor`)
  - [x] Unit tests for multiplexing behavior
- [x] **PH2-02: LCD HD44780 Controller (`LcdController.cs`)**
  - [x] Implement command parsing, DDRAM addressing
  - [x] Handle 4-bit mode safely (or ignore if always 8-bit, check plan)
  - [x] Handle S1 burst mode/write-sequence via Port 30H
  - [x] Unit tests for characters and clearing
- [x] **PH2-03: Single LEDs (`LedIndicators.cs`)**
  - [x] Implement single LEDs (listen to port 0x0F usually)
  - [x] Unit tests for LEDs

## Phase 3: Dot Matrix (M3)
- [x] PH3-01: DotMatrixModel pin decode and orientation
- [x] PH3-02: Persistence integrator
- [x] PH3-03: CS1 wiring and DotMatrixControl
- [x] PH3-04: Sample programs and orientation probes
- [x] PH3-05: Degradation and tuning harness

## Phase 4: LCD (M4)
- [x] PH4-01: LCD 5x8 font table with provenance
- [x] PH4-02: Hd44780Lcd model
- [x] PH4-03: LCD controller wiring and status publishing
- [x] PH4-04: LcdControl
- [ ] PH4-05: Power-up state option
- [ ] PH4-06: Conditional LCD stream mitigation
- [x] PH4-07: Sample program lcd_hello.asm

## Phase 5: Reset (M5)
- [x] PH5-03: Reset button

## Phase 6: Hardening and release (M6)
- [ ] PH6-01: Settings persistence
- [ ] PH6-02: Theme, DPI, About
- [ ] PH6-03: Packaging
- [ ] PH6-04: Documentation
