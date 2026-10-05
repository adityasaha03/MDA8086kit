; MDA8086_Kit.exe
; Purpose: Display a full row using both Red and Green data active on the same lines.
; Expected result: A row of Amber/Yellow.
; Real kit observe: Amber appearance.
; Answers: S4

    ORG 1000H

START:
    MOV AL, 80H
    OUT 1EH, AL

    MOV AL, 7FH
    OUT 18H, AL     ; Red data (bit 7 low)
    OUT 1AH, AL     ; Green data (bit 7 low)

    MOV AL, 0FFH
    OUT 1CH, AL     ; Enable all scan lines

IDLE_LOOP:
    JMP IDLE_LOOP
