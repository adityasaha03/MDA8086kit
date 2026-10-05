# MDA-8086 Virtual Trainer Kit

A virtual device plugin for `emu8086` that fully emulates the hardware of the MDA-Win8086 microprocessor trainer kit. It allows students and instructors to run original lab assignments on virtual LCDs, 7-segment displays, dot matrix displays, and LEDs without needing the physical board.

## Documentation

Full documentation on how to set up the virtual kit, a complete port reference, and code snippets for initializing and managing the virtual devices can be found in the [User Guide](docs/user-guide.md).

For architecture and design records, check the [docs folder](docs).

## Features
- **7-Segment Display & LEDs**
- **8x8 Bi-color Dot Matrix**
- **HD44780 Character LCD**
- **Hardware-Accurate Port Maps** (Supports `CS1` and `CS2` through emulated 8255 Programmable Peripheral Interfaces)
- **Single Executable Delivery** (Drop into your `emu8086/devices` folder and go)

## Download & Installation
1. Go to the [Releases page](../../releases/latest) on the right side of this repository.
2. Download the latest `MDA8086_Kit_vX.Y.Z.zip` file.
3. Extract the ZIP and copy `MDA8086_Kit.exe` into your `emu8086\devices` directory.

## Quick Start
1. Ensure the compiled `MDA8086_Kit.exe` is in your `emu8086\devices` directory.
2. In your assembly source file, use `#start=MDA8086_Kit.exe#` on the first line.
3. Hit "Emulate".

*See the User Guide for detailed instructions on writing your first program!*