# MDA-8086 Virtual Trainer Kit for emu8086 — Project Summary & PRD

| | |
|---|---|
| Version | 0.1 (draft for planning) |
| Date | 2 October 2026 |
| Owner | Aditya (AUST, Dhaka) |
| Status | Pre-implementation — **no code has been written** |
| Intended use | Source document from which a `plan.md` for a coding agent will be derived |

---

## 0. How to read this document

- Sections **7 (requirements)**, **9 (architecture)**, **11 (tests)** and **14 (milestones)** map directly onto plan tasks. IDs (`FR-*`, `NFR-*`, `T-*`, `R-*`, `Q-*`, `S*`) are stable references the plan can cite.
- Evidence tags used throughout:
  - **[Manual]** — taken from the AUST *CSE 3118 Microprocessors and Microcontrollers Lab* manual.
  - **[README]** — taken from emu8086's `_READ_ME.txt` for custom virtual devices.
  - **[io.cs]** — taken from the `io.cs` helper file supplied by the project owner.
  - **[Inferred]** — derived by analysis of the above; must be verified against the real kit.
  - **[Unknown]** — information we do not have yet; see §13.
- Priorities: **P0** = required for first usable release, **P1** = should have, **P2** = nice to have.

---

## 1. Executive summary

Build a **custom emu8086 virtual device** (a standalone Windows `.exe`) that imitates the user-visible hardware of the **MDA-8086 trainer kit** used in the AUST microprocessor lab: a 7-segment display, 4 LEDs, an 8×8 two-colour dot matrix, a 16×2 character LCD, a keypad and a reset button.

The device sits on the **same I/O port addresses as the real kit** (e.g. `OUT 19H, AL` drives the 7-segment display on both). Students can therefore write and debug 8086 assembly in emu8086 at home and see on screen what the real kit would show, with the **same source code** working in both places.

**Guiding principle:** *write once, run on emu8086 + virtual kit, and on the real kit.* Anything that forces source edits between the two is a defect or a documented limitation.

**What this is not:** it is not a full trainer-kit simulator. It does not emulate the CPU, the monitor ROM, the register display on the kit's LCD, or the kit's other peripherals (§3.2).

**Single most important technical risk:** emu8086 devices communicate through a plain file (`c:\emu8086.io`) that stores only the *latest* byte per port. Fast sequences of writes (LCD characters, dot-matrix column scans) may be partly invisible to a polling device. A feasibility spike (S1) is therefore the first piece of work, and the architecture is designed so the device logic can be reused in a standalone simulator if the spike fails (§9.4, "Plan B").

---

## 2. Background and problem statement

- The AUST lab course (CSE 3118) uses the **MDA-8086** kit (Midas Engineering "MDA-Win8086"). [Manual]
- Students currently prototype in **emu8086** with the third-party **I/O Emulation Kit** (by Dr. Mohammed Hawa, University of Jordan). That kit offers generic virtual devices (dot matrix, seven segment, ASCII LCD, LEDs, push buttons, keyboard, switches, thermometer, pressure gauge). [Manual]
- Its devices use **different port numbers and behaviour** from the real MDA-8086, so code must be rewritten before it can run on the kit. This costs lab time and causes errors.
- The Emulation Kit is free to download and its page offers source code, but **no open-source licence was found** in the zip, the repo that re-hosts it, or the lab manual. Modification and redistribution rights are therefore unclear, so this project is **written from scratch** (§15).
- emu8086 supports third-party devices written in any language, so the lowest-effort route to a faithful virtual kit is a purpose-built device rather than a new simulator. [README]

---

## 3. Goals, non-goals, success criteria

### 3.1 Goals

1. Provide six virtual components — 7-segment, 4 LEDs, 8×8 dot matrix, 16×2 LCD, keypad, reset — in one window, mapped to the MDA-8086's real port addresses.
2. Model the **8255A PPI** behaviour that the kit's lab programs rely on (control word, mode 0, port directions, latch behaviour), because all three visual outputs (7-segment, LEDs, dot matrix) are driven through 8255 ports.
3. Make the AUST **Session 5 reference programs** (7-segment 0–9, LED sequence, dot-matrix letter "A") run with the same visible result as on the real kit.
4. Keep device logic **UI-free and unit-testable**, and the port map **configurable**.
5. Ship with a short install/usage guide so a classmate can set it up in minutes.

### 3.2 Non-goals (explicitly out of scope)

- CPU emulation, assembler, editor or debugger (emu8086 already provides these).
- The kit's **monitor ROM** and its keypad/LCD workflow (RES → DA → type hex → STP → REG). A device sees port I/O only, not CPU registers, so the kit's register display (AX/BX/CX/DX on the LCD) is **not** reproducible; emu8086's own register window covers it.
- Peripherals not in the six requested components: stepper motor, ADC (ADC0804), DAC (DAC0800) and level meter, speaker, 8251 serial, 8253 timer, 8259 interrupt controller, system-bus LEDs, thermistor, extension connector.
- Cycle-accurate timing, electrical effects (switch bounce, analog behaviour).
- Non-Windows platforms (emu8086 itself is Windows-only).
- Becoming an official Midas product or implying endorsement.

### 3.3 Success criteria

| ID | Criterion | Target |
|---|---|---|
| SC-1 | Session 5 reference programs 1–3 (7-segment, LED, dot matrix 'A') produce the same visible result as on the real kit | Pass, with no source edits beyond what S2 identifies as unavoidable |
| SC-2 | All P0 functional requirements pass their tests (§11) | 100% |
| SC-3 | Write capture on static outputs (7-segment, LEDs) at emu8086's default run speed | No missed visible state changes (final target set after S1) |
| SC-4 | A classmate installs and runs a sample in under 5 minutes using only the README | Pass |
| SC-5 | Device never crashes on missing file, locked file or permission denial | Pass (shows actionable status message) |

---

## 4. Users and use cases

**Personas**

- **Builder (Aditya):** develops and maintains the device; wants a clean, testable code base.
- **Student (AUST CSE 3118):** practises lab programs at home without access to the kit; wants "what I see here is what I'll see in the lab".
- **Instructor / TA (secondary):** may use it to demo or check student code.

**Core user stories**

| ID | Story |
|---|---|
| US-1 | As a student, I run the 7-segment program in emu8086 and see digits 0–9 cycle on the virtual display exactly as the kit would show them. |
| US-2 | As a student, I run the LED program and see R1, G, Y, R2 light in turn. |
| US-3 | As a student, I run the dot-matrix program and see a stable green letter "A". |
| US-4 | As a student, I send text to the LCD with the kit's instruction/data ports and see it on a 16×2 display. |
| US-5 | As a student, I press keys on the virtual keypad and my program can read them (protocol pending Q-03). |
| US-6 | As a student, I press RES to clear all virtual hardware before re-running. |
| US-7 | As a builder, I can see a live log of port values to debug a program that behaves differently on the kit. |

