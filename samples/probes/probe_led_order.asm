; MDA8086_Kit.exe
; Purpose: Cycle through each LED individually with a long delay.
; Expected result: Each of the 4 LEDs light up in order.
; Real kit observe: Confirm R1, G, Y, R2 order.
; Answers: S4

    ORG 1000H

START:
    MOV AL, 80H
    OUT 1FH, AL

MAIN_LOOP:
    MOV AL, 01H
    OUT 1BH, AL
    CALL DELAY

    MOV AL, 02H
    OUT 1BH, AL
    CALL DELAY

    MOV AL, 04H
    OUT 1BH, AL
    CALL DELAY

    MOV AL, 08H
    OUT 1BH, AL
    CALL DELAY

    JMP MAIN_LOOP

DELAY:
    MOV CX, 0FFFFH
DELAY_WAIT:
    NOP
    LOOP DELAY_WAIT
    RET
