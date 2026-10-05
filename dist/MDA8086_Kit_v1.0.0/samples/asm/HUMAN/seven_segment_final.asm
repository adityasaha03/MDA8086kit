;Rename the register addresses        
PPIC_C EQU 1FH
PPIC EQU 1DH
PPIB EQU 1BH
PPIA EQU 19H 

;----------------------------------
	; Turn off Dot Matrix completely
	MOV	AL,10000000B
	OUT	1EH,AL        ; CS1 Control Register
	MOV	AL,00000000B
	OUT	1CH,AL        ; Green OFF
	MOV	AL,11111111B
	OUT	18H,AL        ; Red OFF
    MOV AL,11111111B
    OUT 1AH,AL        ; Scan OFF
;-------------------------------------  

MOV AL, 10000000B
OUT PPIC_C, AL      ;goes to control register
MOV AL, 11110000B
OUT PPIB, AL        ;goes to PORT B
MOV AL, 00000000B
OUT PPIC, AL        ;goes to PORT C     


L1:    
    MOV AL, 11000000B  
    OUT PPIA, AL    ;goes to PORT A (which is dedicated to seven segment)
    
    MOV AL, 11111001B
    OUT PPIA, AL   
    
    MOV AL, 10100100B
    OUT PPIA,AL  
    
    MOV AL, 10110000B
    OUT PPIA,AL  
    
    MOV AL, 10011001B
    OUT PPIA,AL  
    
    MOV AL, 10010010B
    OUT PPIA,AL       
   
    MOV AL, 10000010B
    OUT PPIA,AL 
    
    MOV AL, 11111000B
    OUT PPIA,AL       
  
    MOV AL, 10000000B
    OUT PPIA,AL   
    MOV AL, 10010000B
    OUT PPIA,AL     
    
    MOV AL, 11000000B
    JMP L1