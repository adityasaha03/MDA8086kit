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
	
	; Turn off RED color on Dot Matrix
	MOV	AL,11111111B
	OUT	18H,AL
	
L1:	MOV	SI, OFFSET FONT
    MOV AH, 11111110B  
    
L2:	
    ; Prevent Ghosting: Turn off all scan lines before updating data
    PUSH AX
    MOV AL, 11111111B
    OUT 1AH, AL
    POP AX
    
    MOV AL, BYTE PTR CS:[SI]
    OUT 1CH, AL
    
    MOV AL, AH
    OUT 1AH, AL
	
	CALL TIMER
	INC SI
	CLC
	ROL AH, 1
	JC L2
	JMP L1
			
TIMER:	
    ; Reduced delay for emu8086 (so it doesn't take 2 minutes)
    MOV	CX, 1H
TIMER1:	NOP
        NOP
        NOP
        NOP
        LOOP TIMER1
        RET      
        
        
FONT: 
    DB 00011000B
    DB 11111111B
    DB 11000000B
    DB 10000001B
    DB 00000000B
    DB 11100111B
    DB 00011000B
    DB 11111111B