---

## 5. Hardware reference — the MDA-8086 kit (source of truth)

### 5.1 Overview [Manual]

Intel 8086 CPU; 64 KB SRAM at `00000H–0FFFFH`; 64 KB monitor ROM at `F0000H–FFFFFH`; user range `10000H–EFFFFH`; 16×2 text LCD; keypad with 16 hex keys plus function keys; two 8255A PPIs; 8251 (serial) and 8253 (timer); 8259 interrupt controller; speaker; 2-colour 8×8 dot matrix; ADC0804; DAC0800; stepper-motor driver. (Clock speed is quoted differently by different sources — 14.7456 MHz in the lab manual, 4.9152 MHz in a reseller listing. Irrelevant here because timing is not emulated.)

### 5.2 I/O address map [Manual]

| Range | Device | Port | Function |
|---|---|---|---|
| 00H–07H | LCD | 00H | Instruction register (write) |
| | | 02H | Status register (read) |
| | | 04H | Data register |
| | Keyboard | 01H | Keyboard register (**read only**) |
| | | 01H | Keyboard flag (**write only**) |
| 08H–0FH | 8251 / 8253 | 08H, 0AH | 8251 data / instruction-status |
| | | 09H, 0BH, 0DH, 0FH | 8253 timers 0–2 / control |
| 10H–17H | 8259 / speaker | 10H, 12H, 11H | 8259 command, 8259 data, speaker |
| 18H–1FH | **8255A CS1** (dot matrix & ADC) | 18H | Port A |
| | | 1AH | Port B |
| | | 1CH | Port C |
| | | **1EH** | Control register |
| | **8255A CS2** (LED, 7-segment & stepper) | 19H | Port A |
| | | 1BH | Port B |
| | | 1DH | Port C |
| | | 1FH | Control register |
| 20H–2FH | I/O extension connector | | |
| 30H–FFH | User range | | |

**Notes**

- The manual's address table prints `1CH` as "C port control register" for CS1. The lab programs use `1EH` as CS1's control register (Experiment 4: control/A/B/C = `1E, 18, 1A, 1C`), so treat the table entry as a typo. [Manual, Inferred]
- The manual's table heads CS2 as "LED & stepping motor", but the Session 5 programs drive the **7-segment display from CS2 port A (19H)** and the **LEDs from CS2 port B (1BH)**. [Manual]
- All in-scope ports lie in `00H–1FH`, so a poller only needs to watch **32 bytes**. [Inferred]

### 5.3 8255A behaviour to model

Standard 8255A control word (port `1FH` for CS2, `1EH` for CS1):

| Bit | Meaning (when D7 = 1, "mode set") |
|---|---|
| D7 | 1 = mode-set word |
| D6–D5 | Group A mode (00 = mode 0, 01 = mode 1, 1x = mode 2) |
| D4 | Port A: 1 = input, 0 = output |
| D3 | Port C upper nibble: 1 = input, 0 = output |
| D2 | Group B mode (0 = mode 0, 1 = mode 1) |
| D1 | Port B: 1 = input, 0 = output |
| D0 | Port C lower nibble: 1 = input, 0 = output |

When D7 = 0 the word is a **bit set/reset (BSR)** command for port C: D3–D1 select the bit (0–7), D0 = 1 sets it, 0 resets it.

- The lab programs only ever send **`80H`** (mode 0, ports A, B and C all outputs). [Manual]
- **Power-up/reset:** all ports are inputs. **On every mode-set write, the output latches of ports A, B and C are cleared to `00H`.** [8255A datasheet behaviour; Inferred for the kit]
- **Faithfulness consequence:** because the 7-segment display is active-low, a freshly mode-set CS2 shows *all segments lit* until the program writes port A. The Session 5 LED program explicitly writes `0FFH` to `19H` after the control word ("segment address forcefully off") to avoid exactly this. [Manual, Inferred] The virtual kit should reproduce this naturally via the 8255 model.
- Modes 1 and 2 (handshake, bidirectional) are not used by the lab programs and are out of scope; the device should log a warning if a program selects them.

### 5.4 7-segment display

- Driven from **CS2 port A (`19H`)**, **active-low** (0 = lit, 1 = off). [Manual]
- Assumed single digit with decimal point. [Inferred — Q-06]
- Segment-to-bit mapping, derived from the manual's digit codes: **bit0 = a, bit1 = b, bit2 = c, bit3 = d, bit4 = e, bit5 = f, bit6 = g, bit7 = dp.** [Inferred; consistent with all ten codes]

| Digit | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 |
|---|---|---|---|---|---|---|---|---|---|---|
| Port A value | C0H | F9H | A4H | B0H | 99H | 92H | 82H | F8H | 80H | 90H |

- Reference program behaviour [Manual]: `OUT 1FH,80H`; then for each digit `OUT 19H,<code>` followed by a software delay (`MOV CX,0FFFFH` / `LOOP`); after 9 it jumps back to 0 and repeats forever.

### 5.5 LEDs (4)

- Driven from **CS2 port B (`1BH`)**, **active-high** (1 = lit). [Manual]
- Bit assignment: **bit0 = R1 (red), bit1 = G (green), bit2 = Y (yellow), bit3 = R2 (red)** → values `01H`, `02H`, `04H`, `08H`. [Manual]
- Bits 4–7 of port B are not described for LEDs (CS2 also serves the stepper driver); the device ignores them. [Inferred]
- Reference program behaviour [Manual]: `OUT 1FH,80H`; `OUT 19H,0FFH` (7-segment off); loop: `OUT 1BH,01H` → delay → `02H` → delay → `04H` → delay → `08H` → delay → repeat. One LED lit at a time.

### 5.6 8×8 two-colour dot matrix

- Driven from **CS1**: port A (`18H`) and port B (`1AH`) are the **red** and **green** LED lines, **active-low**; port C (`1CH`) selects the **scanned line**, one-hot, **active-high**. [Manual: "Port C high and Port A low glows red; Port C high and Port B low glows green"]
- The display is **multiplexed**: the program must keep scanning. A line/LED is lit when its port C bit is 1 **and** the corresponding A (red) or B (green) bit is 0. If both red and green are on, the LED appears orange/yellow. [Manual, Inferred]
- A jumper must be set on the real kit before running any dot-matrix program; the virtual kit assumes it is correct. [Manual]
- Reference program ("A") behaviour [Manual]:
  1. `OUT 1EH,80H` (mode 0, all outputs); `OUT 18H,0FFH` (red off).
  2. Loop over an 8-byte font table starting with `AH = 01H`: `OUT 1AH,<font byte>`; `OUT 1CH,AH`; call a short delay (`CX = 300` × 4 `NOP` + `LOOP`); advance table pointer; `ROL AH,1`; repeat until the one-hot bit rotates out (8 columns); restart.
  3. Font table (active-low): `FFH, C0H, B7H, 77H, 77H, B7H, C0H, FFH`.
