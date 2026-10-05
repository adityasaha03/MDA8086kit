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
	
L1:	MOV	SI, OFFSET FONT
    MOV AH, 11111110B  
    
L2:	MOV AL, BYTE PTR CS:[SI]
    
    ; Output Green (Active High)
    OUT 1CH, AL
    
    ; Output Red (Active Low) - INVERT to make YELLOW (Green + Red)
    NOT AL
    OUT 18H, AL
    
    ; Output Scanline
    MOV AL, AH
    OUT 1AH, AL
	
	CALL TIMER
	INC SI
	CLC
	ROL AH, 1
	JC L2
	JMP L1
			
TIMER:	
    MOV	CX, 10H
TIMER1:	NOP
        NOP
        NOP
        NOP
        LOOP TIMER1
        RET      
        
FONT: 
    DB 00111100B ; Col 0
    DB 01000010B ; Col 1
    DB 10101001B ; Col 2
    DB 10000101B ; Col 3
    DB 10000101B ; Col 4
    DB 10101001B ; Col 5
    DB 01000010B ; Col 6
    DB 00111100B ; Col 7
