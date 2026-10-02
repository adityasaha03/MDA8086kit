# Decisions and Risks

## Risks
- R-14: The single biggest feasibility risk. Consecutive identical bytes to the LCD register are indistinguishable from one write in `.io`.
- R-15: Mode-set clearing latches - repeated identical control word produces no file change.
- R-16: Loss of write order if two ports change in the same poll interval.
- R-17: File zeroing on emulator lifecycle actions may corrupt device state (e.g. false `00H` commands).
- R-18: Monitor ROM initializes LCD vs bare display model.
- R-19: NFR-08 "Single .exe" conflicts with multi-assembly.
- R-20: Device-owned bytes can be stale from previous runs.

## Decisions
- (Placeholder for G0 and other decisions)
