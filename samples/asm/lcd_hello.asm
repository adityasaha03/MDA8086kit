; MDA8086_Kit.exe
; Purpose: Initialize LCD and display "HELLO" on line 1, "MDA-8086" on line 2.
; Expected result: LCD shows the text.
; Note: HELLO contains a doubled 'L'. If LCD is not fast enough, it might drop one 'L' (becoming HELO).
; Answers: IT-4, Q-04, Q-14

    ORG 1000H

START:
    ; 1. Initialize LCD (Command Register is Port 00H)
    MOV AL, 38H     ; Function set: 8-bit, 2 lines, 5x8 font
    OUT 00H, AL
    CALL DELAY

    MOV AL, 0CH     ; Display on, cursor off, blink off
    OUT 00H, AL
    CALL DELAY

    MOV AL, 06H     ; Entry mode: increment, no shift
    OUT 00H, AL
    CALL DELAY

    MOV AL, 01H     ; Clear display
    OUT 00H, AL
    CALL DELAY_LONG ; Clear needs longer delay

    ; 2. Write "HELLO" to line 1
    MOV AL, 80H     ; Set DDRAM address to 00H (line 1 start)
    OUT 00H, AL
    CALL DELAY

    MOV AL, 'H'
    OUT 04H, AL     ; Data register is Port 04H
    CALL DELAY

    MOV AL, 'E'
    OUT 04H, AL
    CALL DELAY

    MOV AL, 'L'
    OUT 04H, AL
    CALL DELAY

    MOV AL, 'L'
    OUT 04H, AL
    CALL DELAY

    MOV AL, 'O'
    OUT 04H, AL
    CALL DELAY

    ; 3. Write "MDA-8086" to line 2
    MOV AL, 0C0H    ; Set DDRAM address to 40H (line 2 start)
    OUT 00H, AL
    CALL DELAY

    MOV AL, 'M'
    OUT 04H, AL
    CALL DELAY
    MOV AL, 'D'
    OUT 04H, AL
    CALL DELAY
    MOV AL, 'A'
    OUT 04H, AL
    CALL DELAY
    MOV AL, '-'
    OUT 04H, AL
    CALL DELAY
    MOV AL, '8'
    OUT 04H, AL
    CALL DELAY
    MOV AL, '0'
    OUT 04H, AL
    CALL DELAY
    MOV AL, '8'
    OUT 04H, AL
    CALL DELAY
    MOV AL, '6'
    OUT 04H, AL
    CALL DELAY

IDLE_LOOP:
    JMP IDLE_LOOP

DELAY:
    MOV CX, 02FFH
DELAY_WAIT:
    NOP
    LOOP DELAY_WAIT
    RET

DELAY_LONG:
    MOV CX, 1FFFH
DELAY_LONG_WAIT:
    NOP
    LOOP DELAY_LONG_WAIT
    RET
