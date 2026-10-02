; MDA8086_Kit.exe
; Purpose: Display a stable green "A" on the dot matrix.
; Expected result: A green "A" is visible, multiplexed across 8 columns.
; Timeline: OUT 1EH,80H -> OUT 18H,0FFH -> Loop: OUT 1AH,font[i] -> OUT 1CH, (1<<i) -> DELAY -> Next
; Answers: IT-3, SC-1

    ORG 1000H

START:
    ; Set CS1 to Mode 0, All Output
    MOV AL, 80H
    OUT 1EH, AL

    ; Turn off Red completely (Active Low)
    MOV AL, 0FFH
    OUT 18H, AL

MAIN_LOOP:
    MOV BX, OFFSET FONT
    MOV CX, 8       ; 8 columns
    MOV AH, 01H     ; First scan line (Port C bit 0)

SCAN_LOOP:
    ; Data to Green port
    MOV AL, CS:[BX]
    OUT 1AH, AL

    ; Enable scan line
    MOV AL, AH
    OUT 1CH, AL

    ; Delay
    PUSH CX
    MOV CX, 300     ; COLDELAY parameter
DELAY_LOOP:
    NOP
    LOOP DELAY_LOOP
    POP CX

    INC BX
    SHL AH, 1       ; Next scan line
    LOOP SCAN_LOOP

    JMP MAIN_LOOP

FONT:
    DB 0FFH, 0C0H, 0B7H, 77H, 77H, 0B7H, 0C0H, 0FFH
