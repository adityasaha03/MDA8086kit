# Environment Inventory (PH0-01)

| Item | Value |
|---|---|
| OS (Windows 10/11) | `11`
| Running as Administrator? | `no`
| .NET Framework 4.8 Installed? | `yes` |
| .NET 8.0 SDK / Visual Studio | `yes` |
| emu8086 Version | `4.08` |
| emu8086 Install Path | `C:\emu8086` |
| emu8086 `devices` Folder Path | `C:\emu8086\DEVICES` |

## File Checks (`c:\emu8086.io` and `c:\emu8086.hw`)

**1. Before starting emu8086:**
- Do they exist? `c:\emu8086.io yes` and `c:\emu8086.hw no`
- Sizes: `12KB`

**2. After starting emu8086:**
- Do they exist? `c:\emu8086.io yes` and `c:\emu8086.hw no`
- Sizes: `12KB`

**3. After running a program:**
- Do they exist? `c:\emu8086.io yes` and `c:\emu8086.hw no`
- Sizes: `12KB`

**4. File Permissions:**
- Can you read/write them? `YES`
