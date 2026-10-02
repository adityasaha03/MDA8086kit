; S1B: Burst with delay
; Bursts with N loop iterations between writes.
; Marks current N index to port 0DH.

ORG 1000H

START:
    MOV BL, 1
OUTER_LOOP:
    MOV AL, BL
    OUT 0DH, AL     ; N index 1..4
    
    MOV CX, 20      ; 20 bursts per N
BURST_LOOP:
    PUSH CX
    
    ; Burst with delay
    MOV AL, 41H
    OUT 04H, AL
    CALL DO_DELAY
    
    MOV AL, 42H
    OUT 04H, AL
    CALL DO_DELAY
    
    MOV AL, 43H
    OUT 04H, AL
    CALL DO_DELAY
    
    MOV AL, 44H
    OUT 04H, AL
    
    POP CX
    LOOP BURST_LOOP
    
    INC BL
    CMP BL, 5
    JNE OUTER_LOOP
    
    ; Reset and repeat
    JMP START

DO_DELAY:
    ; N=10, 100, 1000, 10000 based on BL
    PUSH CX
    CMP BL, 1
    JE SET_10
    CMP BL, 2
    JE SET_100
    CMP BL, 3
    JE SET_1000
    MOV CX, 10000
    JMP DELAY_LOOP
SET_10: MOV CX, 10
    JMP DELAY_LOOP
SET_100: MOV CX, 100
    JMP DELAY_LOOP
SET_1000: MOV CX, 1000
DELAY_LOOP:
    LOOP DELAY_LOOP
    POP CX
    RET

END START
