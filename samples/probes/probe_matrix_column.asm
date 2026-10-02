; MDA8086_Kit.exe
; Purpose: Light a full single column (green).
; Expected result: A full column lights up on the matrix.
; Real kit observe: Which physical axis (X or Y) does port C select?
; Answers: Q-05

    ORG 1000H

START:
    MOV AL, 80H
    OUT 1EH, AL

    MOV AL, 0FFH
    OUT 18H, AL     ; Red off

    MOV AL, 00H
    OUT 1AH, AL     ; Green all bits driven low (all on)

    MOV AL, 01H
    OUT 1CH, AL     ; Select first scan line

    ; Idle
IDLE_LOOP:
    JMP IDLE_LOOP