- **Orientation [Inferred — Q-05]:** inverting the font gives `00, 3F, 48, 88, 88, 48, 3F, 00`, which forms an upright "A" if **port C bit 0 is the left-most scan line and port B bit 7 is the top row**. The manual's prose describes rows and columns the other way round; verify on the real kit and make the orientation configurable (FR-DM-04).
- Because the kit scans quickly, a faithful virtual display needs a **persistence-of-vision integrator** (§9.3), not a naive "show the last write".

### 5.7 16×2 character LCD

- Registers: **`00H` instruction (write)**, **`02H` status (read)**, **`04H` data**. [Manual]
- Assumed **HD44780-compatible** controller (standard for 16×2 modules; the manual refers to a "Text LCD Module, 16 characters × 2 lines"). [Inferred — Q-04]
- Behaviour to model (standard HD44780 instruction set):

| Instruction | Code | Effect |
|---|---|---|
| Clear display | `01H` | Clear DDRAM, cursor home, entry mode increment |
| Return home | `02H` | Cursor to address 0, undo shift |
| Entry mode set | `04H`–`07H` | I/D (increment/decrement), S (display shift) |
| Display on/off | `08H`–`0FH` | D (display), C (cursor), B (blink) |
| Cursor/display shift | `10H`–`1FH` | S/C, R/L |
| Function set | `20H`–`3FH` | DL (bus width), N (lines), F (font) |
| Set CGRAM address | `40H`–`7FH` | Custom character memory |
| Set DDRAM address | `80H`–`FFH` | Line 1 = `00H–0FH` visible, line 2 = `40H–4FH` visible |

- Status read: bit 7 = busy flag (BF), bits 6–0 = address counter. Default design: **BF always 0** (never busy); realistic timing is P2.
- Typical initialisation (`38H`, `0CH`, `06H`, `01H`) is expected in student programs. [Inferred]
- The **monitor ROM also uses this LCD** (AD/REG displays, banner). That behaviour is not emulated. [Manual]

### 5.8 Keypad

- Single port **`01H`**: reads return the **keyboard register**, writes set the **keyboard flag**. [Manual]
- Keys: **16 hexadecimal keys (0–F)** plus function keys **RES, STP, AD, GO, DA, MON, `:`, REG, `+`, `-`**. [Manual]
- The function keys are consumed by the **monitor** in the real kit's machine-code workflow. [Manual]
- **[Unknown — Q-03]:** how a *user program* reads keys (key code encoding, flag handshake, whether it must poll `01H`). The MDA-Win8086 manual has keyboard experiments (e.g. "display the pressed key on LCD") that should answer this.

### 5.9 Reset

- **RES** is the kit's system reset. [Manual] In the virtual kit it resets the **virtual peripherals** (8255 state, LCD state, display latches). It cannot restart emu8086's CPU (see §6.3).

### 5.10 How labs use the real kit [Manual]

| Flow | Steps | Used by |
|---|---|---|
| **Machine-code mode (keypad)** | Assemble in emu8086 to get hex → on kit: `RES`, `DA`, type hex bytes, `+` for next address, `STP` to execute, `REG` to view AX/BX/CX/DX on the LCD. Programs end with `INT 3`. Example programs sit around address `0404`. | Session 1 (arithmetic; no peripherals) |
| **PC mode (serial download)** | Write MASM-style source (`SEGMENT PARA PUBLIC 'CODE'`, `ASSUME CS:…`, `ORG 1000H`, `END START`) → `MASM` → `LOD186` → `COMM`; kit in PC mode, `RES`, type `L`, `F3` + `.ABS` file; kit mode, `RES`, `AD`, `GO`. | Session 5 (7-segment, LEDs, dot matrix) |

The virtual kit targets **PC-mode, peripheral-driving programs** (Session 5 style) and any port-level I/O in machine-code-mode programs.

---

## 6. emu8086 integration facts

### 6.1 Custom device mechanism [README]

- A device is a **standalone `.exe`** placed in emu8086's `devices` folder (README says `c:\emu8086\devices`; verify the real install path — S3). emu8086 lists everything there in its **Virtual Devices** menu **at startup**.
- A device is also **activated automatically when its file name appears in a comment or string** in the program being run.
- Communication is through **`c:\emu8086.io`**: **port N = byte N of that file**, valid ports `0–65535`. A second file, **`c:\emu8086.hw`**, is also mentioned; its purpose is not explained.
- Both files need **read/write permission** in the root of drive C:.
- Seven sample devices ship with emu8086 (Simple, LED_Display, Thermometer, Printer, Robot, Stepper_motor, Traffic_Lights), with source in `c:\emu8086\DEVELOPER`. The README names `Thermometer.exe` twice, so there are seven distinct devices.
- Helper files exist for VB6, VB.NET, C/C++, C#, Delphi.

### 6.2 Analysis of the supplied `io.cs` [io.cs]

| # | Finding | Consequence |
|---|---|---|
| 1 | API is `READ_IO_BYTE`, `READ_IO_WORD`, `WRITE_IO_BYTE`, `WRITE_IO_WORD`. Every call **opens the file, seeks, reads/writes one byte, closes it**, using `FileShare.ReadWrite`. | Works concurrently with emu8086, but is slow for polling many ports. |
| 2 | **No change notification, no queue, no sequence counter.** A write overwrites the previous value at that port. | A polling device sees only the value present at poll time. **Writes faster than the poll interval are lost.** (Risk R-01) |
| 3 | **One byte per port, shared by both directions.** | CPU `OUT` and device-supplied `IN` values alias each other. Port `01H` (key read / flag write) is directly affected. (Risk R-10) |
| 4 | **Bug:** `private const string sIO_FILE = "C:\emu8086.io";` contains an unescaped backslash. | On compilers before C# 13 this is **compile error CS1009**. On C# 13+ `\e` is the ESC escape, so it compiles but points at a **wrong path** (ESC + `mu8086.io`) and fails at run time. **Fix first:** `@"C:\emu8086.io"`. |
| 5 | **No error handling.** `FileMode.Open` throws if emu8086 has not created the file; contention/permission errors throw `IOException`/`UnauthorizedAccessException`. | The device must wrap all access and show a status message (FR-SYS-04). |
| 6 | `c:\emu8086.hw` (mentioned in the README) is **not used** by `io.cs`. | Its role is unknown — study the shipped sample devices (S1). |
| 7 | Word helpers are irrelevant to the kit, which uses byte ports. | Ignore. |

