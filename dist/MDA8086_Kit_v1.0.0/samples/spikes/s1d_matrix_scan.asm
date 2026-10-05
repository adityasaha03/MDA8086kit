; S1D: Dot-matrix scan
; Original program producing the "A" reference timeline.
;
; Timeline:
; [1EH] <- 80H
; [18H] <- 0FFH
; For i=0..7:
;   [1AH] <- font[i]
;   [1CH] <- (1<<i)
;   Delay (300 loops)

ORG 1000H
JMP START

FONT DB 0FFH, 0C0H, 0B7H, 77H, 77H, 0B7H, 0C0H, 0FFH

START:
    MOV AL, 80H
    OUT 1EH, AL
    MOV AL, 0FFH
    OUT 18H, AL

MAIN_LOOP:
    MOV BX, OFFSET FONT
    MOV CX, 8
    MOV AH, 1      ; Column selector (1<<i)
    
SCAN_LOOP:
    MOV AL, CS:[BX]
    OUT 1AH, AL
    MOV AL, AH
    OUT 1CH, AL
    
    ; COLDELAY
    PUSH CX
    MOV CX, 300
DELAY:
    LOOP DELAY
    POP CX
    
    INC BX
    SHL AH, 1
    LOOP SCAN_LOOP
    
    JMP MAIN_LOOP

END START
