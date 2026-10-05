# plan.md — MDA-8086 Virtual Trainer Kit (emu8086 virtual device)

| | |
|---|---|
| Derived from | `MDA8086_Virtual_Kit_PRD.md` v0.1 (2 Oct 2026) |
| Plan version | 0.2 — complete through release (PH0–PH6, appendices A–I) |
| Audience | A coding agent (AGENT) working with the project owner (HUMAN, Aditya) |
| Status | Not started. **No production code exists yet.** |
| Provenance | Merges two drafts: the Claude draft (PH0 spikes, gate G0, task format, risk findings) and the GPT draft (hardware-model detail, contracts, UI/port-monitor spec, traceability, Plan B). Appendix I records which parts came from where. |

---

## 0. How to use this plan

**Roles**

| Tag | Meaning |
|---|---|
| **[AGENT]** | Work the coding agent does alone (code, tests, docs). |
| **[HUMAN]** | Work that needs emu8086, the real MDA-8086 kit, a lab, or a decision. The agent cannot do it and must **not fake results**. Each HUMAN task states exactly what to bring back. |
| **[BOTH]** | The agent prepares or analyses; the human runs or decides. |

**Identifiers.** Phases `PH0…PH6` match PRD milestones `M0…M6`. Tasks are `PHn-nn`. PRD IDs (`FR-*`, `NFR-*`, `T-*`, `IT-*`, `S*`, `Q-*`, `R-*`, `D*`, `SC-*`) are reused unchanged. IDs introduced by this plan continue the PRD sequences (`R-14…R-20`, `Q-13…Q-16`, `T-08…T-09`; findings are `F1…F10`). PH0-02 records them in `docs/`; PH1-01 back-ports them into the PRD copy.

**Task format.** Every task lists *Goal, Depends on, Files, Steps, Acceptance, Traces*. A task is **done** only when every acceptance bullet is true, `dotnet build` is warning-free for Core, `dotnet test` passes, the change is committed, and its checkbox in §2 is ticked.

**Working loop for each task**

1. Read the task and the linked PRD sections.
2. Write or extend tests first where the task has a test list.
3. Implement.
4. Run `dotnet build` and `dotnet test`.
5. Commit with message `PHn-nn: <summary> (<FR/NFR ids>)`.
6. Tick the checkbox in §2 and, if something surprising was learned, append to `docs/decisions.md`.

**Stop-and-ask triggers** (stop and write the question in `docs/questions.md`, then continue only with unblocked tasks):

- Any result contradicts an assumption in the PRD or this plan.
- A task needs a hardware fact the PRD marks `[Unknown]`.
- A gate (G0, X1…X6) is reached.
- A library other than those allowed in §5.3 seems necessary.

---

## 1. Planning findings that go beyond the PRD

These came from analysing `io.cs` and the reference programs while writing this plan. Each has a defined response.

| # | Finding | Why it matters | Response in this plan |
|---|---|---|---|
| F1 | The file stores a **value, not an event**. Two consecutive writes of the **same byte** to a port (e.g. the two `L`s in `HELLO`, `OUT 04H,AL` twice) are indistinguishable from one write. | The 8255 outputs are **state-based**, so identical writes change nothing. The **LCD data register is stream-based**: every write appends a character. A polling device cannot reliably decode repeated characters. This is the single biggest feasibility risk (**R-14**). | S1 scenario C measures it. Gate **G0** decides whether the LCD ships as full, "best-effort", or via Plan B. PH4-06 holds the conditional mitigation work. |
| F2 | If two ports change within one poll interval, their **real order is lost**. Example: `OUT 1FH,80H` then `OUT 19H,0C0H` seen in one poll. Applying `19H` before `1FH` would let the mode-set wipe the data. | Wrong order corrupts state (**R-16**). | `BusPoller` applies a **control-first** ordering policy (PH1-04), configurable. |
| F3 | A repeated **mode-set with the same control value** (e.g. `80H` twice) produces no change in the file. | The 8255 would normally clear its latches on every mode-set (**R-15**). | Documented limitation; low impact for lab programs. Test T-01 covers the model; the limitation goes in the user guide. |
| F4 | emu8086 may **zero or rewrite** the file when an emulation starts, reloads or stops. A value of `00H` written to a control port is a valid **BSR command**, so naive handling would corrupt state (**R-17**, **Q-13**). | Bogus control words after restart. | S1 scenario F records lifecycle behaviour. PH5-03 implements optional "emulator reset detection" (burst-of-zeros heuristic), enabled only if S1 proves it is needed. |
| F5 | `io.cs` declares `"C:\emu8086.io"` without a verbatim `@` prefix. | Compile error on older C#; on C# 13+ silently becomes ESC + `mu8086.io` (**R-06**). | Own `EmuIoPortFile` using `@"C:\emu8086.io"` (PH1-05). Never compile `io.cs` into the product. |
| F6 | The real kit's **monitor ROM leaves the LCD initialised** when a user program starts, whereas a bare HD44780 powers up with the display off (**R-18**, **Q-14**). | A program that never initialises the LCD works on the kit but would show nothing in a bare model. | `LcdPowerUpState` setting: `Hd44780` (PRD default) or `MonitorLike`; real default decided after S4 (PH4-05). |
| F7 | The keypad **protocol is unknown** (**Q-03**) and the physical key layout is unknown (**Q-15**). | M5 is partly blocked. | Build the keypad behind `IKeypadProtocol` with a clearly labelled provisional implementation (PH5-01); finalise after S4/S5 (PH5-04/05). |
| F8 | "Single `.exe`" (NFR-08) conflicts with a multi-assembly solution (**R-19**). | emu8086 lists `.exe` files only; extra DLLs complicate installation. | PH6-03 embeds assemblies (Costura.Fody, fallback ILRepack, fallback zip with DLLs). |
| F9 | **Device-owned bytes can be stale.** The file keeps values from earlier runs. If port `02H` (LCD status) still holds a byte with bit 7 set, a program that spins on the busy flag hangs forever; a stale `01H` looks like a pressed key (**R-20**). | A program can hang or misread through no fault of its own. | On every (re)connect and on RES, the device writes defined values to the ports it owns (`02H`, `01H`) before anything else (PH4-03, PH5-01). |
| F10 | The PRD is internally in tension: S2 (§14.1) says emu8086-ready copies of the lab programs go in `/samples/asm`, but §15 forbids reproducing full programs or manual text in the repository. | A careless agent could commit copyrighted lab material. | PH0-07 records **edits only** (diff style) in `S2.md`; lab-program copies stay in a git-ignored private folder; repository samples are original (rule 3 in §3). |

---

## 2. Progress tracker

Tick as tasks complete. Gate and exit lines are reviewed with the human.

### PH0 — Spikes and feasibility (M0)
- [ ] PH0-01 [HUMAN] Environment inventory
- [ ] PH0-02 [AGENT] Repository scaffold and tool project shells
- [ ] PH0-03 [AGENT] Implement `PortLogger` (read-only)
- [ ] PH0-04 [AGENT] Author S1 spike programs A–G
- [ ] PH0-05 [HUMAN] Run S1 experiments, collect logs
- [ ] PH0-06 [AGENT] `PortLogAnalyzer` and S1 report
- [ ] PH0-07 [HUMAN] S2 — assembler syntax compatibility
- [ ] PH0-08 [HUMAN] S3 — device mechanism checks
- [ ] PH0-09 [HUMAN] S4 — real-kit reference capture (deferrable to PH3/PH5)
- [ ] PH0-10 [HUMAN] S5 — obtain MDA-Win8086 manual (deferrable)
- [ ] PH0-11 [BOTH] **Gate G0** — go/no-go and scope decision

### PH1 — Skeleton and infrastructure (M1)
- [ ] PH1-01 [AGENT] Solution, projects, build settings
- [ ] PH1-02 [AGENT] Core primitives and test doubles
- [ ] PH1-03 [AGENT] `KitConfig` and INI parser
- [ ] PH1-04 [AGENT] `BusPoller`
- [ ] PH1-05 [AGENT] `EmuIoPortFile`
- [ ] PH1-06 [AGENT] `KitController` skeleton, snapshot, port-monitor data
- [ ] PH1-07 [AGENT] WinForms shell: `Program`, `MainForm`, `PollLoop`
- [ ] PH1-08 [AGENT] `ScriptedPortSource` and `--sim` mode
- [ ] PH1-09 [AGENT] Logging and CSV export
- [ ] PH1-10 [HUMAN] M1 verification inside emu8086
- [ ] **X1** Exit review M1

### PH2 — 8255, 7-segment, LEDs (M2)
- [ ] PH2-01 [AGENT] `Ppi8255`
- [ ] PH2-02 [AGENT] `SevenSegmentModel`
- [ ] PH2-03 [AGENT] `LedBank`
- [ ] PH2-04 [AGENT] Controller wiring for CS2, reset, undriven handling
- [ ] PH2-05 [AGENT] `SevenSegControl`, `LedControl`, layout
- [ ] PH2-06 [AGENT] Original sample programs `seg_count.asm`, `led_chase.asm`
- [ ] PH2-07 [HUMAN] IT-1 and IT-2 at three speeds
- [ ] **X2** Exit review M2

### PH3 — Dot matrix (M3)
- [ ] PH3-01 [AGENT] `DotMatrixModel` pin decode and orientation
- [ ] PH3-02 [AGENT] Persistence integrator
- [ ] PH3-03 [AGENT] CS1 wiring and `DotMatrixControl`
- [ ] PH3-04 [AGENT] Sample programs and orientation probes
- [ ] PH3-05 [AGENT] Degradation and tuning harness
- [ ] PH3-06 [HUMAN] IT-3 and orientation probes on the real kit
- [ ] **X3** Exit review M3

### PH4 — LCD (M4)
- [ ] PH4-01 [AGENT] LCD 5×8 font table with provenance
- [ ] PH4-02 [AGENT] `Hd44780Lcd` model
- [ ] PH4-03 [AGENT] LCD controller wiring and status publishing
- [ ] PH4-04 [AGENT] `LcdControl`
- [ ] PH4-05 [AGENT] Power-up state option (and optional busy timing)
- [ ] PH4-06 [AGENT] **Conditional** LCD stream mitigation per G0
- [ ] PH4-07 [AGENT] Sample program `lcd_hello.asm`
- [ ] PH4-08 [HUMAN] IT-4
- [ ] **X4** Exit review M4

### PH5 — Keypad and reset (M5)
- [ ] PH5-01 [AGENT] `KeypadModel`, `IKeypadProtocol`, provisional protocol
- [ ] PH5-02 [AGENT] `KeypadControl` and PC-keyboard mapping
- [ ] PH5-03 [AGENT] Reset button, controller reset, emulator-reset detection
- [ ] PH5-04 [HUMAN] Keypad probe on the real kit; protocol evidence
- [ ] PH5-05 [AGENT] Final keypad protocol (blocked by Q-03)
- [ ] PH5-06 [HUMAN] IT-5 and IT-6
- [ ] **X5** Exit review M5

### PH6 — Hardening and release (M6)
- [ ] PH6-01 [AGENT] Settings persistence and dialog
- [ ] PH6-02 [AGENT] Theme, DPI, always-on-top, About
- [ ] PH6-03 [AGENT] Single-exe packaging and release script
- [ ] PH6-04 [AGENT] Documentation set
- [ ] PH6-05 [AGENT] Performance and soak tooling
- [ ] PH6-06 [BOTH] Fault injection and soak (IT-7, IT-8), test log
- [ ] PH6-07 [BOTH] Release candidate, classmate dry run, tag `v1.0.0`
- [ ] **X6** Exit review M6 / release

---

## 3. Ground rules for the agent

1. **Never invent hardware facts.** Where a fact is missing, use the documented assumption, mark the code `// ASSUMPTION(Q-nn)`, and list it in `docs/assumptions.md`.
2. **Core stays pure.** `Mda8086Kit.Core` has no `System.IO`, `System.Windows.Forms`, `System.Drawing`, threading primitives beyond `lock`, or P/Invoke. Time comes only from `IClock`.
3. **Original work only.** Do not copy code, assets or help text from Emulation Kit, the Midas manuals, or emu8086's sample devices. The AUST lab programs may be **studied**, but the repository's sample `.asm` files must be **written independently** to produce the same port-write timelines (PRD §15).
4. **Read-only spikes.** `PortLogger` and every S1 experiment tool must open `c:\emu8086.io` and `c:\emu8086.hw` **read-only**. The product writes to `c:\emu8086.io` only through `EmuIoPortFile`, and only for device-supplied bytes (ports `01H`, `02H`).
5. **No new dependencies** beyond §5.3.
6. **No sleeps in unit tests.** Use `FakeClock` and `FakePortSource`.
7. **No `Application.DoEvents`, no `Thread.Abort`.** Use `CancellationToken` and `ManualResetEventSlim`.
8. **Keep documents in sync.** If a requirement changes, edit the PRD, add a dated line to `docs/decisions.md`, and mention it in the commit message.
9. **Do not tick HUMAN tasks.** Only the human ticks them (or tells the agent to).
10. **Fail soft in the product.** I/O problems become status messages, never unhandled exceptions (NFR-05).
11. **One door to the file.** UI code never reads or writes `c:\emu8086.io`; device models never open files. Only `EmuIoPortFile` touches it (via `IPortSource`).
12. **Controls draw model state.** Display controls consume `SegmentState`, `LedState`, brightness grids and `LcdDisplayState`. They never decode raw port bytes.
13. **No monitor ROM, no CPU control.** Do not emulate the kit's monitor ROM or its LCD register display, and never let RES try to restart emu8086's CPU (PRD §3.2, §6.3).
14. **No premature claims.** Do not write "matches the real kit" anywhere until the matching integration test and its environment are recorded in `docs/test-log.md`.
15. **Keep the debugging hooks.** The port monitor and logging stay alive from M1 onward; they are the main instrument for comparing virtual and real behaviour.
16. **Tunables stay configurable.** Poll interval, ordering policy, integrator window, gain, smoothing, thresholds and orientation are settings, not constants.
17. **Small and correct beats pretty and wrong.** Prefer a small, testable model over a convincing but semantically incorrect shortcut.
18. **Feature gate.** Before accepting any feature not in the PRD, apply the four questions in Appendix D (non-goal guardrail). If any answer is no, defer it.
19. **Record every lab-source edit.** Any change needed to make a lab program assemble in emu8086 goes in `S2.md`; never silently rewrite reference code.

### 3.1 Source-of-truth hierarchy

When two sources disagree, the higher one wins. Never silently turn an `[Unknown]` or open question into a claimed hardware fact.

1. **Real MDA-8086 kit observations** (S4 and later verification).
2. **Lab manual facts** recorded in the PRD.
3. **Resolved feasibility findings** (S1–S5, G0).
4. **PRD working assumptions** (A-1…A-6) where questions remain open.
5. **Implementation choices in this plan**, only where the PRD leaves room.

When the real kit contradicts the PRD, update the PRD and this plan, and treat the kit as authoritative (PRD §11.4).

---

## 4. Environment and tooling

### 4.1 Prerequisites

| Item | Requirement |
|---|---|
| OS | Windows 10 or 11, x64 |
| IDE / SDK | Visual Studio 2022 with ".NET desktop development" and the **.NET Framework 4.8 targeting pack**, **or** .NET SDK 8.0 plus the automatic `Microsoft.NETFramework.ReferenceAssemblies` package |
| emu8086 | Installed and working; record exact version and install path (PH0-01) |
| Git | Any recent version |
| Permissions | Read/write on `c:\emu8086.io` and `c:\emu8086.hw` (README requirement) |

### 4.2 Standard commands

```
dotnet --info
dotnet build Mda8086Kit.sln -c Debug
dotnet test  Mda8086Kit.sln -c Debug --no-build
dotnet test  tests/Mda8086Kit.Core.Tests -c Debug --collect:"XPlat Code Coverage"
```

If the SDK-style `net48` WinForms project does not honour `<UseWindowsForms>true</UseWindowsForms>`, add explicit `<Reference Include="System.Windows.Forms" />` and `<Reference Include="System.Drawing" />`.

---

## 5. Solution structure

### 5.1 Projects

```
Mda8086Kit.sln
src/
  Mda8086Kit.Core/            netstandard2.0   pure models, bus, config parsing, controller
  Mda8086Kit.Io/              netstandard2.0   EmuIoPortFile, CSV export, file logging
  Mda8086Kit/                 net48 WinExe     WinForms shell; AssemblyName = MDA8086_Kit
tests/
  Mda8086Kit.Core.Tests/      net8.0 (xUnit)
  Mda8086Kit.Io.Tests/        net8.0 (xUnit)
tools/
  PortLogger/                 net48 console    read-only port logger used by S1 (not shipped)
  PortLogAnalyzer/            net8.0 console   analyses PortLogger CSV (not shipped)
  MatrixTuner/                net8.0 console   integrator tuning harness, added in PH3-05 (not shipped)
samples/
  spikes/                     S1 programs (emu8086 syntax)
  asm/                        original reference programs and expected-result notes
docs/                         PRD, user guide, decisions, questions, assumptions, test log, spike reports
third_party/
  io.cs                       original helper, header intact, reference only (not compiled)
```

### 5.2 Project settings

| Setting | Core | Io | Shell | Tests |
|---|---|---|---|---|
| `LangVersion` | 9.0 | 9.0 | 9.0 | latest |
| `Nullable` | disable | disable | disable | disable |
| `TreatWarningsAsErrors` | **true** | true | false | false |
| `Deterministic` | true | true | true | true |
| References | none | Core | Core, Io | project under test |

Avoid language features that need newer runtime support (`record`, `init`) in `netstandard2.0` projects.

### 5.3 Allowed packages

