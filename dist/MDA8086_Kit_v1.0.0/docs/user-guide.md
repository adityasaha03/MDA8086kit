# MDA-8086 Virtual Trainer Kit - User Guide

Welcome to the MDA-8086 Virtual Trainer Kit! This tool provides a virtual emulation of the MDA-Win8086 hardware trainer, enabling you to test and run assembly programs written for the real hardware directly within `emu8086`.

## 1. Installation & Setup

1. Copy the `MDA8086_Kit.exe` into the `devices` folder of your `emu8086` installation directory.
2. In your assembly source file, add the following comment at the very top to automatically launch the virtual kit when you hit emulate:
   ```assembly
   #start=MDA8086_Kit.exe#
   ```
3. Alternatively, you can launch it manually from the `emu8086` Virtual Devices menu.
4. **File Permissions:** Ensure that your user account has write access to `C:\emu8086.io`, as this is how the emulator communicates with the virtual kit.

---

## 2. Port Map Quick Reference

The Virtual Kit listens to the following I/O ports. These precisely match the hardware wiring of the physical MDA-Win8086 trainer.

| Port | Device / Function | Direction | Note |
|---|---|---|---|
| `00H` | LCD Instruction Register | Write | Sends commands (Clear, Home, etc.) |
| `02H` | LCD Status Register | Read | Bit 7 is the Busy Flag |
| `04H` | LCD Data Register | Write | Writes characters to the screen |
| `18H` | CS1 Port A: Dot Matrix (Red) | Write | Active-low |
| `19H` | CS2 Port A: 7-Segment | Write | Active-low |
| `1AH` | CS1 Port B: Dot Matrix (Green) | Write | Active-low |
| `1BH` | CS2 Port B: LEDs (Bits 0-3) | Write | Active-high |
| `1CH` | CS1 Port C: Dot Matrix Scan Line | Write | One-hot |
| `1EH` | CS1 Control Register | Write | Configures 8255 #1 (Ports 18H, 1AH, 1CH) |
| `1FH` | CS2 Control Register | Write | Configures 8255 #2 (Ports 19H, 1BH, 1DH) |

---

## 3. Device Setup & Code Snippets

Before using any of the peripheral devices, you must configure the programmable peripheral interfaces (8255 chips) by writing the correct control word to their control registers. 

By default, writing `80H` sets all ports to Output (Mode 0). 

### Turning Off Unused Devices
Because multiple devices share the same virtual space and I/O files, it is highly recommended to turn off devices you aren't actively using in your program to prevent visual clutter or ghosting.

* **To turn off 7-Segment displays:** Write `FFH` to `19H` (Active-low)
* **To turn off Dot Matrix displays:** Write `FFH` to `18H` and `1AH` (Active-low)
* **To turn off LEDs:** Write `00H` to `1BH` (Active-high)

**Initialization Snippet:**
```assembly
; Initialize CS2 (7-Segment and LEDs) to Output
MOV AL, 80H
OUT 1FH, AL

; Turn off 7-Segment Displays (Active-Low)
MOV AL, 0FFH
OUT 19H, AL

; Turn off LEDs (Active-High)
MOV AL, 00H
OUT 1BH, AL

; Initialize CS1 (Dot Matrix) to Output
MOV AL, 80H
OUT 1EH, AL

; Turn off Dot Matrix Colors (Active-Low)
MOV AL, 0FFH
OUT 18H, AL   ; Red off
OUT 1AH, AL   ; Green off
```

---

### Using the 7-Segment Display

The 7-segment display uses CS2 Port A (`19H`). It is active-low, meaning a `0` turns the segment ON, and a `1` turns it OFF.

```assembly
; Example: Display '0' on the 7-segment (Assuming standard wiring: gfedcba)
; Binary 11000000 (C0H) turns on segments a,b,c,d,e,f and leaves g off.
MOV AL, 80H
OUT 1FH, AL    ; Set CS2 to Output

MOV AL, 0C0H
OUT 19H, AL    ; Write '0' pattern to 7-segment
```

### Using the LEDs

The LED bank uses the lower 4 bits of CS2 Port B (`1BH`). It is active-high, meaning a `1` turns the LED ON.

```assembly
MOV AL, 80H
OUT 1FH, AL    ; Set CS2 to Output

MOV AL, 0FH    ; 00001111 in binary
OUT 1BH, AL    ; Turn on all 4 LEDs
```

### Using the Dot Matrix

The 8x8 Dot Matrix is controlled via CS1. 
- `1CH` selects the active column (Scan Line - usually one-hot).
- `18H` sets the Red row data (Active-low).
- `1AH` sets the Green row data (Active-low).

```assembly
MOV AL, 80H
OUT 1EH, AL    ; Set CS1 to Output

; Select the first column
MOV AL, 01H
OUT 1CH, AL    

; Turn on top-left Red pixel
MOV AL, 0FEH   ; 11111110
OUT 18H, AL
```

### Using the LCD (HD44780)

The LCD does not run through the 8255 PPI, but has dedicated ports. You must issue standard HD44780 initialization commands to `00H` before sending text characters to `04H`.

```assembly
; Clear Display Command
MOV AL, 01H
OUT 00H, AL

; Send character 'A' (ASCII 41H)
MOV AL, 41H
OUT 04H, AL
```

---

## 4. Known Limitations
- **Emulator Reset**: The emulator may truncate or wipe the `C:\emu8086.io` file when starting or stopping.
- **Port 01H (Keypad)**: Keypad data polling can sometimes read stale values from previous programs if not carefully flushed.
- **LCD Display Persistence**: Unlike the real kit where the monitor ROM initializes the LCD and clears it, the Virtual Kit boots into the exact state determined by your UI settings (Display Tuning -> LCD Power-up State).
