; MDA8086_Kit.exe
; Purpose: Light a single corner pixel.
; Expected result: Exactly one LED lights up at the origin.
; Real kit observe: Find the physical origin (0,0) corner.
; Answers: Q-05

    ORG 1000H

START:
    MOV AL, 80H
    OUT 1EH, AL

    MOV AL, 0FFH
    OUT 18H, AL     ; Red off

    MOV AL, 7FH
    OUT 1AH, AL     ; Green bit 7 low

    MOV AL, 01H
    OUT 1CH, AL     ; Scan line 0 high

    ; Idle
IDLE_LOOP:
    JMP IDLE_LOOP