- Tests: `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `coverlet.collector`.
- Packaging only (PH6-03): `Costura.Fody` (preferred) or `ILRepack`.
- Everything else: BCL only. **Configuration is a hand-parsed INI file**; no JSON library.

### 5.4 Namespaces inside Core

```
Mda8086Kit.Core.Abstractions   IClock, IPortSource, IPortDevice, IDeviceWriteSink, IWarningSink,
                               PortChange, PortBatch, ConnectionState, ConnectionStatus
Mda8086Kit.Core.Bus            BusPoller, PollSettings, PollOutcome, ChangeOrdering, AddressDecoder
Mda8086Kit.Core.Config         KitConfig, PortMap, PpiPortMap, IniParser, ConfigWarning
Mda8086Kit.Core.Devices        Ppi8255, PinState, SevenSegmentModel, LedBank, DotMatrixModel,
                               DotMatrixOrientation, PersistenceIntegrator, Hd44780Lcd, LcdFont,
                               LcdPowerUpState, KeypadModel, KeypadKey, IKeypadProtocol
Mda8086Kit.Core.Kit            KitController, KitSnapshot, DeviceWrite, PortMonitorLog,
                               PortMonitorEntry, ResetCoordinator
Mda8086Kit.Core.Simulation     ScriptedPortSource, SimScripts, CsvReplayPortSource (pure, clock-driven)
```

### 5.5 Architecture and data flow

```
 emu8086 CPU --IN/OUT--> c:\emu8086.io   (byte N = port N)
                              |
                    [EmuIoPortFile]  Io: one FileStream, bulk read, safe single-byte writes
                              |
                      [PollLoop]     Shell: dedicated thread, timeBeginPeriod(1), calls PollOnce()
                              |
                       [BusPoller]   Core: diff vs shadow copy -> ordered PortBatch
                              |
                      [KitController]  Core: one lock, owns every device, publishes snapshots
          ________________|______________________________________
         |          |            |             |            |
   [Ppi8255 CS1] [Ppi8255 CS2] [Hd44780Lcd]  [Keypad]   [Reset]
         |             |
   [DotMatrixModel] [SevenSegmentModel] [LedBank]   (stateless views of PPI pin state)
         |
   [PersistenceIntegrator]
                              |
                   immutable KitSnapshot (volatile reference)
                              |
                [WinForms controls, 60 Hz repaint timer]
```

**Layering rule.** Core never depends on WinForms, `System.Drawing`, P/Invoke or the file system. Io owns files. The Shell owns threads, timers and drawing.

**Per-poll sequence** (executed by `KitController.OnPoll`, always in this order):

1. `Tick(t)` on every device. The `PersistenceIntegrator` accounts the interval that **ended** at `t` using the state that was in force **before** this poll's changes.
2. Dispatch the batch's changes in policy order (control-class ports first, then ascending port number) to `OnCpuWrite`.
3. Drain bytes the devices posted through `IDeviceWriteSink` (LCD status, keypad). The poll thread writes them to the file and calls `BusPoller.NoteDeviceWrite` so they are not mistaken for CPU writes.
4. If anything changed and at least `ui.snapshot_ms` (default 8 ms) has passed, build and publish a new immutable snapshot.

**The file carries no direction.** `c:\emu8086.io` cannot distinguish a CPU `OUT` from a device-supplied `IN` value. Observed changes are treated as CPU-visible bus changes unless the device itself wrote them. Do not invent a second channel (D6).

### 5.6 Core contracts

Names may evolve; the separation may not. These live in `Mda8086Kit.Core.Abstractions` and must stay free of WinForms types, `FileStream`, control handles and message boxes.

```csharp
public interface IClock
{
    long Ticks { get; }                 // monotonic; Stopwatch ticks in production
    long TicksPerMillisecond { get; }
}

public readonly struct PortChange
{
    public PortChange(int port, byte oldValue, byte value, long ticks) { /* ... */ }
    public int  Port     { get; }
    public byte OldValue { get; }
    public byte Value    { get; }
    public long Ticks    { get; }       // one timestamp per poll
}

public interface IPortSource : IDisposable
{
    ConnectionState State { get; }
    // Never throws. Returns false and updates State on any failure.
    bool TryReadRange(int firstPort, byte[] buffer, int count);
    // Never throws, never resizes the file. Device-supplied bytes only.
    bool TryWriteByte(int port, byte value);
}

public interface IDeviceWriteSink { void Post(int port, byte value); }

public interface IPortDevice
{
    string Name { get; }
    bool Handles(int port);
    void OnCpuWrite(int port, byte value, long ticks);
    void Tick(long ticks);
    void Reset();
}
```

`PortBatch` is a reusable, preallocated container (ticks, count, indexer, `IsBulk`, `Reseeded`) so an unchanged poll allocates nothing (PH6-05 enforces this). `ConnectionState` carries status, a one-line message, a one-line remedy, the time it began and a consecutive-failure count.

### 5.7 Default port map

The default map **must equal the real kit** (PRD §5.2 and Appendix A). All twelve ports lie in `00H–1FH`, so the default poll window is 32 bytes.

| Function | Port | Function | Port |
|---|---:|---|---:|
| LCD instruction (write) | `00H` | CS2 control | `1FH` |
| Keypad read / keyboard flag write | `01H` | CS1 Port A (matrix red) | `18H` |
| LCD status (read) | `02H` | CS2 Port A (7-segment) | `19H` |
| LCD data | `04H` | CS1 Port B (matrix green) | `1AH` |
| CS1 control | `1EH` | CS2 Port B (LEDs) | `1BH` |
| CS1 Port C (matrix scan) | `1CH` | CS2 Port C (unused) | `1DH` |

**Manual discrepancy, do not "fix".** The lab manual's address table prints `1CH` as a CS1 control register. The lab programs use `1EH` (PRD §5.2). Implement `1EH` and keep the discrepancy visible in `docs/port-map.md`. Never change the default map silently.

---

## 6. Conventions

**Code.** PascalCase types/members, `_camelCase` private fields, `readonly` wherever possible, no public mutable arrays (return copies or read-only views), XML doc comment on every public type and member, citing PRD sections (e.g. `/// PRD §5.3`).

**Time.** All timestamps are `long` ticks from `IClock.Ticks` (Stopwatch frequency). Convert with `IClock.TicksPerMillisecond`. Never use `DateTime.Now` in logic.

**Threading.** One polling thread, one UI thread. All shared model state is guarded by `KitController.SyncRoot` (a single `object`); locks are held only for short, non-blocking work. No I/O inside the lock. The UI never takes the lock to draw: it reads the latest published `KitSnapshot` reference (`Volatile.Read`). UI-originated actions (key press, RES, settings) take the lock briefly and post any device writes to the queue; the poll thread performs the file write.

**Tests.** Arrange-Act-Assert; names `Method_Scenario_Expected`; `[Theory]` for tables; fixtures in `tests/.../Fixtures`. Core target ≥ 90% line coverage (informational, not a gate).

**Commits and branches.** `main` always builds and passes tests. Work on `ph<n>-<topic>` branches, squash-merge, tag `m1…m6` at each exit review and `v1.0.0` at release.

**Docs.** Each spike produces `docs/spike-reports/Sx.md`; each decision a dated bullet in `docs/decisions.md`; each unanswered question an entry in `docs/questions.md` (ID, text, blocking tasks, evidence needed).

---

# PH0 — Spikes and feasibility (M0)

**Purpose:** answer the questions that decide the architecture before any product code is written. Product code must not start until **Gate G0** (PH0-11) is passed. Tasks PH0-09 and PH0-10 are deferrable and do **not** block G0.

#### PH0-01 — Environment inventory · [HUMAN] · size S
- **Goal:** record the facts every later task depends on (Q-08, Q-09, Q-10).
- **Depends on:** none.
- **Steps:**
  1. Record Windows version, whether running as admin, .NET Framework 4.8 present (`reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Release`), Visual Studio or `dotnet --info` output.
  2. Record emu8086 version (Help → About), install folder, and the exact path of the `devices` folder.
  3. Check whether `c:\emu8086.io` and `c:\emu8086.hw` exist **before** starting emu8086, after starting it, and after emulating a program. Record sizes.
  4. Check file permissions on both (Properties → Security) for the current user.
- **Bring back:** `docs/environment.md` filled from the template the agent provides in PH0-02.
- **Acceptance:** every field in the template is filled or marked "not found".
- **Traces:** Q-08, Q-10, NFR-01.

