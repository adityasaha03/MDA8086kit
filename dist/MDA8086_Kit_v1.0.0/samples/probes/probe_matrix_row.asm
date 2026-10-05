; MDA8086_Kit.exe
; Purpose: Light a full single row (green).
; Expected result: A full row lights up on the matrix.
; Real kit observe: Which edge is "top" for B bit 7?
; Answers: Q-05

    ORG 1000H

START:
    MOV AL, 80H
    OUT 1EH, AL

    MOV AL, 0FFH
    OUT 18H, AL     ; Red off

    MOV AL, 7FH
    OUT 1AH, AL     ; Green bit 7 low, others high (one pixel per column)

    MOV AL, 0FFH
    OUT 1CH, AL     ; Enable all scan lines

    ; Idle
IDLE_LOOP:
    JMP IDLE_LOOP
