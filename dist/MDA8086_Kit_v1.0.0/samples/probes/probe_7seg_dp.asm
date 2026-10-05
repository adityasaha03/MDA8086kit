; MDA8086_Kit.exe
; Purpose: Enable only the decimal point segment.
; Expected result: DP is lit, other segments dark.
; Real kit observe: Is dp wired? How many digits?
; Answers: Q-06

    ORG 1000H

START:
    MOV AL, 80H
    OUT 1FH, AL

    MOV AL, 7FH     ; Active-low, bit 7 is DP
    OUT 19H, AL

IDLE_LOOP:
    JMP IDLE_LOOP