#### PH0-02 — Repository scaffold and tool project shells · [AGENT] · size S
- **Goal:** create the repo skeleton and the two tool projects, **no product code**.
- **Depends on:** none.
- **Files:** `Mda8086Kit.sln` (tools only for now), `.editorconfig`, `.gitignore` (VS + dotnet), `docs/` templates (`environment.md`, `decisions.md`, `questions.md`, `assumptions.md`, `test-log.md`, `spike-reports/`), `samples/spikes/`, `samples/asm/`, `third_party/io.cs` (copied from the owner's file with header intact and a `README.txt` noting the path-constant defect), `tools/PortLogger/`, `tools/PortLogAnalyzer/`, copy of the PRD into `docs/`.
- **Steps:** create empty projects per §5.1 settings; add the new IDs this plan introduces (`R-14…R-20`, `Q-13…Q-16`, `T-08…T-09`, findings `F1…F10`; see §1 and Appendix E) to `docs/questions.md` and a risk list in `docs/decisions.md`.
- **Acceptance:** `dotnet build` succeeds for the two tool projects; folder layout matches §5.1; templates exist.
- **Traces:** setup.

#### PH0-03 — Implement `PortLogger` (read-only) · [AGENT] · size M
- **Goal:** a console tool that records every byte change in `c:\emu8086.io` (and `c:\emu8086.hw`) with microsecond timestamps.
- **Depends on:** PH0-02.
- **Files:** `tools/PortLogger/*`.
- **Behaviour spec:**
  - Opens **both** files with `FileAccess.Read` and `FileShare.ReadWrite | FileShare.Delete`, **never writes**. If a file is missing, wait and retry every 250 ms, printing the state.
  - Command line: `--mode 1ms|spin` (default `1ms`), `--range 0 256` (bytes of `.io` to watch, default 0–255), `--hwrange 0 256`, `--out <dir>` (default Desktop), `--no-hw`.
  - `1ms` mode: call `timeBeginPeriod(1)` (winmm P/Invoke), poll with a `Stopwatch` + `Thread.Sleep(1)`. `spin` mode: tight loop with `Thread.SpinWait(20)` (uses a full core; best-case capture reference).
  - One bulk read per source per poll (single `Seek(0)` + `Read`), compared with a shadow copy.
  - Output file 1 `*_changes.csv`: `t_us,src,offset,old,new` where `src` is `io` or `hw`. Times are microseconds since logger start.
  - Output file 2 `*_files.csv`, one row every 250 ms: `t_ms,io_size,io_lastwrite_utc,hw_size,hw_lastwrite_utc,hw_first64_hex`.
  - **Markers:** pressing a letter key inserts a row `t_us,MARK,<letter>,,` so scenarios can be segmented.
  - Buffered writer, flush every second and on exit; Ctrl+C flushes and exits cleanly.
  - Live console line: elapsed time, total changes, last 5 changes.
- **Acceptance:**
  - Unit-style self-test: a hidden `--selftest` flag creates a temp file, mutates it from another thread, and verifies changes are logged.
  - Verified by inspection that no write-capable `FileStream` or `File.Write*` call exists (add a comment block stating the read-only guarantee).
- **Traces:** S1, R-01, R-14, Q-01.

#### PH0-04 — Author S1 spike programs A–G · [AGENT] · size M
- **Goal:** original emu8086 programs that exercise the polling model. Put them in `samples/spikes/`, each with a header comment describing purpose and how to use it.
- **Depends on:** PH0-02.
- **Programs** (emu8086 syntax; use only ports named; loops use `LOOP`):

| File | Behaviour |
|---|---|
| `s1a_burst_distinct.asm` | Forever: increment a burst counter, `OUT 0EH,counter` (segment marker), then send bytes `41H…48H` to port `04H` back-to-back with **no delay**, then a long idle delay (~0.5 s at default speed). |
| `s1b_burst_delay.asm` | Same burst, but with an inner delay of N loop iterations between writes. Steps through N = 10, 100, 1000, 10000; each N repeated 20 bursts; `OUT 0DH,<N index 1..4>` marks the current N. |
| `s1c_identical.asm` | Writes the byte `4CH` twice to port `04H` ("LL") in four variants: (1) back-to-back; (2) 100-iteration gap; (3) 10000-iteration gap; (4) with an `OUT 00H,06H` between the two writes. Each variant preceded by `OUT 0CH,<variant>`. Expected: the second write is invisible as a value change; the analysis looks for any side-channel in `.hw` or file metadata. |
| `s1d_matrix_scan.asm` | Original program producing the same **port-write timeline** as the dot-matrix "A" reference: `OUT 1EH,80H`; `OUT 18H,0FFH`; then forever, for i = 0…7: `OUT 1AH,font[i]`, `OUT 1CH,(1<<i)`, short delay (parameter `COLDELAY` = 300 `NOP`-loops). Font bytes `FFH,C0H,B7H,77H,77H,B7H,C0H,FFH` (as data; this is a fact, not manual text). |
| `s1e_static.asm` | The 7-segment 0–9 timeline (`OUT 1FH,80H`, `OUT 19H,<codes>` with `CX=0FFFFH` delay) followed by the LED chase timeline (`OUT 1BH,01/02/04/08H`) in an outer loop. |
| `s1f_lifecycle.asm` | Writes `19H←0AAH`, `1BH←55H`, `04H←41H` once and then spins. Used with manual start/stop/reload/close actions. |
| `s1g_speed.asm` | A fixed-length write loop whose instruction count per cycle is documented in comments, used to estimate emu8086's effective instructions per second at each speed setting. |

- **Acceptance:** each file assembles in emu8086 (agent cannot test; the human confirms in PH0-05); each documents its port-write timeline in a comment table; no manual text is reproduced.
- **Traces:** S1, Q-01, R-01, R-14, Q-13.

#### PH0-05 — Run S1 experiments, collect logs · [HUMAN] · size M
- **Goal:** produce measured data for G0.
- **Depends on:** PH0-03, PH0-04.
- **Procedure:**
  1. Build `PortLogger`. Start it **before** running a program; keep it running for a whole scenario. Press a marker letter at each manual step (write the letter legend in `docs/spike-reports/S1.md`).
  2. If `c:\emu8086.io` does not appear without a device, copy `PortLogger.exe` into emu8086's `devices` folder, restart emu8086, and start it from the **Virtual Devices** menu instead; note this in the report (it answers part of Q-08).
  3. Run scenarios **A, B, C, D, E** each at three emu8086 speed settings (slowest, default, fastest) and each with `--mode 1ms` and `--mode spin`. Keep each run ≥ 30 s.
  4. Run **F**: marker-annotate these actions in order — Emulate, Run, Stop, Reload/Restart, close emulator window, close emu8086, reopen.
  5. Run **G** at each speed to estimate instructions per second.
- **Bring back:** all `*_changes.csv` and `*_files.csv`, plus notes on emu8086 speed settings used.
- **Acceptance:** at least scenarios A–F captured at default speed with both polling modes; any skipped run is explained.
- **Traces:** S1, Q-01, Q-13, Q-08.

#### PH0-06 — `PortLogAnalyzer` and S1 report · [AGENT] · size M
- **Goal:** turn the logs into numbers and a written recommendation.
- **Depends on:** PH0-05 (and the sample-device sources from PH0-08 step 6, when available).
- **Files:** `tools/PortLogAnalyzer/*`, `docs/spike-reports/S1.md`.
- **Analysis spec** (per scenario, per speed, per polling mode):
  - **A/B:** expected distinct changes per burst (8) vs observed; capture % per N; smallest N with ≥ 99% capture.
  - **C:** confirm second identical write produces no change; list any `.hw` byte changes or file-metadata changes correlated in time with each `OUT` (look for a per-write toggle or counter).
  - **D:** per matrix cycle, number of (B,C) pairs observed out of 8; fraction of cycles with all 8; observed order of B/C changes.
  - **E:** capture % of each digit/LED transition.
  - **F:** list any bulk change (≥ 4 ports changing in one poll) at each lifecycle marker, especially zeroing; file size changes.
  - **G:** estimated instructions per second per speed.
  - Same-tick ordering: how often two ports change in one poll, to size R-16.
- **Report content:** tables of the above; a plain-language answer to Q-01 and Q-13; a paraphrased summary of what the shipped `Printer` and `Simple` devices do about polling, stream handling, start/stop/retry and `c:\emu8086.hw` (no source pasted); a **recommended G0 outcome** using §G0 below.
- **Acceptance:** report reproducible by running `PortLogAnalyzer <csv>`; every number in the report traceable to a CSV.
- **Traces:** S1, R-01, R-14, R-16, R-17.

#### PH0-07 — S2: assembler syntax compatibility · [HUMAN] · size M
- **Goal:** find out whether the lab's MASM-style programs assemble in emu8086 unchanged (Q-02).
- **Depends on:** none.
- **Procedure:** for each of the lab programs (Session 1 problems incl. `INT 3` endings and the `.MODEL SMALL` examples; Session 5 7-segment, LED and dot-matrix programs), paste into emu8086 **unchanged**, try to assemble and emulate, and record results.
- **Check specifically:** `SEGMENT PARA PUBLIC 'CODE'`, `ASSUME CS:`, `ORG 1000H`, `END START`, `ENDS`, `OFFSET`, `BYTE PTR CS:[SI]`, `OUT` with immediate ports `19H`, `1FH`, `1EH`, `18H`, `1AH`, `1CH`, `INT 3` behaviour, the code load address used by emu8086, and whether the `FONT` table placement (`DB` after code) works.
- **Bring back:** `docs/spike-reports/S2.md` using this table per program: *file | assembles unchanged? | error text | minimal edit | runs correctly?*
- **Repository hygiene (resolves F10):** record **edits only**, as before/after lines, never whole programs. Keep any working copies of lab programs in a git-ignored `private/` folder. Repository samples are written independently (PH2-06, PH3-04).
- **Acceptance:** every lab program listed; edits needed (if any) are minimal and described.
- **Traces:** S2, Q-02, SC-1.

#### PH0-08 — S3: device mechanism checks · [HUMAN] · size M
- **Goal:** verify how devices are listed and launched (Q-08, Q-09, Q-10).
- **Depends on:** PH0-01.
- **Procedure:**
  1. Copy any small `.exe` (e.g. a renamed `PortLogger.exe` called `ActivationTest.exe`) into the `devices` folder; restart emu8086; confirm it appears in **Virtual Devices**.
  2. Put the text `ActivationTest.exe` in a **comment** in a program; emulate; note whether the device starts. Repeat with the name inside a **string**.
  3. Run emu8086 as a normal user and as administrator; note whether `c:\emu8086.io` is created and writable in each case.
  4. List which ports each built-in device uses (check emu8086's help or the `DEVELOPER` folder sources, for reading only); highlight overlaps with `00H–1FH`.
  5. Check what happens when two instances of the same device are launched (relevant to FR-SYS-03).
6. **S1 supplement.** Copy the source of the shipped `Printer` and `Simple` devices (and any sample that touches `c:\emu8086.hw`) from `c:\emu8086\DEVELOPER` into a git-ignored `private/sample-devices/` folder (emu8086 is commercial; do not commit it). Note what each does about polling strategy, stream handling, handshakes and the `.hw` file. The agent summarises the findings in PH0-06; nothing is pasted into the repository.
- **Bring back:** `docs/spike-reports/S3.md`.
- **Acceptance:** each step answered yes/no with detail.
- **Traces:** S3, Q-08, Q-09, Q-10, FR-SYS-01/02/03, R-05, R-09.

#### PH0-09 — S4: real-kit reference capture · [HUMAN] · size M · *deferrable*
- **Goal:** observe the real MDA-8086 so the model matches it (Q-03, Q-05, Q-06, Q-07, Q-14, Q-15). Needs lab access. Programs are authored by the agent in PH3-04, PH4-07, PH5-01 and handed over; this task is the checklist.
- **Capture list** (photo or short video each, with notes):
  1. **After RES, before any program:** state of 7-segment, LEDs, dot matrix, LCD banner. Photograph the keypad and write its exact key labels and layout (Q-15).
  2. **7-segment:** a program that only does `OUT 1FH,80H` then loops (expected: all segments lit, if the 8255 clears latches and segments are active-low); a program writing `7FH` to port A (expected: dp only); count digits visible (Q-06).
  3. **LEDs:** a program lighting each of the four in turn; confirm order R1, G, Y, R2.
  4. **Dot matrix probes** (PH3-04): single scan line, single data line, corner LED, red-only, both colours.
  5. **LCD probes** (PH4-07): write `X` with **no** initialisation (does it appear, i.e. does the monitor leave the LCD initialised?); initialise then print text; read status (BF).
  6. **Keypad probe** (PH5-01): loop `IN AL,01H` and show the byte on LEDs/7-segment; record codes per key, behaviour on release, and whether `OUT 01H,<v>` changes the register.
- **Bring back:** `docs/spike-reports/S4.md` with media and answers.
- **Acceptance:** every item answered or marked "not testable".
- **Traces:** Q-03, Q-05, Q-06, Q-07, Q-14, Q-15, FR-DM-04, FR-LCD-10, FR-KP-02.

#### PH0-10 — S5: obtain the MDA-Win8086 manual · [HUMAN] · size S · *deferrable*
- **Goal:** find the authoritative description of the keypad protocol, LCD initialisation, schematics and jumper settings.
- **Procedure:** obtain the full manual (the CD that ships with the kit, the department, or the third-party copy listed in the PRD Appendix B). Note the page numbers of: keyboard experiments (display the pressed key on the LCD), LCD experiment, 8255/7-segment/LED/dot-matrix schematics, jumper setup figure.
- **Bring back:** `docs/spike-reports/S5.md` containing **page references and paraphrased findings only** (no verbatim copying, per PRD §15), plus the manual file location for the owner's private use.
- **Acceptance:** Q-03/Q-04/Q-06 each marked answered, partially answered or open.
- **Traces:** S5, Q-03, Q-04, Q-06.

#### PH0-11 — **Gate G0**: go/no-go and scope decision · [BOTH] · size S
- **Goal:** choose what gets built, using measured data.
- **Depends on:** PH0-06, PH0-07, PH0-08.
- **Decision matrix** (defaults; the human may override with a written reason):

| Observation (from S1) | Outcome | Plan consequence |
|---|---|---|
| Static ports (scenario E) captured 100% at default speed | Plan A viable for 7-segment and LEDs | Proceed with PH1/PH2 |
| Matrix (D): ≥ 95% of column steps seen with `spin`, ≥ 80% with `1ms` at default speed | Matrix feasible | Proceed with PH3; tune in PH3-05 |
| Matrix (D): < 50% of steps seen even with `spin` | Matrix unreliable under file polling | Mark matrix "best-effort", or use Plan B for the matrix (Appendix H) |
| LCD (C): identical consecutive writes invisible **and** no per-write side-channel in `.hw`/metadata | LCD stream cannot be decoded reliably | Choose one: (i) ship LCD as **best-effort** with documented limits; (ii) Plan B for LCD; (iii) drop LCD from v1; (iv) ship with the **opt-in write-sequence include** described in PH4-06 (breaks the write-once rule for LCD text, so it needs a written human decision). Record the choice |
| LCD (C): a per-write toggle/counter exists in `.hw` | Side-channel available | Implement in PH4-06 |
| F: file bulk-zeroed on start/stop/reload | Emulator-reset detection needed | Enable PH5-03 detection by default |
| F: file never rewritten in bulk | Detection not needed | Keep disabled; document |
| A/B: smallest N for ≥ 99% capture is above what lab programs use | LCD/other streams need delay-friendly guidance | Add guidance to the user guide |

- **Outputs:** a dated entry in `docs/decisions.md` titled "G0" stating the chosen outcome, any scope changes, and edits to the PRD (§3.2, §12, §13); updates to this plan (tick/untick, add or remove tasks) committed before PH1 starts.
- **Acceptance:** the human signs off the entry; if **Plan B** is chosen for the whole product, **stop** this plan and write a new one (Appendix H gives the outline).

---

# PH1 — Skeleton and infrastructure (M1)

**Purpose:** a clean, tested skeleton that connects to `c:\emu8086.io`, polls it, logs what it sees and survives every failure in PRD §11.2 IT-7, with **no peripheral models yet**. **Entry condition:** Gate G0 passed and recorded. **Pass targets:** FR-SYS-01, 03, 04, 05, 07, 10; T-06; T-07; IT-7 baseline.

#### PH1-01 — Solution, projects, build settings · [AGENT] · size S
- **Goal:** the full solution builds and tests run, with the layering rule enforced by a test.
- **Depends on:** PH0-11 (G0 recorded).
- **Files:** `Mda8086Kit.sln` (all projects, including the two tools), `Directory.Build.props`, `src/Mda8086Kit.Core`, `src/Mda8086Kit.Io`, `src/Mda8086Kit` (shell), `tests/Mda8086Kit.Core.Tests`, `tests/Mda8086Kit.Io.Tests`, `docs/MDA8086_Virtual_Kit_PRD.md` (back-port of the new IDs).
- **Steps:**
  1. Create the projects with the targets and settings in §5.1–5.2. Shell `AssemblyName` is `MDA8086_Kit`. Add the project references in the §5.2 table; Core references nothing.
  2. Add the packages allowed in §5.3 and nothing else.
  3. Add `CorePurityTests` in Core.Tests. It (a) asserts the Core assembly references no `System.Windows.Forms` or `System.Drawing*`, and (b) scans Core source files for forbidden tokens: `System.Windows.Forms`, `System.Drawing`, `DllImport`, `DateTime.Now`, `Thread.Sleep`, `FileStream`, `File.`, `Application.DoEvents`. This enforces NFR-06 and ground rule 2 mechanically.
  4. Back-port `R-14…R-20`, `Q-13…Q-16`, `T-08…T-09` into the PRD copy under `docs/`, with a dated line in `docs/decisions.md`.
- **Acceptance:** `dotnet build -c Debug` and `-c Release` succeed for the whole solution, Core has zero warnings, `dotnet test` passes (including `CorePurityTests`), and the **empty shell compiles in Release** before any hardware code exists.
- **Traces:** NFR-06, NFR-01, NFR-10, D2, D3.

#### PH1-02 — Core primitives and test doubles · [AGENT] · size M
- **Goal:** the abstractions in §5.6 plus the fixtures every later test uses.
- **Depends on:** PH1-01.
- **Files:** `Core/Abstractions/*`, `tests/Mda8086Kit.Core.Tests/Fixtures/{FakeClock,FakePortSource,RecordingDevice,ReferenceTimelines}.cs`.
- **Steps:**
  1. Implement `IClock`, `PortChange`, `PortBatch`, `IPortSource`, `IDeviceWriteSink`, `IWarningSink`, `IPortDevice`, `ConnectionStatus`, `ConnectionState` exactly as in §5.6. `ConnectionStatus` values: `Connected`, `WaitingForEmu8086`, `AccessDenied`, `Locked`, `PartialRead`, `IoError`, `Stopped`.
  2. `ConnectionState` carries a one-line **message** and a one-line **remedy**. Every non-`Connected` state must answer two questions: what happened, and what should the student do. Example: *Waiting for emu8086. Start emu8086 and run a program that uses the virtual device.* No stack traces in user-facing text.
  3. `FakeClock`: manual `Advance(ms)`, `TicksPerMillisecond = 10_000`. `FakePortSource`: a 256-byte array with `Set(port, value)`, queued failures (`FailNextRead(ConnectionStatus)`), a write log, and a scripted-sequence helper. `RecordingDevice`: records every `OnCpuWrite`, `Tick` and `Reset`.
  4. `ReferenceTimelines`: the fixtures from Appendix B as data (7-segment, LEDs, matrix "A") so tests and the simulator share one source.
- **Acceptance:** Core builds warning-free; a smoke test round-trips values through `FakePortSource`; `PortBatch` has an allocation-free `Clear()`.
- **Traces:** NFR-06, T-06 prerequisites.

#### PH1-03 — `KitConfig` and INI parser · [AGENT] · size M
- **Goal:** a hand-parsed INI configuration, a validated port map and the `AddressDecoder`.
- **Depends on:** PH1-02.
- **Files:** `Core/Config/*`, `Core/Bus/AddressDecoder.cs`, `Io/ConfigFileReader.cs`, `tests/.../Config/*`, a commented sample `MDA8086_Kit.config`.
- **Spec:**
  - File name `MDA8086_Kit.config` beside the exe (PRD §16), INI syntax. Do **not** name it `MDA8086_Kit.exe.config`; .NET claims that name. `KitConfig.Parse(string text)` is pure; `ConfigFileReader` in Io reads the file and never throws (missing file means defaults).
  - Numbers accept `18H`, `0x18` and decimal. Keys are case-insensitive. `;` and `#` start comments.
  - Sections and keys:

```ini
[ports]                 ; defaults equal the real kit (section 5.7)
lcd_instruction = 00H
keypad          = 01H
lcd_status      = 02H
lcd_data        = 04H
cs1_a = 18H
cs1_b = 1AH
cs1_c = 1CH
cs1_ctrl = 1EH
cs2_a = 19H
cs2_b = 1BH
cs2_c = 1DH
cs2_ctrl = 1FH

[poll]
interval_ms    = 1
mode           = timer1ms     ; timer1ms | hybrid | spin
ordering       = control-first ; control-first | ascending
bulk_threshold = 8

[matrix]
window_ms     = 20
gain          = 4.0
smoothing_ms  = 0
orientation   = normal

[lcd]
powerup     = hd44780         ; hd44780 | monitor-like
busy_timing = false
seq_port    =                 ; empty = off; PH4-06 option (iv) suggests 30H

[keypad]
protocol = provisional        ; provisional | off | <verified name after Q-03>

[reset]
detect_emulator = auto        ; auto | on | off
min_ports       = 3           ; ports zeroed together to count as an emulator reset (PH5-03)

[ui]
snapshot_ms = 8               ; minimum time between published snapshots

[log]
verbosity    = info
monitor_rows = 5000
```

  - **Validation** (T-07, FR-SYS-06): every port is within 0–65535; no two functions share a port (overlapping ranges are rejected); an invalid or duplicate `[ports]` section falls back to the **whole default map** with a `ConfigWarning`; an invalid value elsewhere falls back per key. Unknown keys produce a warning. Warnings reach the port monitor, never a crash.
  - **Poll window.** Derived from the map: lowest to highest mapped port inclusive, capped at 256 bytes. The default map yields `00H–1FH` (32 bytes), which meets FR-SYS-05. When `lcd.seq_port` is set, it is included in the span.
  - `AddressDecoder` maps a port to a `PortRole` (`LcdInstruction`, `Keypad`, `LcdStatus`, `LcdData`, `Cs1A…Cs2Ctrl`, `None`) and answers `IsControlClass(port)`: true for both PPI control ports and the LCD instruction port. `BusPoller` uses it for ordering (PH1-04).
- **Tests (T-07):** defaults equal §5.7; overriding a base moves the device; overlap rejected; out-of-range rejected; malformed line ignored with a warning; numeric formats; empty file; the `1CH`/`1EH` discrepancy is **not** altered by any code path.
- **Acceptance:** T-07 green; `KitConfig.Default` is byte-for-byte the §5.7 map.
- **Traces:** FR-SYS-06, T-07, D4.

#### PH1-04 — `BusPoller` · [AGENT] · size L
- **Goal:** deterministic change detection with a documented ordering policy. Pure Core; no thread, no sleep.
- **Depends on:** PH1-02, PH1-03.
- **Files:** `Core/Bus/{BusPoller,PollSettings,PollOutcome,ChangeOrdering}.cs`, `tests/.../Bus/BusPollerTests.cs`.
- **API:**

```csharp
public sealed class BusPoller
{
    public BusPoller(IPortSource source, IClock clock, PollSettings settings, AddressDecoder decoder);
    public PollOutcome PollOnce(PortBatch batch);       // never throws; reuses buffers
    public void NoteDeviceWrite(int port, byte value);  // poll thread only; updates the shadow copy
}
public enum PollOutcome { Unchanged, Changed, Seeded, Disconnected }
```

- **Spec:**
  - One bulk `TryReadRange` per poll into a reusable buffer; compare with the shadow copy; fill the batch. Allocate **only** for actual changes (and even then only into the preallocated batch).
  - **Baseline seeding.** On the first successful read, and again after every reconnect, copy the buffer into the shadow **without** emitting 32 "writes". `PollOutcome.Seeded` tells the controller a reconnect happened (it re-posts device-owned bytes, F9).
  - **Ordering policy (F2).** One timestamp per poll. With `ControlFirst` (default), ports for which `IsControlClass` is true are emitted first, then everything else in ascending port order. With `Ascending`, plain ascending order. **Why not ascending by default:** `OUT 1FH,80H` then `OUT 19H,0C0H` seen in one poll would otherwise apply `19H` first and the mode-set would wipe it. Residual case, documented in `known-limitations.md`: if the program really wrote data *then* control within one poll interval, control-first applies them in the wrong order and the file cannot tell us.
  - **Bulk flag.** If at least `bulk_threshold` ports change in one poll, set `batch.IsBulk` (used by emulator-reset detection, PH5-03). Behaviour is added there; here it is only a flag.
  - **Failure.** A false read yields `PollOutcome.Disconnected` and no events. Reconnect logic lives in the port source, not here.
  - **`NoteDeviceWrite`** sets `shadow[port] = value` so the device's own bytes (LCD status, keypad) never come back as CPU writes.
  - Counters: polls, changes, bulk polls, seeds, max poll duration (ticks).
- **Tests (T-06, with `FakePortSource`):** unchanged poll yields no events; single and multiple changes; ascending and control-first ordering (include the `1FH`/`19H` example); seeding on first read and after a disconnect; a device write is not reported back; a CPU write of the same value as a device write is invisible (documented); bulk flag at exactly the threshold; recovery after injected failures; a zero-allocation assertion for unchanged polls.
- **Acceptance:** T-06 (poller part) green.
- **Traces:** FR-SYS-05, T-06, R-01, R-16, D6.

#### PH1-05 — `EmuIoPortFile` · [AGENT] · size L
- **Goal:** the one and only code that touches `c:\emu8086.io`, resilient and non-destructive.
- **Depends on:** PH1-02.
- **Files:** `Io/EmuIoPortFile.cs`, `tests/Mda8086Kit.Io.Tests/*`.
- **Spec:**
  - Path constant is `@"C:\emu8086.io"` (verbatim string, F5). The constructor accepts a path so tests can use a temp file. Never compile or reference `third_party/io.cs`.
  - Open with `FileMode.Open` (never `Create`, `Truncate` or `OpenOrCreate`), `FileAccess.ReadWrite`, `FileShare.ReadWrite`. Keep **one** `FileStream` open. Do not touch `c:\emu8086.hw` unless S1/G0 showed a role for it. If S1-F shows emu8086 deletes the file, add `FileShare.Delete` and the identity check below.
  - `TryReadRange` does one `Seek(first)` and one `Read`. A short read is a **partial read**: report `PartialRead`, do not use the data.
  - `TryWriteByte` seeks to the port and writes one byte. If the file is shorter than `port + 1`, **refuse** (return false). The file is never extended, truncated or resized (FR-SYS-10); never call `SetLength`.
  - **Identity check** every 500 ms (configurable): if `FileInfo` shows the file missing or with a different creation time, close and reopen. After any reopen the caller sees the change as a reconnect (the poller reseeds).
  - **State machine:**

```
                 open succeeds
 WaitingForEmu8086 ------------> Connected <-----------+
        ^                          |   |                |
        | file missing             |   | transient I/O  | retry succeeds
        +--------------------------+   v                |
                                  (Locked | PartialRead | IoError) --+
 AccessDenied <---- open or read denied (retried slowly; never a crash)
```

  - **Error classification:**

| Condition | Status | Message / remedy (short) |
|---|---|---|
| `FileNotFound`, `DirectoryNotFound` | `WaitingForEmu8086` | Waiting for emu8086. Start emu8086 and run a program that uses the virtual device. |
| `UnauthorizedAccessException` | `AccessDenied` | Cannot open c:\emu8086.io (permission). Give your user read/write access to the file, or run emu8086 and this device as the same user. |
| `IOException` with HRESULT `0x80070020` or `0x80070021` | `Locked` | Cannot open c:\emu8086.io (locked). Close other programs using it, then wait; the device retries. |
| Read returned fewer bytes than requested | `PartialRead` | emu8086 is still creating the file. Wait; the device retries. |
| Any other `IOException` | `IoError` | I/O problem on c:\emu8086.io. See the log; the device retries. |

  - **Retry policy.** Back off 100, 250, 500, then 1000 ms (cap). No tight loops. While disconnected, `TryReadRange` returns false immediately unless the backoff has elapsed. State is retained across outages.
- **Tests (Io.Tests, temp files; skip on non-Windows):** bulk read correctness; single-byte write; missing file; a file held with `FileShare.None` by the test; short file; delete then recreate then reconnect; attempted write beyond the end leaves the length unchanged; never throws on any of these.
- **Acceptance:** all Io tests green; code review confirms no `SetLength`, `FileMode.Create` or `FileMode.Truncate` anywhere.
- **Traces:** FR-SYS-04, FR-SYS-10, NFR-05, R-05, R-06, D1.

#### PH1-06 — `KitController` skeleton, snapshot, port-monitor data · [AGENT] · size L
- **Goal:** the single owner of devices and state, with the snapshot and monitor data the UI needs. Peripheral slices are empty placeholders until their phases.
- **Depends on:** PH1-02, PH1-03, PH1-04.
- **Files:** `Core/Kit/{KitController,KitSnapshot,DeviceWrite,PortMonitorLog,PortMonitorEntry,ResetCoordinator}.cs`, tests.
- **Spec:**
  - `KitController` owns `SyncRoot`, the device list and the `IDeviceWriteSink` queue. `OnPoll(PortBatch)` runs the four-step sequence in §5.5. Locks are short; **no I/O inside the lock** (the poll thread drains the write queue after releasing it).
  - **Snapshot rule.** The model owns mutable state; the UI receives immutable snapshots. `KitSnapshot` holds: `ConnectionState`, `Ports` (read-only copy), `SevenSegmentState`, `LedState`, dot-matrix brightness grids, `LcdDisplayState`, `KeypadState`, recent `Warnings`, counters, `LastChangeTicks` and a `SequenceNumber`. Placeholders are `null` or empty now; each phase fills its slice. Arrays are copied once at creation and exposed read-only.
  - **Publishing.** The poll thread marks the model dirty on any mutation and publishes at most every `ui.snapshot_ms` (default 8 ms) with `Volatile.Write`. Idle polls build nothing. The 60 Hz UI timer reads the latest reference without locking; the latest snapshot wins and the UI never renders individual poll events.
  - **`PortMonitorLog`** (FR-SYS-07): fixed-size ring (default 5,000 rows, `log.monitor_rows`) of `PortMonitorEntry { Ticks, Port, OldValue, Value, Kind, Device, Note }`. `Kind` is `ObservedChange`, `DeviceWrite`, `Warning` or `Status`. Use **"Observed change"**, never "CPU write", unless the model knows the event was device-generated: the file does not carry direction. Also keep per-port counters, last-change ticks (for highlight fading) and the last value. Allocate only for actual changes; notes are constants or formatted lazily on display/export.
  - **Warning channel.** `IWarningSink.Warn(code, message)` de-duplicates by code within a time window so a scanning loop cannot flood the log (used by FR-PPI-07 later).
  - `ResetCoordinator` exists now with the ordered steps as no-ops that later phases fill (PH5-03).
- **Tests:** dispatch reaches the device that `Handles` the port; `Tick` runs before dispatch every poll; control-first order is honoured; the sequence number advances only when something changed; the ring never exceeds its bound; `Reset` reaches every device.
- **Acceptance:** all tests green; a long fake run keeps memory flat.
- **Traces:** FR-SYS-07, NFR-04, NFR-06, FR-SYS-08 (prerequisite).

#### PH1-07 — WinForms shell: `Program`, `MainForm`, `PollLoop` · [AGENT] · size L
- **Goal:** a window that shows live connection status and port values and starts and stops cleanly.
- **Depends on:** PH1-05, PH1-06.
- **Files:** `src/Mda8086Kit/{Program,MainForm,PollLoop,StopwatchClock,NativeMethods,PortMonitorPanel}.cs`, `app.manifest`.
- **Spec:**
  - **`Program`.** Named mutex (`Local\MDA8086_Kit_SingleInstance`) for FR-SYS-03: a second instance exits quietly after bringing the first window forward. Parse `--sim <script>`, `--selftest`, `--perf`. Install top-level exception handlers that log and show a friendly message (NFR-05).
  - **`PollLoop`.** A dedicated background thread (not the thread pool). Calls `timeBeginPeriod(1)` once when polling starts and `timeEndPeriod(1)` on shutdown (P/Invoke lives only in the Shell). Sleep strategy from `poll.mode`: `timer1ms` (default) uses `Thread.Sleep(1)` after `timeBeginPeriod`; `hybrid` sleeps then spins the last 0.5 ms; `spin` is a diagnostic mode that uses a full core and is **never** the default (NFR-03). Each iteration: `PollOnce`, `KitController.OnPoll`, drain device writes to the file, repeat. A catch-all inside the loop logs and continues; after N consecutive unexpected exceptions it shows "Internal error, see log" but keeps running.
  - **Shutdown order** (FR-SYS-10): stop accepting polls (`CancellationToken`), signal `ManualResetEventSlim`, join the thread (2 s timeout), dispose the port source (closes the handle), `timeEndPeriod(1)`, flush logs, detach UI timers and handlers. No `Thread.Abort`, no `DoEvents`.
  - **`MainForm`.** Status bar always shows connection state, the file path in use and the remedy text. A collapsible **port monitor panel** (PRD §10): 32 byte cells in four rows of eight with change highlighting that fades, per-port change counters, a scrolling log with columns *Time, Port, Old, New, Event, Device, Note*, and an Export CSV button (PH1-09). Placeholder panels mark where the LCD, matrix, 7-segment, LEDs and keypad will go. A 60 Hz `Timer` reads the latest snapshot; the UI thread never touches the file.
  - Window title: `MDA-8086 Virtual Kit (unofficial)`.
- **Acceptance:** builds; runs with no emu8086 and shows *Waiting for emu8086* plus its remedy without exceptions; closing the window ends the process; a second launch does not open a second window; `--sim` runs (PH1-08).
- **Traces:** FR-SYS-01, 03, 04, 05, 07, 10, FR-UI-02 (partial), NFR-03, NFR-05.

#### PH1-08 — `ScriptedPortSource` and `--sim` mode · [AGENT] · size M
- **Goal:** drive the whole pipeline without emu8086, for development, tuning and headless tests.
- **Depends on:** PH1-02, PH1-07.
- **Files:** `Core/Simulation/{ScriptedPortSource,SimScripts,CsvReplayPortSource}.cs`, tests.
- **Spec:**
  - `ScriptedPortSource : IPortSource` is clock-driven and pure. It applies timed steps (`t_ms port value`, with `loop` support) to a 256-byte array and returns the **latest value only**, exactly like the real file, so polling loss is reproduced honestly. It also supports injected failures (`InjectFailure(status, duration)`) for fault-injection tests.
  - `SimScripts` generates the Appendix B timelines (7-segment, LED, matrix "A", LCD hello) with delay parameters.
  - `CsvReplayPortSource` rebuilds the file contents over time from the S1 `*_changes.csv` logs. Later phases use it to tune against **real captured timelines** (PH3-05).
  - `--sim <name|file>` switches the shell to `ScriptedPortSource`. The status bar says **SIMULATED** in a distinct colour so it can never be mistaken for a live connection.
- **Acceptance:** `--sim seg_count` shows the monitor bytes cycling; unit tests prove a sim read between two writes sees only the latest value.
- **Traces:** US-7, T-06, S1 tooling reuse.

#### PH1-09 — Logging and CSV export · [AGENT] · size M
- **Goal:** lightweight diagnostics and the port-monitor export, with no logging framework.
- **Depends on:** PH1-06.
- **Files:** `Io/{CsvExporter,FileLog}.cs`, tests.
- **Spec:**
  - `CsvExporter` writes `timestamp,port,value,event_type,device,note`. `timestamp` is ISO-8601 UTC, derived from the wall-clock time recorded once at start plus tick offsets (the only place wall-clock time is used). Quote fields that contain commas or quotes. Export only data, not UI decoration.
  - `FileLog` writes `%LOCALAPPDATA%\MDA8086_Kit\logs\kit.log`, rotating at about 1 MB with two files, levels from `log.verbosity`. Stack traces go to the log only. It never throws, even if the log file is locked.
- **Acceptance:** Io.Tests cover quoting, rotation and a locked log file; the Export CSV button writes a file that opens correctly in a spreadsheet.
- **Traces:** FR-SYS-07, US-7, NFR-05.

#### PH1-10 — M1 verification inside emu8086 · [HUMAN] · size M
- **Goal:** prove the skeleton works against the real emu8086.
- **Depends on:** PH1-07, PH1-09, S3 results.
- **Procedure:**
  1. Copy `MDA8086_Kit.exe` into the `devices` folder found in S3; restart emu8086; confirm it is listed under **Virtual Devices**.
  2. Run `samples/spikes/s1e_static.asm`; confirm the port monitor tracks `19H`, `1BH` and `1FH` live.
  3. **IT-7 baseline:** start the device **before** emu8086; deny then restore file permission; hold the file open from another program; delete and recreate `c:\emu8086.io`; restart emu8086. After each, note the status text and whether the device recovers without restarting.
  4. Close the device and confirm the file is not truncated or resized (compare size before and after).
- **Bring back:** rows in `docs/test-log.md`, status-text screenshots, and the exact device folder path used.
- **Acceptance:** FR-SYS-01/04/05/07/10 observed passing; the device survives every IT-7 baseline case.
- **Traces:** IT-7, SC-5, FR-SYS-01.

#### X1 — Exit review M1 · [BOTH]
- **First coding checkpoint** (before any peripheral UI): the repository builds a clean WinForms exe; instantiates the Core; opens and retries `c:\emu8086.io`; polls 32 bytes on a dedicated thread; shows connection status and live port values; and survives a missing or locked file.
- PRD M1 exit criteria all true (FR-SYS-01/04/05/07/10; T-06; T-07; IT-7).
- `docs/decisions.md` and `docs/questions.md` updated; PRD synced. Tag `m1`.
- The human decides whether to continue to PH2.

---

# PH2 — 8255, 7-segment, LEDs (M2)

**Purpose:** the first real hardware behaviour, driven through the 8255 model so semantics (direction, latch clearing on mode-set) apply uniformly. **Entry condition:** X1 passed. **Pass targets:** T-01, T-02, T-03, IT-1, IT-2 (SC-1 partial).

#### PH2-01 — `Ppi8255` · [AGENT] · size L
- **Goal:** two independent 8255A instances with mode-0 semantics and an explicit pin-state concept.
- **Depends on:** PH1-06.
- **Files:** `Core/Devices/{Ppi8255,PinState}.cs`, `Core/Config/PpiPortMap.cs`, `tests/.../Devices/Ppi8255Tests.cs`.
- **Spec (FR-PPI-01…08, PRD §5.3):**
  - Instances: **CS1** (A `18H`, B `1AH`, C `1CH`, CTRL `1EH`) and **CS2** (A `19H`, B `1BH`, C `1DH`, CTRL `1FH`), built from `PpiPortMap` so the config can move them. The constructor takes the instance name and an `IWarningSink`.
  - **State:** latches A, B, C; directions A, B, C-upper, C-lower; group A and B modes; last control word; `IsConfigured` (false until the first control word).
  - **Power-up / `Reset()`:** all ports input, latches `00H`, `IsConfigured = false`, nothing driven.
  - **Mode-set (D7 = 1):** decode D6–D5 (group A mode), D4 (A direction), D3 (C upper), D2 (group B mode), D1 (B direction), D0 (C lower); store the word; **clear latches A, B and C to `00H`** (FR-PPI-03); set `IsConfigured`.
  - **BSR (D7 = 0):** D3–D1 pick the port C bit, D0 sets or resets it. The latch bit changes only when that bit's nibble is configured as output; BSR on an input nibble is ignored.
  - **Data writes:** a write to A, B or C updates the latch only if that port (or nibble) is output. A write to an input-configured port is **ignored** and does not drive pins (FR-PPI-06). A write before any control word is therefore ignored, and the mode-set clears the latches anyway.
  - **Pin state.** Expose `PinState` for A, B and C: `Value` plus `DrivenMask`. For driven bits `pin = latch`; for undriven bits the value is the external input if one was supplied through `SetExternalInput(port, value)` (FR-PPI-08, P2) and otherwise undriven. Visual models **must** consume pin state, never raw latches or raw port bytes.
  - **Unsupported modes:** if D6–D5 is not `00` or D2 is `1`, decode and store the word, warn once per distinct control word (`PPI-MODE`, FR-PPI-07), treat directions as mode 0, and do not pretend handshake or bidirectional behaviour exists.
  - **Known limitation (F3):** a repeated mode-set with the **same** control byte produces no change in the file, so the latches are not re-cleared. Documented in `known-limitations.md`; irrelevant for the lab programs.
- **Tests (T-01):** all 16 direction combinations (A, B, C-upper, C-lower) through mode-0 control words; mode-set clears latches that were written earlier; power-up state; BSR set and reset for each of the 8 bits, plus BSR ignored on an input nibble; writes to an input-configured port do not drive pins; `DrivenMask` for each C-nibble combination; unsupported modes warn once; a write before any control word is ignored; a CS1 instance never reacts to CS2 ports and vice versa; control words with D7 = 1 and extra bits are decoded per the table in PRD §5.3.
- **Acceptance:** T-01 green; every non-obvious rule carries a `// PRD §5.3` comment (NFR-10).
- **Traces:** FR-PPI-01…08, T-01, R-15.

#### PH2-02 — `SevenSegmentModel` · [AGENT] · size S
- **Goal:** a stateless decode from CS2 port A pin state to eight lit/unlit segments.
- **Depends on:** PH2-01.
- **Files:** `Core/Devices/SevenSegmentModel.cs` (with `SegmentState`), tests.
- **Spec:**
  - Active-low: `0` is lit, `1` is off. `bit0 = a … bit6 = g, bit7 = dp` (PRD §5.4, Q-06 assumption A-2).
  - `SegmentState { A, B, C, D, E, F, G, DecimalPoint }` plus `Driven`. A segment is lit only if its pin is **driven** and low.
  - **Before any control word** (ports are inputs, so nothing is driven) the display is **blank** (assumption for Q-07, marked `// ASSUMPTION(Q-07)`). **After a mode-set** the latch is `00H` and the segments are all lit until the program writes `19H`; this falls out of the PPI model with no special code (FR-7S-03).
  - **No digit special-casing** in the model or the renderer. Digit codes exist only as test fixtures.
- **Tests (T-02):** all ten reference codes (`C0H F9H A4H B0H 99H 92H 82H F8H 80H 90H`) give the expected segment sets; `FFH` is dark; `00H` lights all eight including dp; each single-bit-low value lights exactly that segment; undriven is blank; mode-set then no write is all lit.
- **Acceptance:** T-02 green.
- **Traces:** FR-7S-01, 02, 03, T-02.

#### PH2-03 — `LedBank` · [AGENT] · size S
- **Goal:** the four-LED view of CS2 port B.
- **Depends on:** PH2-01.
- **Files:** `Core/Devices/LedBank.cs` (with `LedState`), tests.
- **Spec:** active-high. `bit0 = R1`, `bit1 = G`, `bit2 = Y`, `bit3 = R2`. Bits 4–7 are ignored (FR-LED-02). A bit lights only if its pin is driven.
- **Tests (T-03):** `01H`, `02H`, `04H`, `08H` light exactly one LED; `0FH` lights all four; values with upper bits set affect only bits 0–3 (for example `F1H` lights R1 only); undriven is off.
- **Acceptance:** T-03 green.
- **Traces:** FR-LED-01, 02, T-03.

#### PH2-04 — Controller wiring for CS2, reset, undriven handling · [AGENT] · size M
- **Goal:** both PPIs live in the controller, the 7-segment and LED views in the snapshot, and a working `ResetAll()` for what exists so far.
- **Depends on:** PH2-01…PH2-03.
- **Files:** `Core/Kit/KitController.cs` (extend), `ResetCoordinator.cs`, `tests/.../Kit/KitControllerPeripheralTests.cs`.
- **Steps:**
  1. Register `cs1` and `cs2` with the `AddressDecoder` map. Derive `SevenSegmentState` and `LedState` from CS2 pin state when the snapshot is built (views, not stored state).
  2. Implement the first steps of `ResetCoordinator`: reset both PPIs. Later phases add the rest (PH5-03).
  3. Replay the Appendix B fixtures through `FakePortSource` + `BusPoller` + `KitController` and assert the snapshot after each step. This is IT-1 and IT-2 as a headless test: `1FH←80H` shows all segments lit, `19H←FFH` shows dark, `1BH←01H…08H` walks R1 → G → Y → R2.
- **Acceptance:** the headless replay tests are green; `Reset` returns the snapshot to the power-up state.
- **Traces:** FR-7S-03, FR-SYS-08 (partial), IT-1/IT-2 (headless).

#### PH2-05 — `SevenSegControl`, `LedControl`, layout · [AGENT] · size M
- **Goal:** the first custom-drawn controls and the kit-style main layout.
- **Depends on:** PH2-04.
- **Files:** `src/Mda8086Kit/Controls/{SevenSegControl,LedControl}.cs`, `KitLayout.cs`, `MainForm` layout changes.
- **Spec:**
  - Double-buffered (`OptimizedDoubleBuffer | AllPaintingInWmPaint | UserPaint`), GDI+, anti-aliased. Controls take a snapshot slice and paint; they hold no model.
  - **`SevenSegControl`:** seven polygon segments and a dp dot from `SegmentState`. Unlit segments are drawn faint (not invisible) so the physical shape stays readable. Does not decode port bytes.
  - **`LedControl`:** four indicators with **permanent text labels** `R1`, `G`, `Y`, `R2` (FR-LED-03, NFR-09). A lit LED is drawn filled with a highlight ring and its label in bold, so state never depends on colour alone. Set `AccessibleName` and `AccessibleDescription` on each.
  - **Layout:** follow PRD §10: status bar on top, LCD and matrix areas (placeholders until PH3 and PH4), then 7-segment and LEDs, keypad at the bottom. `KitLayout` computes rectangles proportionally from `ClientSize` so scaling needs no rewrite later (FR-UI-03).
  - Repaint only when the snapshot `SequenceNumber` changes.
- **Acceptance:** builds; `--sim seg_count` and `--sim led_chase` render correctly; no flicker at the 60 Hz timer; the UI thread never blocks on I/O.
- **Traces:** FR-7S-01, FR-LED-03, FR-UI-01 (partial), FR-UI-02, NFR-09.

#### PH2-06 — Original sample programs `seg_count.asm`, `led_chase.asm` · [AGENT] · size M
- **Goal:** emu8086-ready programs that produce the **same port-write timelines** as the lab's reference programs, written independently (ground rule 3).
- **Depends on:** PH0-07 (header style), PH2-04.
- **Files:** `samples/asm/seg_count.asm`, `samples/asm/led_chase.asm`, `samples/asm/README.md`.
- **Spec:**
  - `seg_count.asm`: `OUT 1FH,80H`; then forever, for each digit code `C0H … 90H`: `OUT 19H,<code>`, then a software delay (`MOV CX,<DELAY>` / `LOOP`).
  - `led_chase.asm`: `OUT 1FH,80H`; `OUT 19H,0FFH`; then forever `OUT 1BH,01H / 02H / 04H / 08H`, each followed by the delay.
  - Delay counts are named constants with comments. Each file has a header comment giving purpose, the port-write timeline, the expected visual result and the line `; MDA8086_Kit.exe`, so S3's auto-activation test (FR-SYS-02) can use it.
  - **Header style:** use whichever header S2 found assembles in emu8086 *and* is accepted by the lab toolchain. If no single header does both, ship two clearly named variants and state the difference in `README.md` (SC-1 wording).
  - `README.md`: a table of file, purpose, timeline, expected visual, delay constants and tested speeds (blank until the test log fills it). No manual text and no lab program is reproduced.
- **Acceptance:** the files assemble in emu8086 (confirmed by the human in PH2-07); timelines match Appendix B exactly.
- **Traces:** FR-SYS-02, SC-1, IT-1, IT-2, PRD §15.

#### PH2-07 — IT-1 and IT-2 at three speeds · [HUMAN] · size M
- **Goal:** verify the 7-segment and LEDs against real emu8086.
- **Depends on:** PH2-05, PH2-06.
- **Procedure:** for each of slowest, default and fastest emu8086 speed: run the sample `seg_count`, then the owner's actual lab program (with only the edits `S2.md` lists); then `led_chase` and its lab equivalent. Note flicker, stray segments, missed digits or LED states, and visible duration per digit.
- **Bring back:** `docs/test-log.md` rows (test ID, date, OS, emu8086 version, device build, speed, result, notes). If S4 has been done, also the real kit's after-RES state for Q-07.
- **Acceptance:** IT-1 shows digits 0→9 cycling with no stray segments; IT-2 shows R1 → G → Y → R2 with the 7-segment dark; SC-3 (no missed visible changes on static outputs) assessed per speed.
- **Traces:** IT-1, IT-2, SC-1 (partial), SC-3.

#### X2 — Exit review M2 · [BOTH]
- T-01, T-02, T-03 green; IT-1 and IT-2 pass at three speeds; SC-1 partial recorded.
- `Q-06` and `Q-07` status updated (answered, working assumption kept, or open) with evidence.
- Docs and PRD synced. Tag `m2`.

---

# PH3 — Dot matrix (M3)

**Purpose:** the most timing-sensitive model. A multiplexed display is a time integral, not "the last write". **Entry condition:** X2 passed and the G0 matrix outcome recorded. **Pass targets:** T-04, IT-3 at three speeds (SC-1 complete), Q-05 closed or narrowed.

#### PH3-01 — `DotMatrixModel` pin decode and orientation · [AGENT] · size M
- **Goal:** instantaneous lit state from CS1 pins, and orientation as a transform rather than baked-in coordinates.
- **Depends on:** PH2-01.
- **Files:** `Core/Devices/{DotMatrixModel,DotMatrixOrientation}.cs`, tests.
- **Spec (FR-DM-01, 02, 04, 05):**
  - CS1 port A = **red** data, active-low; port B = **green** data, active-low; port C = **scan line**, one-hot, active-high. For scan line `k` (port C bit) and data bit `j`:

```
red_lit   = (C[k] == 1) AND (A[j] == 0)
green_lit = (C[k] == 1) AND (B[j] == 0)
both lit  -> orange/yellow (one documented combined state)
```

  - Evaluate in **logical space** `(k, j)`, then apply the orientation transform to get screen coordinates. Default (Q-05 assumption): **port C bit 0 is the left-most column and port B bit 7 is the top row**, so `(col, rowFromTop) = (k, 7 − j)`.
  - `DotMatrixOrientation` has eight values over `(c, r)`: `Normal (c,r)`, `FlipH (7−c,r)`, `FlipV (c,7−r)`, `Rotate180 (7−c,7−r)`, `Rotate90 (7−r,c)`, `Rotate270 (r,7−c)`, `Transpose (r,c)`, `AntiTranspose (7−r,7−c)`.
  - Pins that are not driven light nothing (Q-07 assumption). Port C undriven means no scan line is selected.
  - **Steady (non-scanned) patterns** work with no special case: all scan lines high plus one data bit low lights a full row (FR-DM-05).
- **Tests (T-04, part 1):** truth table of the equation; red only, green only, both; undriven pins; corner probe `(k=0, j=7)` lands at top-left for `Normal` and at the expected corner for each of the other seven transforms; a steady all-lines-high pattern.
- **Acceptance:** tests green; orientation lives in exactly one function.
- **Traces:** FR-DM-01, 02, 04, 05, T-04, Q-05.

#### PH3-02 — Persistence integrator · [AGENT] · size L
- **Goal:** turn a scanned display into a stable image by integrating on-time.
- **Depends on:** PH3-01, PH1-06.
- **Files:** `Core/Devices/PersistenceIntegrator.cs`, tests.
- **Spec (FR-DM-03, PRD §9.3):**
  - Never display "the last scanned column". That is explicitly incorrect for a multiplexed device.
  - Keep `long` on-time accumulators for every LED and colour (64 red, 64 green). On each `Tick(t)`, add the interval `t − lastTick` to every LED that was lit **before** this poll's changes (the controller ticks first, see §5.5).
  - Window `W` (default 20 ms, `matrix.window_ms`). When the elapsed time in the window reaches `W`, commit `duty = onTime / W`, clamp to `[0,1]`, publish, and zero the accumulators. An interval that spans more than one window is split proportionally. No per-tick allocation.
  - **Display brightness** = `clamp(duty × gain)`, `matrix.gain` default `4.0` (assumption; tuned in PH3-05) so a 1-in-8 scan reads as clearly lit while a steady pattern clamps to 1. Optional smoothing (`matrix.smoothing_ms`, default 0 = off) applies an exponential moving average between committed windows. Both `Duty` (raw) and `Display` arrays are available; tests assert on `Duty`.
  - `Reset()` clears accumulators and the committed grid so stale accumulation cannot ghost after RES.
- **Tests (T-04, part 2):** exact fractions with `FakeClock` (one LED lit for 1 ms of a 20 ms window gives `0.05`); the reference "A" timeline gives each lit LED a duty equal to its share of the scan; window rollover including a spanning interval; steady pattern gives duty `1.0`; gain and clamp; smoothing converges; reset leaves no ghost; no allocation per tick.
- **Acceptance:** T-04 green with exact integer-tick arithmetic.
- **Traces:** FR-DM-03, FR-DM-06, T-04, R-04, R-12.

#### PH3-03 — CS1 wiring and `DotMatrixControl` · [AGENT] · size M
- **Goal:** the matrix in the snapshot and on screen.
- **Depends on:** PH3-02, PH2-05.
- **Files:** `KitController` (extend), `KitSnapshot.DotMatrixState`, `Controls/DotMatrixControl.cs`.
- **Spec:**
  - The controller wires `cs1` pins into `DotMatrixModel` and ticks the integrator in step 1 of the per-poll sequence. The snapshot carries 64 red and 64 green display brightness values (orientation applied, origin top-left, row-major) plus the current orientation.
  - `DotMatrixControl` draws an 8×8 grid. Off is dark grey. Red and green scale intensity by brightness; both together blend to amber. Each cell has a tooltip such as *col 3, row 7: green 85% (CS1 C bit 3, B bit 7)* so state is not conveyed by colour alone (NFR-09, PRD §10).
  - Reads the latest snapshot only; double-buffered.
- **Acceptance:** `--sim matrix_a` shows a stable green "A" in the headless fixture and on screen; no flicker.
- **Traces:** FR-DM-01…06, FR-UI-01.

#### PH3-04 — Sample programs and orientation probes · [AGENT] · size M
- **Goal:** the dot-matrix sample plus the **S4 probe pack** the human runs on the real kit (PH0-09). All original code.
- **Depends on:** PH0-07, PH3-03.
- **Files:** `samples/asm/matrix_a.asm`, `samples/probes/*.asm`, `samples/probes/README.md`.
- **Programs:**

| File | Behaviour | Answers |
|---|---|---|
| `matrix_a.asm` | `OUT 1EH,80H`; `OUT 18H,0FFH`; forever, for `i = 0…7`: `OUT 1AH,font[i]`, `OUT 1CH,(1<<i)`, delay `COLDELAY` (parameter). Font `FF C0 B7 77 77 B7 C0 FF`. | IT-3, SC-1 |
| `probe_matrix_column.asm` | `OUT 1CH,01H`; `OUT 1AH,00H`; red off. A full column lights. | which axis port C selects (Q-05) |
| `probe_matrix_row.asm` | `OUT 1CH,0FFH`; `OUT 1AH,7FH`. A full row lights. | which edge is "top" for B bit 7 |
| `probe_matrix_corner.asm` | `OUT 1CH,01H`; `OUT 1AH,7FH`. One LED lights. | corner and origin |
| `probe_matrix_red.asm` | the "A" scan through the red path (`18H` font, `1AH←FFH`) | red wiring |
| `probe_matrix_both.asm` | `A` and `B` low on the same lines | amber appearance |
| `probe_7seg_initial.asm` | `OUT 1FH,80H` then idle | all segments lit after mode-set? (Q-07) |
| `probe_7seg_dp.asm` | `OUT 19H,7FH` | is dp wired? how many digits? (Q-06) |
| `probe_led_order.asm` | each LED in turn, held long | confirm R1, G, Y, R2 order |

  Every file has a header comment: purpose, expected result in the virtual kit, what to observe on the real kit, and which `Q-` it answers. Where a reference program exists, the probe reproduces its **timeline**, not its text.
- **Acceptance:** each file documents its port-write timeline; the human confirms they assemble (PH3-06).
- **Traces:** S4, Q-05, Q-06, Q-07, FR-DM-04.

#### PH3-05 — Degradation and tuning harness · [AGENT] · size L
- **Goal:** choose the integrator defaults with evidence, and quantify how the display degrades when polling loses writes.
- **Depends on:** PH3-02, PH1-08, S1 data from PH0-06.
- **Files:** `tools/MatrixTuner` (net8.0 console, not shipped; add to the solution), `docs/spike-reports/matrix-tuning.md`.
- **Spec:**
  - Feed the "A" timeline, and **real S1 scenario D logs** through `CsvReplayPortSource`, into `BusPoller` + `KitController` on a `FakeClock`.
  - Sweep poll interval (0.5, 1, 2, 5, 15 ms; 15 ms is Windows' default timer), column delay (emu8086 speeds), window `W`, gain and smoothing.
  - Report: columns observed out of 8; mean absolute brightness error against the expected grid (Appendix B); **flicker** (standard deviation of a lit cell's brightness across consecutive windows); **ghost** (brightness in cells that should be dark).
  - Recommend defaults. If fixed windows flicker at slow emu8086 speeds, try smoothing first, and only then an automatic window sized to the detected scan period. Record what was tried.
- **Proposed targets** (adjust against G0 data, in writing): at default speed with `1ms` polling, lit-cell error ≤ 0.15 and ghost ≤ 0.05.
- **Acceptance:** the report exists with tables traceable to the logs; defaults in `KitConfig.Default` match its recommendation; any gap against FR-DM-06 is stated plainly.
- **Traces:** FR-DM-03, FR-DM-06, R-04, R-12, R-01.

#### PH3-06 — IT-3 and orientation probes on the real kit · [HUMAN] · size M
- **Goal:** SC-1 complete, and Q-05 answered.
- **Depends on:** PH3-04, PH3-05.
- **Procedure:** (1) in emu8086 at three speeds, run `matrix_a` and the lab's dot-matrix program; judge stability and ghost columns. (2) At the lab, with the jumper set as the manual says, run the probe pack on the real kit and photograph each result. Note which physical axis each port selects and where the origin is.
- **Bring back:** `docs/test-log.md` rows; `S4.md` matrix section; a recommendation for `matrix.orientation`.
- **Acceptance:** IT-3 shows a stable green "A" with no flicker or ghost columns at three speeds, or the shortfall is documented; Q-05 answered or narrowed; the default orientation is updated if the kit disagrees with the PRD.
- **Traces:** IT-3, SC-1, Q-05, FR-DM-04.

#### X3 — Exit review M3 · [BOTH]
- T-04 green; IT-3 passes at three speeds; SC-1 complete; tuning report committed.
- The G0 matrix outcome ("feasible", "best-effort") is reflected in the user guide.
- Docs and PRD synced. Tag `m3`.

---

# PH4 — LCD (M4)

**Purpose:** the 16×2 HD44780-style display. The model is straightforward; the **transport** is the risk (F1). **Entry condition:** X3 passed and the G0 LCD outcome recorded. **Pass targets:** T-05, IT-4. The model is hardware-faithful only to the extent Q-04 is resolved; until then it is an HD44780 *assumption*, and docs must say so.

#### PH4-01 — LCD 5×8 font table with provenance · [AGENT] · size M
- **Goal:** a legible glyph table whose origin is recorded (R-11, PRD §15).
- **Depends on:** PH1-01.
- **Files:** `Core/Devices/LcdFont.cs`, `docs/font-provenance.md`, `docs/lcd-font-sheet.txt`, tests.
- **Spec:**
  - 5×8 bitmaps for printable ASCII `20H–7EH` (95 glyphs), 5 bits per row, authored **from scratch** for this project. Do not copy from any datasheet, font file or other implementation.
  - `docs/font-provenance.md` states: authored for this project by the coding agent on the commit date; not derived from any third-party table; shapes follow ordinary 5×7 conventions; the real kit's character ROM variant is unknown (Q-04), so glyphs are **not** claimed to be pixel-identical to an HD44780 ROM. If the human prefers a clearly licensed font, replace the table and record its licence here.
  - **Code policy (FR-LCD-05):** `00H–0FH` render CGRAM glyph `code & 7` (HD44780 mirrors 08H–0FH onto 00H–07H); `20H–7EH` render the table; every other code renders a **placeholder box** glyph. Document the policy in the user guide.
- **Tests:** every row uses only bits 0–4; space is blank; all 95 glyphs are pairwise distinct (catches copy-paste mistakes); spot checks of `A`, `0`, `-`; a test regenerates `lcd-font-sheet.txt` so the human can review the table visually.
- **Acceptance:** tests green; the human has reviewed the font sheet.
- **Traces:** FR-LCD-05, R-11, PRD §15.

#### PH4-02 — `Hd44780Lcd` model · [AGENT] · size L
- **Goal:** a deterministic, UI-free HD44780-style state machine.
- **Depends on:** PH4-01, PH1-06.
- **Files:** `Core/Devices/{Hd44780Lcd,LcdPowerUpState}.cs`, `LcdDisplayState`, `tests/.../Devices/Hd44780LcdTests.cs`.
- **State (FR-LCD-01…08):** DDRAM 80 bytes; CGRAM 64 bytes (eight 5×8 glyphs); address counter; address mode (DDRAM or CGRAM); entry mode (`I/D`, `S`); display control (`D`, `C`, `B`); display-shift offset (0–39); function-set fields (`DL`, `N`, `F`).
- **Instruction decode** on port `00H`. Decode by the **highest set bit**, as the real chip does; the ranges in PRD §5.7 follow from it:

| Highest set bit | Instruction | Effect |
|---|---|---|
| 7 | Set DDRAM address (`80H–FFH`) | AC = low 7 bits; DDRAM mode |
| 6 | Set CGRAM address (`40H–7FH`) | AC = low 6 bits; CGRAM mode |
| 5 | Function set (`20H–3FH`) | store `DL` (bit 4), `N` (bit 3), `F` (bit 2); no visual change |
| 4 | Cursor / display shift (`10H–1FH`) | bit 3 `S/C`: 0 moves the cursor (AC ±1), 1 shifts the display; bit 2 `R/L`: 0 left, 1 right |
| 3 | Display on/off (`08H–0FH`) | `D` bit 2, `C` bit 1, `B` bit 0 |
| 2 | Entry mode (`04H–07H`) | `I/D` bit 1, `S` bit 0 |
| 1 | Return home (`02H`) | AC = 0, shift offset = 0, DDRAM unchanged |
| 0 | Clear display (`01H`) | DDRAM filled with `20H`, AC = 0, shift offset = 0, `I/D` = 1 (`S` unchanged) |

- **DDRAM mapping:** line 1 is `00H–27H` (visible window `00H–0FH`), line 2 is `40H–67H` (visible `40H–4FH`). The model keeps the full 80 bytes. Addresses in the gaps (`28H–3FH`, `68H–7FH`) discard writes.
- **Data writes (port `04H`):** in DDRAM mode write at AC; in CGRAM mode write the low 5 bits at the CGRAM address. Then AC moves by `I/D`. If entry `S = 1`, the display also shifts (offset `+1` when `I/D = 1`, `−1` otherwise, modulo 40). CGRAM addresses wrap modulo 64.
- **Address wrap at line ends is not specified by the PRD.** Assume AC increments and decrements modulo 80 (`0x00–0x7F` space) without skipping, mark `// ASSUMPTION(Q-16)`, and add **Q-16** to `docs/questions.md`. Revisit after S4.
- **Visible rendering:** cell `(line, c)` shows DDRAM at `base(line) + ((offset + c) mod 40)`. The cursor shows at the AC position when it falls in the visible window; blink timing is **not** in the model (the control toggles a phase). Display off blanks the output but keeps DDRAM.
- **Status read (port `02H`):** `BF = 0` always (P0), bits 6–0 = AC. Realistic busy timing is P2 (PH4-05). Data reads from `04H` are not modelled (Q-12); mark the assumption.
- `LcdDisplayState`: 32 character codes, CGRAM copy, `DisplayOn`, `CursorOn`, `BlinkOn`, cursor row/column (or none), AC, and the power-up mode in force.
- **Tests (T-05):** init `38H, 0CH, 06H, 01H`; `HELLO` at `00H` and `WORLD` at `40H`; clear; home; entry mode increment and decrement; display shift left and right with the window moving; set DDRAM across the line boundary; display, cursor and blink flags; CGRAM write then render of a custom glyph (the five-step sequence: set CGRAM address, write patterns, set DDRAM address, write code `00H`, rendered glyph equals the stored pattern); status read gives BF 0 plus AC; highest-bit decode for every code `00H–FFH` (table-driven); unknown function-set bits do not crash.
- **Acceptance:** T-05 green; assumptions are comment-marked and listed in `docs/assumptions.md`.
- **Traces:** FR-LCD-01…08, T-05, Q-04, Q-16.

#### PH4-03 — LCD controller wiring and status publishing · [AGENT] · size M
- **Goal:** the LCD in the controller, with the device-owned status byte handled safely (F9).
- **Depends on:** PH4-02.
- **Files:** `KitController` (extend), `KitSnapshot.LcdDisplayState`, tests.
- **Spec:**
  - `Hd44780Lcd : IPortDevice` handles `00H`, `02H` and `04H` from `PortMap`.
  - **Status publishing.** After any model change that alters AC or BF, post `02H = status` through `IDeviceWriteSink`; the poll thread writes it and calls `NoteDeviceWrite`. Coalesce to the latest value once per poll.
  - **Stale-byte rule (F9):** on connect (a `Seeded` outcome), after RES and at startup, post `02H = 00H` **before anything else**. A stale value with bit 7 set would hang any program that waits for the busy flag.
  - A change observed on `02H` that the device did not post (for example emu8086 zeroing the file) is logged as *Observed change on a read-only port* and the device re-posts its current status.
  - Publish `LcdDisplayState` in the snapshot.
- **Tests:** an AC change posts exactly one status write; connect posts `02H = 00H` first; a stray change on `02H` is repaired; the status write is never echoed back as a CPU write.
- **Acceptance:** tests green; `--sim lcd_hello` fills the model.
- **Traces:** FR-LCD-04, F9, R-20.

#### PH4-04 — `LcdControl` · [AGENT] · size M
- **Goal:** a legible 16×2 display control.
- **Depends on:** PH4-03, PH4-01.
- **Files:** `Controls/LcdControl.cs`.
- **Spec:**
  - 16×2 grid of 5×8 pixel cells with gaps, dark pixels on a green-tinted background (PRD §10). Scale pixels to the control size with nearest-neighbour drawing so text stays crisp at any window size.
  - Glyphs come from `LcdFont` and the CGRAM copy in the snapshot. Placeholder box for unprintable codes.
  - Cursor is an underline on the bottom pixel row; blink alternates a full block with the character on a UI-timer phase (about 500 ms). The phase lives in the control, not the model.
  - **Display off** draws a blank panel with a faint label "LCD off (not initialised)" so state is also communicated by text (NFR-09). `AccessibleDescription` exposes the two text lines.
  - Hover tooltip (P2): address counter and flags.
- **Acceptance:** `--sim lcd_hello` shows `HELLO` and `MDA-8086`; custom characters render; no flicker.
- **Traces:** FR-LCD-01, 05, 06, NFR-09, FR-UI-01.

#### PH4-05 — Power-up state option (and optional busy timing) · [AGENT] · size M
- **Goal:** resolve F6 with a setting, and optionally model the busy flag.
- **Depends on:** PH4-02.
- **Files:** `Hd44780Lcd`, `LcdPowerUpState`, `KitConfig` (`[lcd]`), tests.
- **Spec:**
  - `lcd.powerup = hd44780` (PRD default, FR-LCD-10): display off, cleared, 8-bit, entry mode increment, AC 0. `lcd.powerup = monitor-like`: the same but display **on**, cursor off, blink off (the state the real kit's monitor ROM leaves, F6). The default stays `hd44780` until S4 answers Q-14; then the decision and a dated line go into `docs/decisions.md`. No monitor banner (non-goal).
  - **Optional busy timing** (FR-LCD-09, P2, `lcd.busy_timing`, default **false**): after an instruction, BF reads 1 for about 40 µs, and about 1.5 ms after clear or home, using the injected `IClock`. Post the status byte on both edges. The 1 ms poll cannot resolve 40 µs, so only the clear/home window is observable; document that.
  - Unit tests drive the busy window with `FakeClock`.
- **Acceptance:** both power-up modes tested for initial state and for RES; busy timing tested only when enabled.
- **Traces:** FR-LCD-09, FR-LCD-10, Q-14, R-18.

#### PH4-06 — Conditional LCD stream mitigation per G0 · [AGENT] · size M · *conditional*
- **Goal:** implement whichever mitigation the **G0 decision** chose for LCD text. If G0 found the LCD fully reliable, record "not needed" in `docs/decisions.md` and stop.
- **Depends on:** PH0-11 (G0), PH4-03.
- **Why this exists (F1):** the file holds a value, not an event. Two consecutive writes of the same byte to `04H` (the two `L`s of `HELLO`) are indistinguishable from one write, **at any poll speed**. Fast bursts can also lose characters.
- **Options by G0 outcome:**

| G0 outcome | Work |
|---|---|
| (i) best-effort | Add a port-monitor warning (`LCD-STREAM`) when `04H` changes in at least 3 consecutive polls ("writes may be arriving faster than the device can see them"). Document in the user guide that doubled letters can drop and that a delay of at least the smallest "99% capture" N from S1-B should separate characters. Sample programs insert such delays. |
| side channel found in `.hw` or file metadata | Implement a decoder per the S1 report so every write is counted, including repeats. |
| (iv) opt-in write-sequence include | Author `samples/include/mda_lcd.inc` with a macro `LCD_PUTC` that writes the character to `04H` and then toggles a counter on a user-range port (`lcd.seq_port`, suggested value `30H`, empty by default). Extend the poll window to cover that byte. When `lcd.seq_port` is set, `Hd44780Lcd` accepts a data write **only when the counter changes**, using the data byte at that moment, so repeats work. Default **off**. Bursts faster than the poll are still lost; say so. |
| (ii)/(iii) Plan B or drop LCD | No work here; follow Appendix H or remove the LCD tasks from scope with a decision entry. |

- **Honesty rules:** option (iv) breaks "write once, run on both" for LCD text, so it needs a written human decision and a note in `known-limitations.md`. It must be confirmed in S4 that `OUT 30H,AL` is harmless on the real kit (the user range is unused, but verify). Identical consecutive **instructions** (for example two `14H` cursor moves) are affected the same way; the guide says so.
- **Acceptance:** the chosen option is implemented, tested against `ScriptedPortSource` scenarios (including a doubled letter), and described in the user guide; or the "not needed" entry exists.
- **Traces:** F1, R-14, R-01, FR-LCD-02, FR-LCD-03, D5.

#### PH4-07 — Sample program `lcd_hello.asm` · [AGENT] · size S
- **Goal:** IT-4 and the LCD half of the S4 probe pack, all original.
- **Depends on:** PH4-03, PH3-04 (conventions).
- **Files:** `samples/asm/lcd_hello.asm`, `samples/asm/lcd_hello_seq.asm` (only if G0 chose option (iv)), `samples/probes/probe_lcd_*.asm`.
- **Spec:**
  - `lcd_hello.asm`: write `38H, 0CH, 06H, 01H` to `00H`, each followed by a delay; `80H` then `HELLO`; `0C0H` then `MDA-8086`. Note in a header comment that `HELLO` contains a doubled `L` and that G0 decides whether it displays correctly.
  - Probes: `probe_lcd_noinit.asm` (write `X` to `04H` with no initialisation: does it appear, answering Q-14), `probe_lcd_init_text.asm`, `probe_lcd_status.asm` (loop `IN AL,02H` and show it on the LEDs and 7-segment: BF and AC behaviour).
- **Acceptance:** each file documents its port-write timeline and the question it answers.
- **Traces:** IT-4, Q-04, Q-14, S4.

#### PH4-08 — IT-4 · [HUMAN] · size M
- **Goal:** verify the LCD in emu8086 and gather real-kit LCD evidence.
- **Depends on:** PH4-04, PH4-07.
- **Procedure:** at three speeds run `lcd_hello` and note whether `HELLO` appears intact, as `HELO`, or with other losses; record the effect of delay length. At the lab run the LCD probes on the real kit (photo or video each).
- **Bring back:** `docs/test-log.md` rows, `S4.md` LCD section, and a statement on whether the monitor leaves the LCD initialised (Q-14).
- **Acceptance:** line 1 shows `HELLO` and line 2 `MDA-8086` at the documented delays, or the shortfall is recorded against the G0 outcome.
- **Traces:** IT-4, Q-04, Q-14, FR-LCD-10.

#### X4 — Exit review M4 · [BOTH]
- T-05 green; IT-4 passes (or the shortfall matches the G0 decision); CGRAM, shift and cursor behave per tests.
- The user guide describes the LCD transport limits that apply. `Q-04`, `Q-14`, `Q-16` updated. Docs and PRD synced. Tag `m4`.

---

# PH5 — Keypad and reset (M5)

**Purpose:** the last peripheral (its protocol is unknown, Q-03) and the reset behaviour. **Entry condition:** X4 passed. **Pass targets:** FR-KP-*, FR-RST-*, IT-5, IT-6. Never present a guessed key encoding as verified.

#### PH5-01 — `KeypadModel`, `IKeypadProtocol`, provisional protocol · [AGENT] · size M
- **Goal:** keypad state, an isolated protocol layer, and the port `01H` arbitration, so the real protocol drops in later.
- **Depends on:** PH1-06.
- **Files:** `Core/Devices/{KeypadModel,KeypadKey,IKeypadProtocol,ProvisionalKeypadProtocol,KeypadDevice}.cs`, `samples/probes/kp_probe*.asm`, tests (T-08).
- **Spec:**
  - `KeypadKey`: `Hex0…HexF` plus `Res, Stp, Ad, Go, Da, Mon, Colon, Reg, Plus, Minus`. `KeypadModel` tracks the pressed key; UI calls take the controller lock briefly.
  - **Protocol seam:**

```csharp
public interface IKeypadProtocol
{
    string Name { get; }
    bool   IsProvisional { get; }
    byte   IdleValue { get; }
    void OnKeyDown(KeypadKey key, IDeviceWriteSink sink);
    void OnKeyUp(KeypadKey key, IDeviceWriteSink sink);
    void OnCpuWrite(byte value, IDeviceWriteSink sink);   // keyboard-flag write
    void Reset(IDeviceWriteSink sink);
}
```

  - **Provisional protocol (`// ASSUMPTION(Q-03)`, deliberately the simplest):** a hex key press posts the key value `00H–0FH` to `01H` and holds it until the CPU writes any value to `01H` (treated as the flag acknowledge) or another key is pressed; idle is `0FFH`. The UI shows a **PROVISIONAL** badge and the monitor logs it. It is **not** claimed to match the kit. `keypad.protocol = provisional` is the development default; PH5-05 replaces it, or the release states plainly that the keypad is experimental (R-03).
  - **Port `01H` arbitration (FR-KP-04):** the device remembers the last byte it posted. `NoteDeviceWrite` makes the poller treat that byte as unchanged. Any later observed change at `01H` is by definition not ours, so it is a CPU write and goes to `protocol.OnCpuWrite`. **Residual case:** if the CPU writes exactly the byte value the device last posted, the file shows no change and the write is invisible. Document it; do not claim directional certainty the transport cannot give.
  - **Stale-byte rule (F9):** on connect, after RES and at startup, post the protocol's `IdleValue` to `01H` first.
  - **Probe programs (S4):** `kp_probe.asm` loops `IN AL,01H`, `OUT 1BH,AL` (low nibble on the LEDs) and `OUT 19H,AL` (raw byte on the 7-segment). `kp_probe_flag.asm` also writes `OUT 01H,<v>` each loop to show the handshake. Note in the header that the monitor ROM may own the keyboard on the real kit; the manual's keyboard experiment (S5) may answer Q-03 better than a probe.
- **Tests (T-08):** press and release produce the documented byte; the idle value after release or acknowledge; a CPU write is classified as a flag write; a device write is never reported as a CPU write; the same-byte residual case behaves as documented; a stale `01H` is overwritten on connect; protocols are swappable without touching `KeypadModel`.
- **Acceptance:** T-08 green; every provisional behaviour carries an `ASSUMPTION(Q-03)` comment and an entry in `docs/assumptions.md`.
- **Traces:** FR-KP-02, FR-KP-04, T-08, R-03, R-10, Q-03.

#### PH5-02 — `KeypadControl` and PC-keyboard mapping · [AGENT] · size M
- **Goal:** the keypad on screen, usable by mouse and keyboard.
- **Depends on:** PH5-01.
- **Files:** `Controls/KeypadControl.cs`, `MainForm` changes.
- **Spec:**
  - Layout from PRD §10: hex keys as `C D E F / 8 9 A B / 4 5 6 7 / 0 1 2 3`; function keys `RES STP AD GO DA / MON : REG + -`. The physical layout is Q-15; refine after S4 photos.
  - Mouse down presses, mouse up (or leave) releases. Keys are tab-reachable with `AccessibleName` set.
  - **PC keyboard (FR-KP-01):** `0–9` and `A–F` (case-insensitive, main row and numpad digits) via `KeyPreview`. Ignore OS auto-repeat while held. Keys typed into emu8086's own window do **not** reach the device; the guide says so.
  - **Function keys (FR-KP-03):** drawn but greyed and inert, with the tooltip *Handled by the kit's monitor ROM; not emulated.* **RES works** (PH5-03).
- **Acceptance:** clicking and typing both update the pressed state; function keys visibly inert; no repeat flooding.
- **Traces:** FR-KP-01, FR-KP-03, NFR-09, FR-UI-01.

#### PH5-03 — Reset button, controller reset, emulator-reset detection · [AGENT] · size M
- **Goal:** RES resets every virtual peripheral and says what it cannot do; optional detection of emu8086 resetting the file (F4).
- **Depends on:** PH5-01, PH4-05, PH3-02, PH2-04.
- **Files:** `Core/Kit/ResetCoordinator.cs`, `KitController`, `Controls`/`MainForm`, tests (T-09).
- **Spec:**
  - **Scope of RES (FR-RST-01, FR-SYS-08):** both PPIs, the LCD, keypad latch and state, the dot-matrix integrator, display latches and models, transient monitor highlights. **Connection to the file is retained.**
  - **Order (`ResetCoordinator`):** (1) take the lock; (2) reset both PPIs; (3) reset the LCD to its configured power-up state; (4) clear the keypad; (5) reset the integrator and committed grid; (6) clear transient highlights; (7) post device-owned bytes (`02H = 00H`, `01H = idle`, F9); (8) rebuild the snapshot; (9) release the lock; (10) the UI refreshes.
  - **Limitation message (FR-RST-02):** after RES the status bar shows *Virtual peripherals reset. RES does not restart your program. Use Reload/Restart in emu8086.* The tooltip and user guide say the same. RES never tries to control emu8086's CPU.
  - Keyboard shortcut for RES (FR-RST-03, P2): `Ctrl+R`.
  - **Emulator-reset detection (F4):** if enabled, treat a poll as an emu8086 reset when `PortChange` shows at least `reset.min_ports` (default 3, add to `[reset]`) ports changing **all to `00H`** and at least 90% of the previously non-zero ports in the window zeroed. Then call `ResetAll()`, drop that batch without dispatching its writes (a zero written to a control port is a valid BSR command and must not be applied), and log a `Status` entry. `reset.detect_emulator = auto` follows the G0 decision for S1 scenario F; `on` and `off` override. `IsBulk` from the poller is used for diagnostics only.
- **Tests (T-09):** RES returns every slice of the snapshot to power-up; connection state unchanged; device-owned bytes re-posted; a bulk-zero poll triggers the reset and is dropped when enabled and is applied normally when disabled; a lone `OUT 1FH,00H` does **not** trigger it; the limitation message is raised.
- **Acceptance:** T-09 green; IT-6 procedure is writable from it (PH5-06).
- **Traces:** FR-RST-01, 02, 03, FR-SYS-08, R-17, Q-13, T-09.

#### PH5-04 — Keypad probe on the real kit; protocol evidence · [HUMAN] · size M
- **Goal:** evidence for Q-03 and Q-15.
- **Depends on:** PH5-01, PH0-09, PH0-10.
- **Procedure:** run `kp_probe.asm` and `kp_probe_flag.asm` on the kit if the monitor allows. For every key record the byte seen, the behaviour on release, whether a write to `01H` changes the register, and any handshake. Photograph the keypad and write its exact key labels and layout (Q-15). Also note the page numbers of the manual's keyboard experiment (S5), paraphrasing only.
- **Bring back:** `docs/spike-reports/S4-keypad.md` with a table *key → observed byte* and the handshake description.
- **Acceptance:** Q-03 marked answered, partially answered or open, with the reason.
- **Traces:** Q-03, Q-15, FR-KP-02, S4, S5.

#### PH5-05 — Final keypad protocol (blocked by Q-03) · [AGENT] · size M
- **Goal:** replace the provisional protocol with the evidenced one.
- **Depends on:** PH5-04 (Q-03 answered).
- **Files:** `Core/Devices/*Protocol.cs`, tests, `samples/asm/kp_echo.asm`.
- **Spec:** implement the protocol exactly as the evidence table says; remove the PROVISIONAL badge; add T-08 cases generated from the evidence; author `kp_echo.asm` (a pressed key shown on the LCD or LEDs, simple enough to isolate keypad errors from LCD errors) and document the protocol identically for the virtual and real kit.
- **If Q-03 stays open at release time:** keep the provisional protocol, keep the badge, document the keypad as **experimental** in the README and `known-limitations.md`, mark FR-KP-02 "not verified" in Appendix F, and obtain a written human sign-off for the exception to the PRD's definition of done.
- **Acceptance:** protocol tests match the evidence; or the exception is recorded.
- **Traces:** FR-KP-02, IT-5, Q-03, R-03.

#### PH5-06 — IT-5 and IT-6 · [HUMAN] · size M
- **Goal:** verify keypad echo and reset.
- **Depends on:** PH5-05 (or PH5-02 for IT-6 alone), PH5-03.
- **Procedure:** **IT-5:** click and type keys; confirm the program reads them at `01H` and echoes them (`kp_echo.asm`). **IT-6:** while IT-1 runs, press RES; confirm all virtual hardware clears and the status message appears; restart the program in emu8086 and confirm a clean start. If S1-F showed emu8086 resets the file, also run Stop and Reload and confirm detection behaves as configured.
- **Bring back:** `docs/test-log.md` rows.
- **Acceptance:** IT-5 and IT-6 pass per PRD §11.2, or the keypad exception is recorded.
- **Traces:** IT-5, IT-6, FR-KP-*, FR-RST-*.

#### X5 — Exit review M5 · [BOTH]
- FR-KP-* and FR-RST-* pass; T-08, T-09, IT-5 and IT-6 recorded; `Q-03` and `Q-15` closed or the exception signed off.
- Docs and PRD synced. Tag `m5`.

---

# PH6 — Hardening and release (M6)

**Purpose:** make it installable, robust, documented and measurably within the NFRs. **Entry condition:** X5 passed (or the keypad exception signed off). **Pass targets:** IT-7, IT-8, SC-2…SC-5, classmate dry run.

#### PH6-01 — Settings persistence and dialog · [AGENT] · size M
- **Goal:** FR-SYS-09 (P2) without making the port map easy to break.
- **Depends on:** PH1-03, PH1-07.
- **Files:** `Io/UserSettingsStore.cs`, `src/Mda8086Kit/SettingsForm.cs`, tests.
- **Spec:**
  - **Two layers.** `MDA8086_Kit.config` beside the exe is the shipped, user-editable defaults and is only ever **read** (the `devices` folder may not be writable). User settings (window position and size, theme, always-on-top, tuning overrides) live in `%LOCALAPPDATA%\MDA8086_Kit\settings.ini`. Precedence: built-in defaults < `.config` < user settings < command line.
  - Writes are atomic (temp file then replace). A corrupt or unreadable file falls back to defaults with a warning; startup validation reuses `KitConfig` rules.
  - **Settings dialog (⚙):** two clearly separated groups. *Display tuning* (poll interval, matrix window, gain, smoothing, orientation, LCD power-up state, busy timing) applies live. *Hardware mapping* (the port map) sits behind an **Advanced** section with a confirmation, shows the real-kit defaults beside the values and offers "Restore defaults", and applies on restart. An accidental port-map change must be hard.
- **Tests (Io.Tests):** round-trip; corrupt file; atomic replace; precedence; port-map overlap rejected from the dialog path too.
- **Acceptance:** settings survive a restart; the port map cannot be changed without the Advanced confirmation.
- **Traces:** FR-SYS-09, FR-SYS-06, D4.

#### PH6-02 — Theme, DPI, always-on-top, About · [AGENT] · size M
- **Goal:** the remaining UI requirements.
- **Depends on:** PH2-05, PH3-03, PH4-04, PH5-02, PH6-01.
- **Files:** `app.manifest`, `KitTheme.cs`, `AboutForm.cs`, `MainForm` changes.
- **Spec:**
  - **DPI (FR-UI-03):** declare per-monitor-v2 awareness in the manifest (with the `dpiAware` fallback), enable visual styles, set a minimum size, and keep all drawing relative to `ClientSize` through `KitLayout`. Check at 100, 125, 150 and 200% and on a second monitor.
  - **Theme (FR-UI-04):** a `KitTheme` palette (light and dark) for backgrounds, panels, the port monitor and status bar. Authentic component colours (LCD green, LED colours, matrix colours) stay fixed so the kit still looks like the kit. Setting is persisted.
  - **Always on top (FR-UI-05):** a toggle, persisted.
  - **About (FR-UI-06):** version from the assembly informational version; the statement **"Unofficial project, not affiliated with Midas Engineering or emu8086"**; credits (the `io.cs` author credit to deTrox Yang and the documented path-constant fix); licence; the tested emu8086 version. No Midas or emu8086 branding.
  - **Teaching tooltips (FR-7S-04, P2):** hover shows port and bit for each segment, LED and matrix cell (for example *LED R1 → port 1BH bit 0*), reusing the PH3-03 mechanism.
- **Acceptance:** UI is crisp and correctly proportioned at the listed DPI settings; theme and always-on-top persist; About text is exact.
- **Traces:** FR-UI-03…06, FR-7S-04, NFR-09, PRD §15.

#### PH6-03 — Single-exe packaging and release script · [AGENT] · size M
- **Goal:** `MDA8086_Kit.exe` as one file (NFR-08, FR-SYS-01), with a repeatable release build (F8, R-19).
- **Depends on:** PH6-02.
- **Files:** `FodyWeavers.xml` (or ILRepack target), `tools/release.ps1`, `docs/release-notes.md`.
- **Spec:**
  - **Embedding, in order of preference:** `Costura.Fody` embeds `Mda8086Kit.Core.dll` and `Mda8086Kit.Io.dll`; fall back to `ILRepack`; last resort is a zip with the DLLs beside the exe, and in that case record a deviation from NFR-08 in `docs/decisions.md` (emu8086 lists `.exe` files only and extra DLLs complicate installation).
  - **`tools/release.ps1`:** clean, build Release, run all tests, embed, stamp the version from the git tag, then assemble `dist/MDA8086_Kit_vX.Y.Z.zip` with `MDA8086_Kit.exe`, a commented sample `MDA8086_Kit.config`, `README.md`, the `docs/` user material, `samples/asm`, `samples/probes`, `LICENSE`, `NOTICE`, `third_party/README` and a SHA-256 file. No monitor ROM, no manual text, no Emulation Kit assets.
  - **`--selftest`:** instantiate the Core, load the default config, run 1,000 simulated polls through the controller, exit 0 on success. The release script runs it from an **empty folder** to prove the single exe is self-contained.
  - Note in the README that an unsigned exe can trigger SmartScreen or antivirus warnings (R-08). Code signing is out of scope.
- **Acceptance:** the script produces the zip from a clean checkout; `--selftest` passes from an empty folder; the exe name is exactly `MDA8086_Kit.exe`.
- **Traces:** FR-SYS-01, NFR-01, NFR-08, F8, R-19, R-08.

#### PH6-04 — Documentation set · [AGENT] · size L
- **Goal:** everything a classmate needs, written so SC-4 can pass.
- **Depends on:** PH6-03; fed by S1–S5 and test-log results.
- **Files:** `README.md`, `docs/user-guide.md`, `docs/port-map.md`, `docs/test-log.md`, `docs/known-limitations.md`, `docs/font-provenance.md`, `NOTICE`, `third_party/README.txt`.
- **Contents:**
  - **`README.md`** (a classmate follows only this, in under 5 minutes): what the virtual kit is; requirements; install (the exact `devices` folder path from S3); where to copy the exe and optional config; how to activate (menu, and the comment trick if S3 verified it); the file-permission steps for `c:\emu8086.io` and `.hw`; the first sample (`seg_count`); the RES limitation; known limitations; the port quick reference; the unofficial-status line. **Do not claim a feature until it has been tested.**
  - **`docs/user-guide.md`:** device layout; the 8255 explained briefly; 7-segment mapping; LED mapping; dot-matrix scanning, persistence and orientation; LCD ports, init sequence and transport limits; keypad protocol (verified or experimental); port monitor usage; settings; reset semantics; troubleshooting by status message.
  - **`docs/port-map.md`:** the twelve ports (Appendix A) with the `1CH`/`1EH` manual discrepancy noted.
  - **`docs/test-log.md`:** columns *test ID, date, OS, emu8086 version, device build, run speed, result, notes, evidence*.
  - **`docs/known-limitations.md`**, at minimum: no monitor ROM; no register display on the LCD; RES resets peripherals only; port `01H` aliasing; same-value writes are invisible (F1, F3); control-first ordering and its reverse-order residual (F2); no 8255 modes 1 and 2; timing differs from the physical kit; any S2 source edits; every S1 transport limit and the G0 outcome; keypad protocol status.
  - **`NOTICE` and `third_party/README.txt`:** the `io.cs` credit and the documented fix.
- **Acceptance:** every claim in the docs is backed by a test-log row or a spike report; links resolve; the unofficial statement appears in README, About and NOTICE.
- **Traces:** SC-4, PRD §15, PRD §16, Q-11.

#### PH6-05 — Performance and soak tooling · [AGENT] · size M
- **Goal:** measure NFR-02, 03 and 04 instead of asserting them.
- **Depends on:** PH1-07, PH3-02.
- **Files:** `src/Mda8086Kit/PerfMonitor.cs`, `tests/.../Perf/AllocationTests.cs`, `tools/soak.ps1`.
- **Spec:**
  - **`--perf`:** sample every 5 s into `perf.csv`: process CPU % (from `TotalProcessorTime`), working set, private bytes, handle count, thread count, GC counts, polls per second, changes per second, max poll duration, and UI frames per second. Log time-to-first-paint at startup (NFR-02).
  - **Allocation tests** (run on net8.0 with `GC.GetAllocatedBytesForCurrentThread`): 100,000 unchanged polls allocate essentially nothing; a run with changes allocates only into the preallocated batch and ring buffer. Watch the hot spots: per-poll buffers, reopening the file, per-port file opens, string formatting on the poll thread, unbounded logs, full-window invalidations and integrator allocation.
  - **`--sim-soak`:** alternate the matrix "A" and `seg_count` scripts every 60 s without emu8086 so the human can soak on any machine.
  - **UI throttling:** confirm the 60 Hz timer plus latest-snapshot-wins is what actually runs.
- **Targets:** start in under 2 s; idle polling under 5% of one core; memory under 100 MB; no growth trend over 30 minutes.
- **Acceptance:** tests green; `--perf` output is understandable without the code.
- **Traces:** NFR-02, 03, 04, FR-SYS-05.

#### PH6-06 — Fault injection and soak (IT-7, IT-8), test log · [BOTH] · size L
- **Goal:** prove resilience and stability on the release candidate.
- **Depends on:** PH6-05, PH6-03.
- **Agent prepares:** `docs/test-procedures/IT-7.md` and `IT-8.md` (step-by-step, with the data to bring back), and fills `test-log.md` from what the human returns.
- **Human runs:**
  - **IT-7**, six cases: start the device before emu8086; deny file permission; hold the file locked; delete `c:\emu8086.io`; recreate it; restart emu8086. After each, the app stays alive, the status text is actionable and polling recovers by itself where technically possible. Confirm the file is never truncated or resized.
  - **IT-8:** alternate IT-1 and IT-3 for **30 minutes** with `--perf`. Record CPU, memory, UI frame stability, thread count, file-handle count and any crash or hang.
- **Acceptance:** IT-7 passes all six cases; IT-8 meets the NFR-03 and NFR-04 targets with no leak trend and zero crashes; both are logged.
- **Traces:** IT-7, IT-8, SC-5, NFR-03, 04, 05.

#### PH6-07 — Release candidate, classmate dry run, tag `v1.0.0` · [BOTH] · size M
- **Goal:** the last gate before release.
- **Depends on:** PH6-04, PH6-06.
- **Steps:**
  1. Agent builds the release candidate with `tools/release.ps1` and runs the Appendix F checklist, listing anything unmet.
  2. Human tests on a **clean machine** (Windows 10 and, if possible, 11) without administrator rights where the device allows (NFR-07, NFR-01).
  3. **Classmate dry run:** someone other than the builder follows only the README and runs a sample; time it. Pass is under 5 minutes (SC-4).
  4. Ask the instructor about sharing and the preferred name (Q-11) **before broad distribution**.
  5. Final traceability review (Appendix C), then tag `v1.0.0` and publish the zip.
- **Acceptance:** SC-4 passes; Q-11 answered or distribution is limited to personal use; Appendix F is fully ticked or each exception is signed off.
- **Traces:** SC-4, Q-11, NFR-01, NFR-07, NFR-08.

#### X6 — Exit review M6 / release · [BOTH]
- IT-7 and IT-8 pass; SC-2 to SC-5 met (SC-1 per PH2/PH3); Appendix F complete.
- Open questions Q-01, Q-02, Q-03, Q-05 answered and the documents updated, or recorded exceptions (PRD §16).
- Tag `m6` and `v1.0.0`.

---

# Appendices

## Appendix A — Port quick reference

| Port | CPU view | Function |
|---|---|---|
| `00H` | Write | LCD instruction register |
| `01H` | Read / Write | Keypad register (read) / keyboard flag (write); aliased in the file (R-10) |
| `02H` | Read | LCD status register (device-owned byte, F9) |
| `04H` | Write (read TBD) | LCD data register |
| `18H` | Write | CS1 port A: dot-matrix red, active-low |
| `19H` | Write | CS2 port A: 7-segment, active-low |
| `1AH` | Write | CS1 port B: dot-matrix green, active-low |
| `1BH` | Write | CS2 port B: LEDs (bits 0–3), active-high |
| `1CH` | Write | CS1 port C: dot-matrix scan line, one-hot, active-high |
| `1DH` | none | CS2 port C (not used in scope) |
| `1EH` | Write | CS1 control register |
| `1FH` | Write | CS2 control register |

## Appendix B — Reference fixtures

Port-write timelines with delays omitted. These feed `ReferenceTimelines` (PH1-02), the simulator (PH1-08), the headless tests (PH2-04, PH3-02) and the tuning harness (PH3-05).

```text
7-segment:  1FH <- 80H
            repeat: 19H <- C0H F9H A4H B0H 99H 92H 82H F8H 80H 90H

LEDs:       1FH <- 80H
            19H <- FFH
            repeat: 1BH <- 01H 02H 04H 08H

Dot matrix: 1EH <- 80H
            18H <- FFH
            repeat for i = 0..7:  1AH <- font[i]   1CH <- (1 << i)
            font = FFH C0H B7H 77H 77H B7H C0H FFH   (active-low)

LCD demo:   00H <- 38H 0CH 06H 01H
            00H <- 80H, 04H <- 'H' 'E' 'L' 'L' 'O'
            00H <- C0H, 04H <- 'M' 'D' 'A' '-' '8' '0' '8' '6'
```

**Expected "A" grid** (top row is port B bit 7, left column is port C bit 0, `#` is green lit). The orientation is **inferred (Q-05)** until the real kit confirms it:

```text
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

Checked by hand: inverting the font gives `00 3F 48 88 88 48 3F 00`; column 1 (`3F`) lights rows 0–5, column 2 (`48`) rows 3 and 6, columns 3 and 4 (`88`) rows 3 and 7. That reproduces the grid above.

## Appendix C — Requirements traceability matrix

"Pri" is the PRD priority. "Tasks" are where the work happens; "Verification" is how it is proven.

| Requirement | Pri | Tasks | Verification |
|---|---|---|---|
| FR-SYS-01 exe in Virtual Devices | P0 | PH1-07, PH6-03 | PH1-10, PH6-07 |
| FR-SYS-02 auto-activation | P1 | PH0-08, PH2-06, PH6-04 | S3, manual |
| FR-SYS-03 single instance | P1 | PH1-07 | manual |
| FR-SYS-04 status, never crash | P0 | PH1-05, PH1-06, PH1-07 | T-06, IT-7 |
| FR-SYS-05 poll 00H–1FH at 1 ms | P0 | PH1-04, PH1-07 | T-06, PH6-05 |
| FR-SYS-06 external port map | P1 | PH1-03, PH6-01 | T-07 |
| FR-SYS-07 port monitor, CSV | P1 | PH1-06, PH1-07, PH1-09 | PH1-10 |
| FR-SYS-08 reset action | P0 | PH2-04, PH5-03 | T-09, IT-6 |
| FR-SYS-09 persist settings | P2 | PH6-01 | manual |
| FR-SYS-10 clean shutdown, no resize | P0 | PH1-05, PH1-07 | T-06, IT-7 |
| FR-PPI-01…06 | P0 | PH2-01 | T-01 |
| FR-PPI-05, 07 BSR, mode warning | P1 | PH2-01 | T-01 |
| FR-PPI-08 input pin values | P2 | PH2-01 | targeted unit test |
| FR-7S-01…03 | P0 | PH2-02, PH2-05 | T-02, IT-1 |
| FR-7S-04 tooltip | P2 | PH6-02 | manual |
| FR-LED-01, 02 | P0 | PH2-03, PH2-05 | T-03, IT-2 |
| FR-LED-03 labels | P1 | PH2-05 | manual |
| FR-DM-01, 02, 03, 05, 06 | P0 | PH3-01, PH3-02, PH3-03, PH3-05 | T-04, IT-3 |
| FR-DM-04 orientation | P1 | PH3-01, PH3-04, PH3-06 | T-04, PH3-06 |
| FR-LCD-01…05 | P0 | PH4-01, PH4-02, PH4-03, PH4-04 | T-05, IT-4 |
| FR-LCD-06…08, 10 | P1 | PH4-02, PH4-04, PH4-05 | T-05 |
| FR-LCD-09 busy timing | P2 | PH4-05 | T-05 (enabled mode) |
| FR-KP-01, 02 | P0 | PH5-01, PH5-02, PH5-05 | T-08, IT-5 |
| FR-KP-03, 04 | P1 | PH5-01, PH5-02 | T-08, IT-5 |
| FR-RST-01 | P0 | PH5-03 | T-09, IT-6 |
| FR-RST-02 | P1 | PH5-03 | T-09 |
| FR-RST-03 shortcut | P2 | PH5-03 | manual |
| FR-UI-01, 02 | P0 | PH1-07, PH2-05, PH3-03, PH4-04, PH5-02 | manual, ITs |
| FR-UI-03, 06 | P1 | PH6-02 | manual |
| FR-UI-04, 05 | P2 | PH6-02 | manual |
| NFR-01 no runtime install | | PH6-03, PH6-07 | clean-machine test |
| NFR-02 start < 2 s | | PH6-05 | stopwatch log |
| NFR-03 idle < 5% of a core | | PH1-07, PH6-05 | PH6-06 |
| NFR-04 memory < 100 MB, no leak | | PH6-05 | IT-8 |
| NFR-05 resilience | | PH1-05, PH1-07 | IT-7 |
| NFR-06 UI-free core | | PH1-01 | `CorePurityTests` |
| NFR-07 no admin rights | | PH0-08, PH6-07 | non-admin test |
| NFR-08 single exe | | PH6-03 | `--selftest` from empty folder |
| NFR-09 not colour alone | | PH2-05, PH3-03, PH4-04, PH5-02 | UI review |
| NFR-10 comments cite PRD | | every task | code review |
| SC-1 reference programs match | | PH2-07, PH3-06 | IT-1, IT-2, IT-3 |
| SC-2 all P0 pass | | all | PH6-07 |
| SC-3 no missed static changes | | PH0-06, PH2-07 | S1, IT-1, IT-2 |
| SC-4 install under 5 min | | PH6-04, PH6-07 | classmate dry run |
| SC-5 never crashes on file faults | | PH1-05, PH6-06 | IT-7 |

**Tests introduced by this plan.** `T-08` covers the keypad, protocol seam and port `01H` arbitration. `T-09` covers reset, emulator-reset detection and snapshot reset. Both continue the PRD's `T-01…T-07` sequence.

## Appendix D — Priority roll-up and non-goal guardrail

**P0 (first usable release):** connection, recovery and shutdown; polling; both 8255A instances in mode 0 with reset semantics; 7-segment; four LEDs; dot matrix with persistence; LCD core behaviour; keypad on-screen input and its port protocol (blocked by Q-03); reset; core layout; 30+ fps rendering.

**P1 (should have):** auto-activation check; single instance; external port map; port monitor and CSV export; BSR; unsupported-mode warnings; LED labels; matrix orientation; LCD cursor, blink, shift and CGRAM; function-key display; port `01H` arbitration; RES limitation message; DPI scaling; About dialog.

**P2 (nice to have):** persisted window and settings; segment and bit tooltips; realistic LCD busy timing; RES shortcut; light/dark theme; always-on-top.

**Non-goal guardrail** (ground rule 18). Before accepting any feature, ask:

1. Does it correspond to an existing PRD requirement?
2. Does it resolve a documented risk or open question?
3. Does it support one of the six in-scope components or required infrastructure?
4. Does it preserve the rule that this is a custom emu8086 device and not a full simulator?

If any answer is no, defer it unless the D5 Plan B trigger explicitly changes the scope.

## Appendix E — Open questions and risks tracker

Keep this current. "Resolved by" names the task whose output closes it.

| ID | Question or risk | Status | Resolved by | Blocks |
|---|---|---|---|---|
| Q-01 | How are successive `OUT`s delivered; what is `.hw` for? | Open until S1 | PH0-05/06 | architecture, LCD, matrix |
| Q-02 | Does emu8086 assemble the lab source unchanged? | Open until S2 | PH0-07 | SC-1 wording |
| Q-03 | How does a user program read the keypad? | Open | PH0-10, PH5-04 | FR-KP-02, IT-5 |
| Q-04 | Is the LCD HD44780-compatible; read, timing and font behaviour? | Working assumption | PH0-10, PH4-08 | LCD fidelity |
| Q-05 | Matrix orientation and the jumper's effect | Working assumption | PH3-06 | FR-DM-04 |
| Q-06 | One digit? dp wired? what is CS2 port C? | Working assumption | PH0-09, PH3-04 | FR-7S-01 |
| Q-07 | What shows before any control word after RES? | Working assumption (dark) | PH0-09, PH2-07 | FR-7S-03 |
| Q-08 | `devices` path, listing, activation, permissions | Open until S3 | PH0-08 | FR-SYS-01/02 |
| Q-09 | Built-in device port overlaps | Open until S3 | PH0-08 | R-09 |
| Q-10 | emu8086 version, Windows 11 behaviour | Open until S3 | PH0-01, PH0-08 | NFR-01 |
| Q-11 | May it be shared; preferred name? | Open | PH6-07 | release |
| Q-12 | Do lab programs read 8255 ports or the LCD data register? | Open | lab-code review | FR-PPI-08 |
| Q-13 | Does emu8086 zero or rewrite the file on start, stop or reload? | Open until S1-F | PH0-05/06 | PH5-03 |
| Q-14 | Does the monitor ROM leave the LCD initialised? | Open | PH0-09, PH4-08 | PH4-05 default |
| Q-15 | Physical keypad key layout | Open | PH0-09, PH5-04 | PH5-02 |
| Q-16 | LCD address-counter wrap at line ends | Assumed | PH4-08 | PH4-02 detail |
| R-01 | Latest-value-only transport loses fast writes | High | S1, G0, PH3-05, PH4-06 | |
| R-02 | MASM source rejected by emu8086 | Medium | S2 | |
| R-03 | Keypad protocol unknown | High | PH5-04/05 | |
| R-04 | Matrix orientation and timing wrong | Medium | PH3-05/06 | |
| R-05 | File permission problems | High | S3, PH1-05 | |
| R-06 | `io.cs` path-constant bug | Certain | PH1-05 | |
| R-07 | Emulation Kit has no open-source licence | Certain | ground rule 3 | |
| R-08 | emu8086, Windows or antivirus compatibility | Medium | PH6-03, PH6-07 | |
| R-09 | Port overlap with built-in devices | Medium | S3 | |
| R-10 | Port `01H` aliasing | High | PH5-01 | |
| R-11 | LCD font provenance | Low | PH4-01 | |
| R-12 | Virtual timing mismatch | Medium | PH3-05 | |
| R-13 | Scope creep | Medium | Appendix D | |
| R-14 | Identical consecutive stream writes are invisible (F1) | High | G0, PH4-06 | |
| R-15 | Repeated identical mode-set does not re-clear latches (F3) | Low | PH2-01 | |
| R-16 | Same-poll write order lost (F2) | Medium | PH1-04 | |
| R-17 | emu8086 rewrites the file on lifecycle events (F4) | Medium | PH5-03 | |
| R-18 | Monitor ROM leaves the LCD initialised (F6) | Medium | PH4-05 | |
| R-19 | Single exe versus multi-assembly (F8) | Medium | PH6-03 | |
| R-20 | Stale device-owned bytes hang or misread programs (F9) | Medium | PH4-03, PH5-01 | |

## Appendix F — Definition of done and final acceptance checklist

The project is done only when every box is true, or the exception is recorded with a written human sign-off.

**Functional**
- [ ] All P0 requirements implemented and verified (SC-2 = 100%).
- [ ] Session 5 7-segment, LED and dot-matrix programs match the real kit to the extent defined by S1, S2 and S4 (SC-1).
- [ ] LCD P0 behaviour works through the documented ports, with transport limits documented (G0 outcome).
- [ ] Keypad P0 behaviour verified after Q-03, or the experimental exception is signed off.
- [ ] RES resets all virtual peripherals and states its CPU limitation.

**Architecture**
- [ ] Core is UI-free and file-free (`CorePurityTests` green).
- [ ] File access exists only in `EmuIoPortFile`; polling logic only in `BusPoller`; the UI reads snapshots only.
- [ ] Port map is configurable and defaults equal the real kit.
- [ ] Two 8255A instances with correct mode-set, reset and BSR semantics.
- [ ] 7-segment active-low, LED active-high, matrix scan plus persistence, LCD command/data/status mapping.

**Transport and reliability**
- [ ] Survives a missing file, a locked file and permission denial; recovers after recreation or an emu8086 restart.
- [ ] Never truncates or resizes `c:\emu8086.io`.
- [ ] Device-owned bytes (`02H`, `01H`) are always written defined values on connect and RES.

**Performance (measured)**
- [ ] Start < 2 s; idle polling < 5% of one core; memory < 100 MB; no leak or crash in the 30-minute soak.

**emu8086 integration**
- [ ] Device appears under Virtual Devices; activation behaviour verified and documented.
- [ ] Tested `devices` path, non-administrator operation, port conflicts and the emu8086 version recorded.
- [ ] Three-speed matrix completed for every timing-sensitive test.

**Quality**
- [ ] `T-01…T-09` pass; `IT-1…IT-8` completed or explicitly blocked and documented.
- [ ] SC-1…SC-5 reviewed; no known P0 defect remains.

**Documentation and legal**
- [ ] README passes the under-5-minute classmate dry run; user guide, port map, known limitations and test log complete.
- [ ] Q-01, Q-02, Q-03 and Q-05 answered and reflected in the docs.
- [ ] Unofficial status stated in README, About and NOTICE; `io.cs` credit kept; font provenance recorded.
- [ ] No unlicensed Emulation Kit code, assets or help text; no lab text or monitor ROM in the repository.
- [ ] Q-11 resolved before broad distribution.

**Release**
- [ ] `MDA8086_Kit.exe` is a single file; optional config included only if needed.
- [ ] Release zip tested on a clean machine; `v1.0.0` tagged.

## Appendix G — Dependency graph and critical blockers

```
PH0-01 -> PH0-08
PH0-02 -> PH0-03 -> PH0-05 -> PH0-06 -> PH0-11 (G0) -> PH1 ...
PH0-02 -> PH0-04 -> PH0-05
PH0-07 (S2) --------------------> PH0-11, PH2-06 (header style)
PH0-08 (S3) --------------------> PH0-11, PH1-10, PH6-04
PH0-09 (S4) deferrable ---------> PH3-06, PH4-08, PH5-04 (Q-05, Q-06, Q-07, Q-14, Q-15)
PH0-10 (S5) deferrable ---------> PH5-04/05 (Q-03), PH4 (Q-04)

PH1 --X1--> PH2 --X2--> PH3 --X3--> PH4 --X4--> PH5 --X5--> PH6 --X6--> v1.0.0
                                       \                      ^
                                        PH4-06 follows the G0 LCD outcome
                                        PH5-05 is blocked by Q-03
```

**Critical blockers**
- **G0 (S1)** decides the transport architecture and the LCD and matrix scope. Nothing in PH1 starts before it.
- **Q-03** blocks PH5-05, IT-5 and the P0 status of FR-KP-02. Ship the keypad as experimental if it stays open.
- **Q-05** controls the matrix default orientation and the expected grid. Orientation stays configurable until closed.
- **Q-04** controls how confidently the LCD can be called hardware-faithful.
- **S2** controls the exact SC-1 wording and the header style of every sample.

## Appendix H — Plan B outline: standalone simulator

**Enter this appendix only if the human records a Plan B decision at G0 (PH0-11), or later if a transport limit proves unfixable.** Then **stop this plan and write a new `plan.md`** from this outline. Do not start any of it before the go/no-go is written down.

**Trigger (PRD D5).** Write loss on critical LCD or matrix patterns remains unacceptable after: faster reliable polling, `timeBeginPeriod(1)`, reasonable emu8086 speed settings, transport-aware model tuning (PH3-05) and any LCD mitigation (PH4-06), and findings from the shipped sample devices.

**What must survive unchanged** (this is why the models are UI-free and clock-injected):

- `Ppi8255`, `SevenSegmentModel`, `LedBank`, `DotMatrixModel`, `PersistenceIntegrator`, `Hd44780Lcd`, `KeypadModel` and its protocol layer.
- `KitSnapshot` and the controls that draw it; `KitConfig` and the port map; most unit tests and the Appendix B fixtures.
- The key enabler: models take time from `IClock`. In Plan B the clock becomes **virtual time derived from executed instructions**, so the integrator needs no change and timing is deterministic.

**New scope, only after the trigger:**

1. **8086 CPU core** covering what the lab programs use: `MOV`, `ADD`, `SUB`, `MUL`, `DIV`, `INC`, `DEC`, `CMP`, `TEST`, `AND`, `OR`, `XOR`, `NOT`, shifts and rotates, `JMP` and conditional jumps, `LOOP`, `CALL`/`RET`, `PUSH`/`POP`, `IN`/`OUT`, `NOP`, `INT 3`, `HLT`.
2. **Assembler subset** for the lab dialect (`SEGMENT … PARA PUBLIC`, `ASSUME`, `ORG`, `END`, `DB`/`DW`, `OFFSET`), or load emu8086 output.
3. **Direct I/O dispatch:** `IN`/`OUT` call `IPortDevice.OnCpuWrite` and the read path directly, with no file, no polling and no lost writes. Port `01H` aliasing disappears.
4. **Run, step and stop UI** and a minimal memory and register view. This is the point at which the PRD non-goal "no CPU, assembler or debugger" is deliberately overridden, so record it in `docs/decisions.md`.
5. **Test plan:** the same IT programs run headlessly against the new core; add instruction-level tests.

**Rollout order for the new plan:** freeze the current repository tag; extract and verify the reusable pieces; build the CPU core with tests; add the assembler; wire direct I/O; build the run UI; re-run IT-1…IT-8 and SC-1…SC-5.

## Appendix I — Document history

| Version | Date | Notes |
|---|---|---|
| 0.1 | 2 Oct 2026 | Claude draft: working rules, findings F1–F8, tracker, environment, PH0 spikes and gate G0. Ended after PH0-11. |
| GPT draft | 2 Oct 2026 | Full-length plan: architecture, contracts, per-component model sections, UI and port-monitor spec, traceability, Plan B. Flat checklist, no acceptance criteria per task, no human/agent split. |
| 0.2 | 2 Oct 2026 | This merged plan. Task structure, rules, spikes, G0 and findings F1–F8 from the Claude draft; completed through PH6. From the GPT draft: source-of-truth hierarchy, agent operating rules, architecture diagram and contracts, `PortFile` state machine and error classification, baseline seeding, shutdown order, config validation, per-model detail (8255, 7-segment, LEDs, matrix, HD44780), keypad arbitration, snapshot and concurrency design, port-monitor and UI spec, error-message policy, packaging, documentation set, traceability matrix, definition of done and Plan B. New in 0.2: findings F9 and F10, tests T-08 and T-09, Q-16, R-20, the optional LCD write-sequence include, the emulator-reset signature, `CorePurityTests`, the tuning harness and the sample-device source review. |

**Where this plan deliberately departs from the GPT draft.** (1) Poll-batch ordering is control-class first, not plain ascending port order, because ascending order applies `19H` before `1FH` and the mode-set then wipes the data (F2). (2) Repository samples are written independently rather than copied from the lab programs, to respect PRD §15. (3) Same-value writes are treated as an unfixable property of the file transport, not a polling-speed problem, so LCD text gets an explicit mitigation decision at G0 (F1). (4) Every task has acceptance criteria and a human/agent tag so the agent cannot fake lab or emulator results.
