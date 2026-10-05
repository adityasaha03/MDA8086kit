; S1C: Identical byte writes
; Writes 4CH to port 04H twice.

ORG 1000H

START:
    ; Variant 1: back-to-back
    MOV AL, 1
    OUT 0CH, AL
    MOV AL, 4CH
    OUT 04H, AL
    OUT 04H, AL
    
    MOV CX, 0FFFFH
    CALL WAIT
    
    ; Variant 2: 100-iteration gap
    MOV AL, 2
    OUT 0CH, AL
    MOV AL, 4CH
    OUT 04H, AL
    MOV CX, 100
    CALL WAIT
    OUT 04H, AL
    
    MOV CX, 0FFFFH
    CALL WAIT

    ; Variant 3: 10000-iteration gap
    MOV AL, 3
    OUT 0CH, AL
    MOV AL, 4CH
    OUT 04H, AL
    MOV CX, 10000
    CALL WAIT
    OUT 04H, AL
    
    MOV CX, 0FFFFH
    CALL WAIT
    
    ; Variant 4: with OUT 00H,06H between
    MOV AL, 4
    OUT 0CH, AL
    MOV AL, 4CH
    OUT 04H, AL
    MOV AL, 06H
    OUT 00H, AL
    MOV AL, 4CH
    OUT 04H, AL
    
    MOV CX, 0FFFFH
    CALL WAIT
    
    JMP START

WAIT:
    LOOP WAIT
    RET

END START
