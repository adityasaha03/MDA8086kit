;Rename the register addresses        
PPIC_C EQU 1FH
PPIC EQU 1DH
PPIB EQU 1BH
PPIA EQU 19H   

MOV AL, 10000000B
OUT PPIC_C, AL     
MOV AL, 11111111B
OUT PPIA, AL        ;goes to PORT A inactive, as it works on active low
MOV AL, 00000000B
OUT PPIC, AL            


L1:    
    MOV AL, 00001011B  
    OUT PPIB, AL    
    
    JMP L1