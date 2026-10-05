	ORG	0100H

	; Configure CS2 to Mode 0, all output
	MOV	AL,10000000B
	OUT	1FH,AL
	; Turn off 7-segment completely (Active low)
	MOV	AL,11111111B
	OUT	19H,AL

	; Configure CS1 (Dot Matrix) to Mode 0, all output
	MOV	AL,10000000B
	OUT	1EH,AL
	
	; Turn off GREEN color on Dot Matrix (active high -> 0 to turn off)
	MOV	AL,00000000B
	OUT	1CH,AL
	
L1:	MOV	SI, OFFSET FONT
    MOV AH, 11111110B  
    
L2:	MOV AL, BYTE PTR CS:[SI]
    ; Red port (18H) is Active Low. We need to invert the font bits so that '1' means ON.
    NOT AL
    OUT 18H, AL
    
    MOV AL, AH
    OUT 1AH, AL
	
	CALL TIMER
	INC SI
	CLC
	ROL AH, 1
	JC L2
	JMP L1
			
TIMER:	
    MOV	CX, 1H
TIMER1:	NOP
        NOP
        NOP
        NOP
        LOOP TIMER1
        RET      
        
; Red Heart FONT
FONT: 
    DB 11111111B ; Col 0
    DB 00000000B ; Col 1
    DB 00000000B ; Col 2
    DB 00000000B ; Col 3
    DB 00000000B ; Col 4
    DB 00000000B ; Col 5
    DB 00000000B ; Col 6
    DB 00000000B ; Col 7