**Decision:** keep `io.cs` (with its original header and credit to deTrox Yang) as a reference, but implement a small **`PortFile`** wrapper of our own that holds **one** `FileStream` open with `FileShare.ReadWrite`, **bulk-reads bytes `00H–1FH` in a single call** per poll, and handles all errors.

### 6.3 Implications

1. The device can only **observe** values in the file. It cannot see CPU registers, memory, flags or the instruction pointer, and it cannot reset or pause the CPU. The reset button therefore resets only virtual hardware.
2. Programs that scan displays or stream LCD characters stress the polling model. This is validated in S1 before any UI work.
3. emu8086's run speed (step delay) changes how fast `OUT` sequences occur. All timing-sensitive tests must be repeated at several speeds.
4. Ports `00H–1FH` may overlap those used by emu8086's built-in sample devices. Use **one device at a time** until S3 confirms the overlaps.
5. The lab code is MASM-style; emu8086's assembler dialect may need small header edits (S2).
6. emu8086 is old software; the README asks for "the most recent version". Record the exact emu8086 version used for development and testing.

---

## 7. Functional requirements

Priority: **P0** first usable release · **P1** should have · **P2** nice to have.

### 7.1 System and integration

| ID | Requirement | Pri |
|---|---|---|
| FR-SYS-01 | Single standalone `.exe` (working name `MDA8086_Kit.exe`) that appears in emu8086's Virtual Devices menu when placed in the `devices` folder. | P0 |
| FR-SYS-02 | Auto-activation when the exe file name appears in a program comment (verify in S3). | P1 |
| FR-SYS-03 | Only one instance runs at a time. | P1 |
| FR-SYS-04 | Visible connection status: *Connected*, *Waiting for emu8086*, *Cannot open c:\emu8086.io (permission/lock)*, each with a short remedy. Never crash on I/O errors; retry automatically. | P0 |
| FR-SYS-05 | Poll ports `00H–1FH` at a configurable interval (default 1 ms) using one bulk read per poll. | P0 |
| FR-SYS-06 | Port map (base addresses of LCD, keypad, both 8255s) loaded from an external config file; defaults equal the real kit. | P1 |
| FR-SYS-07 | Port monitor panel: live hex view of `00H–1FH`, timestamped log of observed changes, counters, export to CSV. | P1 |
| FR-SYS-08 | Reset action returns every virtual peripheral to its power-up state. | P0 |
| FR-SYS-09 | Persist window position and settings between runs. | P2 |
| FR-SYS-10 | Clean shutdown: close file handle, never truncate or resize `c:\emu8086.io`. | P0 |

### 7.2 8255A PPI (two instances)

| ID | Requirement | Pri |
|---|---|---|
| FR-PPI-01 | Two instances: **CS1** (A=`18H`, B=`1AH`, C=`1CH`, CTRL=`1EH`) and **CS2** (A=`19H`, B=`1BH`, C=`1DH`, CTRL=`1FH`). | P0 |
| FR-PPI-02 | Mode-set control word: mode 0, independent direction for A, B, C-upper, C-lower. | P0 |
| FR-PPI-03 | On mode-set, clear output latches A, B, C to `00H`. | P0 |
| FR-PPI-04 | Power-up/reset state: all ports input, latches `00H`. | P0 |
| FR-PPI-05 | Bit set/reset (BSR) commands on port C. | P1 |
| FR-PPI-06 | Pins of a port are driven only while that port is configured as output. | P0 |
| FR-PPI-07 | Log a warning in the port monitor if a program selects mode 1 or 2 (unsupported). | P1 |
| FR-PPI-08 | Input-configured ports return device-supplied pin values (needed only if a program reads them). | P2 |

### 7.3 Seven-segment display

| ID | Requirement | Pri |
|---|---|---|
| FR-7S-01 | Render segments a–g and dp from CS2 port A pins, **active-low**, bit0=a … bit6=g, bit7=dp. | P0 |
| FR-7S-02 | All ten reference digit codes (§5.4) render the correct digit. | P0 |
| FR-7S-03 | Display reflects the 8255 model (e.g. all segments lit after mode-set until port A is written; blank/undriven before any control word — final behaviour per Q-07). | P0 |
| FR-7S-04 | Optional tooltip showing port/bit for each segment. | P2 |

### 7.4 LEDs

| ID | Requirement | Pri |
|---|---|---|
| FR-LED-01 | Four LEDs on CS2 port B bits 0–3: R1 (red), G (green), Y (yellow), R2 (red); **active-high**. | P0 |
| FR-LED-02 | Port B bits 4–7 are ignored by the LED bank. | P0 |
| FR-LED-03 | Labels under each LED; colours distinguishable without relying on colour alone (text label). | P1 |

### 7.5 Dot matrix

| ID | Requirement | Pri |
|---|---|---|
| FR-DM-01 | 8×8 grid of two-colour (red/green) LEDs. | P0 |
| FR-DM-02 | LED lit when its CS1 port C line is 1 **and** the matching A (red) or B (green) bit is 0; both → orange/yellow. | P0 |
| FR-DM-03 | **Persistence integrator:** accumulate on-time per LED over a configurable window (default ≈ 20 ms) and display brightness proportional to duty cycle. | P0 |
| FR-DM-04 | Orientation configurable (default: C bit 0 = left-most column, B bit 7 = top row — Q-05); options to flip/rotate. | P1 |
| FR-DM-05 | Steady-state (non-scanned) patterns render correctly, e.g. all lines high with a data bit low lights a full row/column. | P0 |
| FR-DM-06 | Poll rate and window must keep the reference "A" program stable (no visible flicker, no ghost columns). | P0 |

### 7.6 LCD (16×2)

| ID | Requirement | Pri |
|---|---|---|
| FR-LCD-01 | 16×2 display of 5×8 character cells backed by 80-byte DDRAM (line 1 at `00H`, line 2 at `40H`). | P0 |
| FR-LCD-02 | Decode and execute the instruction set in §5.7 on port `00H`. | P0 |
| FR-LCD-03 | Data register (`04H`) writes go to DDRAM or CGRAM with address increment/decrement per entry mode. | P0 |
| FR-LCD-04 | Status register (`02H`) returns BF = 0 and the current address counter. | P0 |
| FR-LCD-05 | Printable ASCII `20H–7EH` rendered; other codes render a blank or placeholder box. | P0 |
| FR-LCD-06 | Display on/off, cursor and blink states. | P1 |
| FR-LCD-07 | Cursor and display shift. | P1 |
| FR-LCD-08 | Eight CGRAM custom characters. | P1 |
| FR-LCD-09 | Optional realistic busy timing (clear ≈ 1.5 ms, others ≈ 40 µs, wall-clock based). | P2 |
| FR-LCD-10 | Power-up state per HD44780 (display off, cleared, 8-bit, entry mode increment); optional monitor-style banner is **not** required. | P1 |

