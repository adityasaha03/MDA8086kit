; MDA8086_Kit.exe
; Purpose: Output Mode 0 then idle without writing data.
; Expected result: All segments lit after mode-set (due to zeroed latches).
; Real kit observe: Are all segments lit after mode-set?
; Answers: Q-07

    ORG 1000H

START:
    MOV AL, 80H
    OUT 1FH, AL

IDLE_LOOP:
    JMP IDLE_LOOP
