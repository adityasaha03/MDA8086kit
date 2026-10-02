; S1A: Burst of distinct bytes
; Writes bytes 41H...48H to port 04H with no delay, then a long idle delay.
;
; Timeline:
; [0EH] <- Counter
; [04H] <- 41H
; ...
; [04H] <- 48H

ORG 1000H

START:
    MOV BL, 0
MAIN_LOOP:
    MOV AL, BL
    OUT 0EH, AL   ; Segment marker
    INC BL
    
    ; Burst writes
    MOV AL, 41H
    OUT 04H, AL
    MOV AL, 42H
    OUT 04H, AL
    MOV AL, 43H
    OUT 04H, AL
    MOV AL, 44H
    OUT 04H, AL
    MOV AL, 45H
    OUT 04H, AL
    MOV AL, 46H
    OUT 04H, AL
    MOV AL, 47H
    OUT 04H, AL
    MOV AL, 48H
    OUT 04H, AL
    
    ; Long delay (~0.5s)
    MOV CX, 0FFFFH
DELAY:
    LOOP DELAY
    
    JMP MAIN_LOOP

END START