### 7.7 Keypad

| ID | Requirement | Pri |
|---|---|---|
| FR-KP-01 | On-screen keys 0–F, mouse-clickable, plus PC keyboard mapping (0–9, A–F). | P0 |
| FR-KP-02 | Expose the pressed key on port `01H` per the kit's protocol. **Blocked by Q-03.** | P0 |
| FR-KP-03 | Function keys (RES, STP, AD, GO, DA, MON, `:`, REG, `+`, `-`) displayed; **RES works**, others inert and greyed with a tooltip explaining why. | P1 |
| FR-KP-04 | Arbitrate port `01H` aliasing: device remembers the last byte it wrote and treats a different value at the next poll as a CPU write (keyboard flag). Known limitation if the CPU writes the same value. | P1 |

### 7.8 Reset button

| ID | Requirement | Pri |
|---|---|---|
| FR-RST-01 | RES button resets all virtual peripherals (both 8255s, LCD, keypad latch, dot-matrix integrator). | P0 |
| FR-RST-02 | UI states clearly that RES does **not** restart the emulated CPU and that emu8086's own reload/reset must be used. | P1 |
| FR-RST-03 | Keyboard shortcut for reset. | P2 |

### 7.9 User interface

| ID | Requirement | Pri |
|---|---|---|
| FR-UI-01 | Single window laid out like the physical kit: LCD on top, 7-segment, 4 LEDs, dot matrix, keypad, RES. | P0 |
| FR-UI-02 | Custom-drawn, double-buffered controls; refresh ≥ 30 fps without flicker. | P0 |
| FR-UI-03 | DPI-aware and resizable with proportional scaling. | P1 |
| FR-UI-04 | Light/dark theme. | P2 |
| FR-UI-05 | "Always on top" toggle so the window can sit beside emu8086. | P2 |
| FR-UI-06 | About dialog: version, "unofficial — not affiliated with Midas Engineering or emu8086", credits. | P1 |

---

## 8. Non-functional requirements

