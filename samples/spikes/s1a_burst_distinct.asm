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
    MOV AL, 0
MAIN_LOOP:
    OUT 0EH, AL   ; Segment marker
    INC AL
    
    ; Burst writes
    MOV DL, 41H
    OUT 04H, DL
    MOV DL, 42H
    OUT 04H, DL
    MOV DL, 43H
    OUT 04H, DL
    MOV DL, 44H
    OUT 04H, DL
    MOV DL, 45H
    OUT 04H, DL
    MOV DL, 46H
    OUT 04H, DL
    MOV DL, 47H
    OUT 04H, DL
    MOV DL, 48H
    OUT 04H, DL
    
    ; Long delay (~0.5s)
    MOV CX, 0FFFFH
DELAY:
    LOOP DELAY
    
    JMP MAIN_LOOP

END START
