; MDA8086_Kit.exe
; Purpose: Display the "A" scan through the red path.
; Expected result: A stable red "A".
; Real kit observe: Red wiring correctness.
; Answers: S4

    ORG 1000H

START:
    MOV AL, 80H
    OUT 1EH, AL

    MOV AL, 0FFH
    OUT 1AH, AL     ; Green off

MAIN_LOOP:
    MOV BX, OFFSET FONT
    MOV CX, 8       
    MOV AH, 01H     

SCAN_LOOP:
    MOV AL, CS:[BX]
    OUT 18H, AL     ; Red data port

    MOV AL, AH
    OUT 1CH, AL

    PUSH CX
    MOV CX, 300
DELAY_LOOP:
    NOP
    LOOP DELAY_LOOP
    POP CX

    INC BX
    SHL AH, 1
    LOOP SCAN_LOOP

    JMP MAIN_LOOP

FONT:
    DB 0FFH, 0C0H, 0B7H, 77H, 77H, 0B7H, 0C0H, 0FFH