| ID | Requirement |
|---|---|
| NFR-01 | Runs on Windows 10 and 11 without installing a runtime (target **.NET Framework 4.8 WinForms**, which ships with Windows). |
| NFR-02 | Starts in under 2 seconds. |
| NFR-03 | Polling thread uses under 5% of one CPU core when idle; UI thread never blocks on file I/O. |
| NFR-04 | Memory under 100 MB; no leaks in a 30-minute soak. |
| NFR-05 | Resilient: survives missing/locked/permission-denied files, emu8086 restarts, and partial reads. |
| NFR-06 | Deterministic, UI-free device models with unit tests; core logic has no dependency on WinForms or the file system. |
| NFR-07 | No administrator rights needed by the device itself (the README's file-permission requirement is documented for the user). |
| NFR-08 | Single `.exe` deliverable (plus optional config file) so installation is "copy into the devices folder". |
| NFR-09 | Accessibility: all state also communicated by labels/tooltips, not colour alone. |
| NFR-10 | Source code commented where hardware behaviour is non-obvious, with references back to this document's section numbers. |

---

## 9. Architecture proposal

### 9.1 Technology decision

**C# on .NET Framework 4.8, WinForms.** Reasons: the supplied helper is C#; the framework is preinstalled on Windows 10/11 so the exe stays small and dependency-free; WinForms custom drawing is sufficient for these widgets. Structure the solution as a **UI-free core library** plus a thin WinForms shell.

### 9.2 Layers and data flow

```
 emu8086 CPU  --OUT/IN-->  c:\emu8086.io  (byte N = port N)
                                |
                       [PortFile]  one FileStream, bulk read 00H-1FH, safe writes
                                |
                       [BusPoller]  dedicated thread, ~1 ms period,
                                |    diff vs shadow copy -> PortChanged(port, value, t)
                                |
                       [AddressDecoder]  config-driven port map
              ______________|____________________________
             |        |         |            |           |
        [Ppi8255 CS1] [Ppi8255 CS2] [Hd44780Lcd] [Keypad] [Reset]
             |             |
        [DotMatrix]   [SevenSeg] [LedBank]       (views of PPI output pins)
                                |
                  thread-safe state snapshot
                                |
                 [WinForms UI]  60 Hz repaint timer + custom controls
```

- **PortFile:** single `FileStream` with `FileShare.ReadWrite`; `ReadRange(0, 32)`; `WriteByte(port, value)` for device-supplied inputs; catches and reports I/O errors; reopens when emu8086 restarts.
- **BusPoller:** high-resolution loop. `Thread.Sleep(1)` really sleeps ~15 ms on default Windows timer resolution, so either call `timeBeginPeriod(1)` (winmm) or use a `Stopwatch` spin/sleep hybrid. Compares each poll against a shadow copy and raises ordered change events with a timestamp.
- **Devices** implement a small interface, e.g. `IPortDevice { bool Handles(int port); void OnCpuWrite(int port, byte value, long ticks); void Tick(long ticks); }`. Models never touch UI or files.
- **Views** (7-segment, LEDs, dot matrix) read **pin state** from the PPI models, not raw ports, so 8255 semantics (direction, latch clear on mode-set) apply uniformly.
- **UI:** a repaint timer pulls an immutable snapshot of model state; custom controls draw it with GDI+ (double-buffered).

### 9.3 Dot-matrix persistence integrator

For each poll tick of duration Δt, for every LED (line *k*, data bit *j*, colour *c*): if lit, add Δt to its on-time accumulator. Every refresh window W (default ≈ 20 ms, configurable), brightness = on-time / W, then reset accumulators. This turns a scanned display into a stable image and also renders steady (unscanned) patterns correctly. Tune W and the poll interval against the reference "A" program at several emu8086 speeds.

### 9.4 Key design decisions

| ID | Decision | Rationale / trigger |
|---|---|---|
| D1 | Use own `PortFile` wrapper; keep `io.cs` as credited reference. | `io.cs` is slow, bug-ridden path constant, no error handling (§6.2). |
| D2 | .NET Framework 4.8 WinForms. | Zero-install on Windows; matches `io.cs`. |
| D3 | Models are UI-free class library. | Unit-testable and reusable if the architecture changes. |
| D4 | Port map via config file. | Adaptable to other kits/boards; avoids magic numbers. |
| D5 | **Plan B trigger:** if S1 shows write loss on LCD streams or dot-matrix scans that cannot be fixed by polling faster or slowing emu8086, pivot to a **standalone simulator** with its own 8086 core and reuse the models unchanged. | Models stay valid regardless of how port writes arrive (file polling vs direct CPU callbacks). |
| D6 | Device-supplied inputs are written back to the same file byte. | Only mechanism the README/`io.cs` offers; aliasing handled per FR-KP-04. |

### 9.5 Suggested repository layout

```
/src/Mda8086Kit.Core/          device models (Ppi8255, Hd44780Lcd, DotMatrix, ...), no UI
/src/Mda8086Kit/               WinForms shell, PortFile, BusPoller, controls
/tests/Mda8086Kit.Core.Tests/  unit tests (xUnit/NUnit/MSTest) + fake bus
/samples/asm/                  reference .asm programs + expected-result notes
/docs/                         this PRD, user guide, port map, test log
/third_party/io.cs             original helper with header/credit intact
```

---

## 10. UI layout specification

Single window, kit-faithful arrangement (proportions approximate; refine after S4 photos of the real kit):

```
+--------------------------------------------------------------+
| [status: Connected]                       [Port monitor v] [⚙]|
|  +----------------------------------+                         |
|  |  16 x 2 LCD (green, 5x8 cells)   |      8 x 8 dot matrix   |
|  |                                  |      (red/green LEDs)   |
|  +----------------------------------+                         |
|   7-segment [ 8. ]      LEDs: (R1) (G) (Y) (R2)               |
|                                                               |
|   Keypad:  [C][D][E][F]   function: [RES][STP][AD][GO][DA]    |
|            [8][9][A][B]               [MON][ : ][REG][+][-]   |
|            [4][5][6][7]                                       |
|            [0][1][2][3]                                       |
+--------------------------------------------------------------+
```

- Status bar always shows connection state and the emu8086 file path in use.
- Port monitor is a collapsible panel (FR-SYS-07): 32 byte cells with change highlighting, plus a scrolling log.
- Function keys other than RES are visibly inert with a tooltip: "Handled by the kit's monitor ROM; not emulated."
- Hover tooltips show port/bit mapping (e.g. LED R1 → port 1BH bit 0) as a teaching aid (P2).

---

## 11. Test and acceptance plan

### 11.1 Unit tests (core library, no UI, no file system)

| ID | Scope | Cases |
|---|---|---|
| T-01 | `Ppi8255` | Control-word decode table (all direction combinations); mode-set clears latches; BSR set/reset each bit; power-up state; writes to an input-configured port do not drive pins; unsupported modes raise a warning. |
| T-02 | Seven-segment decode | Each code in §5.4 → expected lit segment set; `FFH` → dark; `00H` → all eight lit including dp; bit mapping a–g/dp. |
| T-03 | LED bank | `01H/02H/04H/08H` → single LED; `0FH` → all four; bits 4–7 ignored. |
| T-04 | Dot matrix | Feed the §11.3 "A" scan timeline → integrator output equals the expected grid; steady-state patterns; red only, green only, both = yellow; window/Δt arithmetic. |
| T-05 | `Hd44780Lcd` | Init `38H,0CH,06H,01H`; write "HELLO" at `00H` and "WORLD" at `40H`; clear; home; entry mode (inc/dec, shift); set DDRAM address across line boundary; display/cursor/blink flags; CGRAM write/read; status register returns BF=0 + address counter. |
| T-06 | `BusPoller` with fake `PortFile` | Change detection; ordering; no event when unchanged; recovery after injected `IOException`; reopen after file recreation. |
| T-07 | Address decoder/config | Default map equals §5.2; overriding a base address moves the device; overlapping ranges rejected. |

### 11.2 Integration tests (manual, inside emu8086)

Repeat each at **at least three emu8086 run speeds**.

| ID | Program | Expected result |
|---|---|---|
| IT-1 | Session 5 Exp. 2 (7-segment 0–9) | Digits 0→9 cycle repeatedly, each visible for a similar time, then wrap to 0. No stray segments. |
| IT-2 | Session 5 Exp. 3 (LED sequence) | One LED at a time: R1 → G → Y → R2 → repeat. 7-segment stays dark (`0FFH` written to `19H`). |
| IT-3 | Session 5 Exp. 4 (dot matrix "A") | Stable green "A" (grid below), no flicker or ghost columns. |
| IT-4 | New LCD demo (to be authored) | Init `38H,0CH,06H,01H`; line 1 shows `HELLO`, line 2 shows `MDA-8086`. |
| IT-5 | New keypad echo (to be authored) | Pressed key shown on LCD/LEDs. **Blocked by Q-03.** |
| IT-6 | Reset | During IT-1, press RES: all virtual hardware clears; re-run starts clean. |
| IT-7 | Fault injection | Start device before emu8086; deny file permission; delete/recreate `c:\emu8086.io`; restart emu8086. Device shows status and recovers without restarting. |
| IT-8 | Soak | 30 minutes running IT-1 and IT-3 alternately: stable CPU, memory and frame rate. |

### 11.3 Fixtures derived from the reference programs [Manual]

**Port-write timelines** (delays omitted):

- *7-segment:* `1FH←80H`; then repeating `19H←C0H, F9H, A4H, B0H, 99H, 92H, 82H, F8H, 80H, 90H`.
- *LEDs:* `1FH←80H`; `19H←FFH`; then repeating `1BH←01H, 02H, 04H, 08H`.
- *Dot matrix:* `1EH←80H`; `18H←FFH`; then repeating for i = 0…7: `1AH←font[i]`, `1CH←(1 << i)` with `font = FF, C0, B7, 77, 77, B7, C0, FF`.

**Expected "A" grid** (top row = port B bit 7, left column = port C bit 0; `#` = green lit) **[Inferred orientation — Q-05]:**

```
row7  ...##...
row6  ..#..#..
row5  .#....#.
row4  .#....#.
row3  .######.
row2  .#....#.
row1  .#....#.
row0  .#....#.
      01234567   <- port C bit (column)
```

### 11.4 Definition of "pass"

A requirement passes when its unit test passes and, where an integration test applies, the observed behaviour matches in at least three emu8086 speeds. Where the real kit disagrees with this document, **the real kit wins** and the document is updated (§13).

---

## 12. Risks and mitigations

| ID | Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|---|
| R-01 | File holds only the latest byte per port; fast writes (LCD streams, matrix scans) are lost. | High | High | Spike S1 first; poll at ~1 ms with `timeBeginPeriod(1)`; test at several emu8086 speeds; study shipped sample devices (Printer) for stream handling; **Plan B (D5)**. |
| R-02 | emu8086 rejects the lab's MASM-style source. | Medium | Medium | Spike S2; document minimal header edits; provide emu8086-ready copies of reference programs in `/samples/asm`. |
| R-03 | Keypad user-program protocol unknown. | High | Medium | Obtain the MDA-Win8086 manual keyboard experiment; write a tiny probe program for the real kit (S4/S5); ship keypad last (M5). |
| R-04 | Dot-matrix orientation/timing wrong. | Medium | Medium | Compare with real kit (S4); orientation options (FR-DM-04); tune integrator window. |
| R-05 | `c:\emu8086.io`/`.hw` permission problems on Windows 10/11. | High | Medium | Clear status message (FR-SYS-04); README section; test with and without admin (S3). |
| R-06 | `io.cs` path constant bug (`"C:\emu8086.io"`). | Certain | High | Use verbatim string `@"C:\emu8086.io"`; own `PortFile` wrapper (D1). |
| R-07 | Licensing: Emulation Kit has no explicit open-source licence. | Certain | Medium | Write from scratch; do not copy its code or assets; email the author if reuse is ever desired (§15). |
| R-08 | emu8086 age/compatibility (Windows 11, antivirus, high DPI). | Medium | Medium | Record versions tested; keep device self-contained; test on a clean machine. |
| R-09 | Port overlap with emu8086's built-in devices. | Medium | Low | Use one device at a time; verify overlaps in S3; document. |
| R-10 | Port `01H` aliasing (key read vs flag write share one byte). | High | Medium | FR-KP-04 arbitration; document residual limitation. |
| R-11 | LCD character ROM/font IP. | Low | Low | Author own 5×8 bitmap table or use a clearly licensed one; record provenance. |
| R-12 | Virtual timing differs from real kit (emu8086 speed vs 8086 at kit clock), causing flicker or blur. | Medium | Medium | Integrator window configurable; speed-matrix testing (§11.2). |
| R-13 | Scope creep into full simulator. | Medium | Medium | Hold the non-goals in §3.2; Plan B only on D5 trigger. |

---

## 13. Open questions and assumptions

### 13.1 Open questions

| ID | Question | How to resolve | Blocks |
|---|---|---|---|
| Q-01 | How does emu8086 deliver successive `OUT`s through the file, and what is `c:\emu8086.hw` for? | S1: read `Printer`/`Simple` sources in `c:\emu8086\DEVELOPER`; empirical port-logger test. | Architecture (D5), FR-LCD, FR-DM |
| Q-02 | Does emu8086 assemble the lab programs (`SEGMENT PARA PUBLIC 'CODE'`, `ASSUME`, `ORG 1000H`) unchanged? What edits are needed? | S2. | SC-1 wording |
| Q-03 | How does a user program read the keypad via port `01H` (code mapping, flag handshake)? | MDA-Win8086 manual keyboard experiment; probe program on real kit. | FR-KP-02, IT-5 |
| Q-04 | Is the LCD HD44780-compatible on this kit? Busy-flag behaviour, data-register reads, character ROM variant, power-up contents? | MDA-Win8086 manual LCD experiment; real-kit probe. | FR-LCD-* details |
| Q-05 | Dot-matrix orientation (which port is row vs column; left/top origin) and what the jumper changes. | Real-kit test with the "A" program; manual figures. | FR-DM-04, expected grid |
| Q-06 | 7-segment: one digit or more? Is dp wired? What is connected to CS2 port C? | Real kit; schematic. | FR-7S-01 |
| Q-07 | What do the 7-segment, LEDs and matrix show **before** any control word is written (ports are inputs)? | Real kit after RES. | FR-7S-03 |
| Q-08 | Exact `devices` folder path, menu listing and auto-activation by file name in comments; file-permission needs. | S3. | FR-SYS-01/02 |
| Q-09 | Which ports do emu8086's built-in devices use, and do they overlap `00H–1FH`? | S3. | R-09 |
| Q-10 | emu8086 version and Windows 11 behaviour (high DPI, UAC). | S3. | NFR-01 |
| Q-11 | May the finished tool be shared with classmates/department? Preferred name? | Ask the course instructor. | Release |
| Q-12 | Do lab programs ever *read* the 8255 ports or LCD data register? | Review remaining lab programs/exam tasks. | FR-PPI-08 |

### 13.2 Working assumptions (to be corrected as questions close)

- A-1: Windows 10/11 with emu8086 installed and working.
- A-2: One 7-segment digit with dp on bit 7.
- A-3: LCD is HD44780-compatible; busy flag always reads 0.
- A-4: Dot-matrix orientation as in §5.6.
- A-5: Student programs resemble the Session 5 reference programs.
- A-6: Timing is not emulated; only port-visible behaviour matters.

---

## 14. Milestones

### 14.1 M0 — Feasibility spikes (do first; time-boxed)

| Spike | Work | Exit criterion |
|---|---|---|
| **S1** Write-capture | Minimal "PortLogger" device (WinForms or console) polling `00H–1FH` at 1 ms with `timeBeginPeriod(1)` and logging every change with timestamps. Test in emu8086 at several speeds: (a) 8 consecutive `OUT 04H,AL` with distinct bytes and no delay, (b) the same with a short delay, (c) the dot-matrix "A" loop, (d) the LED loop. Read the shipped sample device sources to learn stream handling and the `emu8086.hw` role. | Measured % of writes observed per scenario; **written go/no-go on Plan B (D5)**. |
| **S2** Syntax | Assemble the three Session 5 programs and the Session 1 programs in emu8086 unchanged; record errors and required edits; check `ORG`, segment directives, `INT 3`, `OUT` with immediate ports below `100H`. | List of required edits (or "none"); emu8086-ready copies saved to `/samples/asm`. |
| **S3** Environment | Confirm `devices` folder path, menu listing, auto-activation by file name in a comment, `c:\emu8086.io` creation/size, permission behaviour (with/without admin), built-in device port overlaps, emu8086 version. | Documented install procedure and gotchas. |
| **S4** Real-kit reference | Photograph/record each reference program on the real kit: 7-segment initial state after RES, dot-matrix orientation, LCD content. Optional probe programs for keypad (read `01H`, echo to LEDs). | Answers or narrowed options for Q-03, Q-05, Q-06, Q-07. |
| **S5** Documents | Obtain the MDA-Win8086 user manual (keyboard and LCD experiments, schematics, jumper figures). | Q-03/Q-04 resolved or reduced. |

### 14.2 Delivery milestones

| ID | Milestone | Scope | Exit criteria |
|---|---|---|---|
| M1 | Skeleton | Solution layout; `PortFile`; `BusPoller`; address decoder + config; WinForms shell with status bar and port monitor; appears in emu8086 Virtual Devices menu. | FR-SYS-01/04/05/07/10 pass; T-06, T-07 pass; device survives IT-7. |
| M2 | 8255 + 7-segment + LEDs | `Ppi8255` ×2; seven-segment and LED views. | T-01/02/03 pass; IT-1 and IT-2 pass (SC-1 partial). |
| M3 | Dot matrix | Matrix model, integrator, orientation options, view. | T-04 passes; IT-3 passes at three speeds (SC-1 complete). |
| M4 | LCD | `Hd44780Lcd` model and view; CGRAM; shift; cursor. | T-05 passes; IT-4 passes. |
| M5 | Keypad + reset | Keypad view and port `01H` protocol (needs Q-03); RES behaviour; aliasing arbitration. | FR-KP-*, FR-RST-* pass; IT-5, IT-6 pass. |
| M6 | Hardening and release | Soak, DPI, themes, about box, README/user guide, installer-free zip, tagged release. | IT-7, IT-8 pass; SC-2 to SC-5 met; classmate dry-run succeeds. |

**Dependencies:** S1 gates M1's final design; S2 gates the wording of SC-1; Q-03 gates M5; Q-05 can be closed during M3 via S4.

---

## 15. Legal, licensing and attribution

- **Emulation Kit:** no open-source licence was found in the zip, in the GitHub repo that re-hosts it, or in the lab manual. Its source is offered for download on the author's page, but **availability is not a licence**. This project must not copy its code, assets or help text. If reuse is ever desired, obtain written permission from the author (Dr. Mohammed Hawa, University of Jordan).
- **`io.cs`:** supplied for custom-device authors (author credit: deTrox Yang). Keep its header intact; the path-constant fix is a documented modification.
- **emu8086:** commercial software; this project only produces a separate device executable and does not modify or redistribute emu8086.
- **Lab manual and MDA-Win8086 manual:** used as specification facts (port addresses, bit meanings, behaviours). Do not reproduce manual text, figures or full programs in the repository beyond the minimal fixtures needed for tests; link to sources instead.
- **Monitor ROM:** do not dump or distribute Midas firmware.
- **Fonts:** author the LCD 5×8 bitmap table from scratch or use a clearly licensed one; record provenance (R-11).
- **Naming:** label as **unofficial**; no Midas or emu8086 branding; confirm sharing rules with the course instructor (Q-11).

---

## 16. Deliverables and definition of done

**Deliverables**

1. `MDA8086_Kit.exe` (plus optional `MDA8086_Kit.config`).
2. Source repository with the layout in §9.5 and passing unit tests.
3. `/samples/asm` containing emu8086-ready reference programs and expected-result notes.
4. User guide: install (copy to `devices` folder), file-permission setup, how to activate, port quick-reference, known limitations (monitor/register display, CPU reset, port `01H` aliasing).
5. Test log recording emu8086 version, Windows version, speeds tested and results for IT-1…IT-8.

**Definition of done (release 1.0)**

- All P0 requirements implemented and verified (SC-2).
- SC-1, SC-3, SC-4 and SC-5 met.
- Open questions Q-01, Q-02, Q-03, Q-05 answered and the document updated.
- Known limitations documented in the user guide.

---

## 17. Glossary

| Term | Meaning |
|---|---|
| 8255A / PPI | Programmable Peripheral Interface: three 8-bit ports (A, B, C) plus a control register. |
| Mode 0 | Basic I/O mode of the 8255A (no handshaking). |
| BSR | Bit Set/Reset: control-word form that sets or clears a single port C bit. |
| Latch | Output register holding the last value written to a port. |
| Active-low / active-high | A signal that is "on" at logic 0 / at logic 1. |
| HD44780 | Industry-standard character LCD controller. |
| DDRAM / CGRAM | Display data RAM (characters on screen) / character generator RAM (custom glyphs). |
| BF | Busy flag in the LCD status register. |
| Persistence of vision | Human eye blending a rapidly scanned display into a steady image; modelled by the integrator. |
| Monitor ROM | Firmware on the kit that handles keypad, LCD and PC download. |
| MASM / LOD186 / COMM | Assembler, loader/locator and serial-download tools used in the lab's PC-mode flow. |
| RES, STP, AD, GO, DA, MON, REG | Kit function keys: reset, single-step, set address, go, update data, break/NMI, register display. |
| `emu8086.io` / `emu8086.hw` | Files in the root of drive C: used by emu8086 for virtual-device communication. |

---

## 18. Appendices

### Appendix A — In-scope port quick reference

| Port | Direction (CPU view) | Function |
|---|---|---|
| 00H | Write | LCD instruction register |
| 01H | Read / Write | Keypad register (read) / keyboard flag (write) |
| 02H | Read | LCD status register |
| 04H | Write (read TBD) | LCD data register |
| 18H | Write | CS1 port A — dot matrix red, active-low |
| 19H | Write | CS2 port A — 7-segment, active-low |
| 1AH | Write | CS1 port B — dot matrix green, active-low |
| 1BH | Write | CS2 port B — LEDs (bits 0–3), active-high |
| 1CH | Write | CS1 port C — dot matrix scan line, one-hot |
| 1DH | — | CS2 port C (not used in scope) |
| 1EH | Write | CS1 control register |
| 1FH | Write | CS2 control register |

### Appendix B — Source references

- AUST, *CSE 3118 Microprocessors and Microcontrollers Lab* manual (Sessions 1, 2, 5 and the MDA-8086 address map): `https://www.aust.edu/lab_manuals/CSE/CSE%203118%20Lab%20Manual.pdf`
- I/O Emulation Kit download page (author's site, linked from the lab manual): `https://sites.google.com/site/hawawebsite/more/emulation-kit`
- MDA-Win8086 user's manual (third-party hosted copy; keyboard and LCD experiments): `https://manualzz.com/doc/6781119/mda-win8086-manual`
- emu8086 custom-device `_READ_ME.txt` and `io.cs` (supplied by the project owner).

### Appendix C — Document history

| Version | Date | Notes |
|---|---|---|
| 0.1 | 2 Oct 2026 | Initial project summary and PRD from the lab manual, emu8086 device README and `io.cs` analysis. |

## Backported IDs from S1 Spike
- R-14: Emulator merges consecutive identical IO writes if too fast.
- R-15: emu8086.hw is not created or used.
- R-16: IO file is truncated to highest port, not 64KB.
- Q-13: Does emu8086 write consecutive identical bytes reliably? (No)
- Q-14: Is file size 64KB? (No, truncated)
- Q-15: Is emu8086.hw used? (No)
- T-08: Run S1 spikes to determine emulator behavior.
- T-09: Write PortLogAnalyzer to determine feasibility.
