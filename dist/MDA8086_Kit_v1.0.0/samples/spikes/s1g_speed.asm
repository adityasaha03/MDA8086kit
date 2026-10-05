; S1G: Speed
; Fixed length write loop to estimate emu8086 IPS

ORG 1000H

START:
    MOV AL, 0
LOOP_START:
    OUT 0EH, AL   ; 1 byte output
    INC AL        ; loop body
    JMP LOOP_START

END START
