; S1F: Lifecycle
; Writes fixed bytes and spins.
; Used with manual start/stop/reload/close actions.

ORG 1000H

START:
    MOV AL, 0AAH
    OUT 19H, AL
    
    MOV AL, 55H
    OUT 1BH, AL
    
    MOV AL, 41H
    OUT 04H, AL
    
SPIN:
    JMP SPIN

END START
