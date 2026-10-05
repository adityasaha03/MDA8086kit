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
	
	; Turn off GREEN color on Dot Matrix (Active High -> 0 is OFF)
	MOV	AL,00000000B
	OUT	1CH,AL
	
	; Turn off RED color on Dot Matrix (Active Low -> 1 is OFF)
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
    NOT AL              ; Invert because Red port is Active-Low!
    OUT 18H, AL         ; Output to RED port instead of Green
    
    MOV AL, AH
    OUT 1AH, AL         ; Output Scan line
	
	CALL TIMER
	INC SI
	CLC
	ROL AH, 1
	JC L2
	JMP L1
			
TIMER:	
    ; Reduced delay for emu8086
    MOV	CX, 10H         ; Changed to 10H for better stability
TIMER1:	NOP
        NOP
        NOP
        NOP
        LOOP TIMER1
        RET      
        
        
FONT: 
    DB 00000000B
    DB 00111111B
    DB 01001000B
    DB 10001000B
    DB 10001000B
    DB 01001000B
    DB 00111111B
    DB 00000000B
