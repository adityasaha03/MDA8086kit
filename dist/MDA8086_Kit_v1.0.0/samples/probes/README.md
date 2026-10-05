# MDA-8086 Kit Probes

This directory contains standalone assembly programs intended to run on the **real physical MDA-8086 kit** (and tested against this virtual emulator).

These probes are designed to systematically answer the open questions (`Q-*`) recorded in the project's documentation by displaying specific diagnostic patterns.

## Available Probes

| File | Purpose | Answers |
|---|---|---|
| `probe_matrix_column.asm` | Lights a single column | Q-05 (Port C axis) |
| `probe_matrix_row.asm` | Lights a single row | Q-05 (Port B edge) |
| `probe_matrix_corner.asm` | Lights one pixel | Q-05 (Origin corner) |
| `probe_matrix_red.asm` | Matrix A scan in Red | S4 (Red wiring) |
| `probe_matrix_both.asm` | Both lines active | S4 (Amber appearance) |
| `probe_7seg_initial.asm` | Mode-set only | Q-07 (Are segments lit after mode set?) |
| `probe_7seg_dp.asm` | DP only | Q-06 (DP wiring and digit count) |
| `probe_led_order.asm` | Cycles LEDs | S4 (R1, G, Y, R2 order) |

## Usage
Assemble these in emu8086 and run them on the real hardware to collect visual evidence.
