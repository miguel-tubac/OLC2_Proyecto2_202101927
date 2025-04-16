.data
heap: .space 4096
//Esto lo voy a usar para Float
newline: .ascii "\n"
zero:           .double 0.0
ten:            .double 10.0
neg_one:        .double -1.0
//Esta es para imprimir dobles
point:      .byte '.'
round_const:    .double 0.0000005 
half:           .double 0.5 
//Esta es para imprimir Bool
msg_true:   .asciz "true\n"
msg_false:  .asciz "false\n"
//Esta es para imprimir Int
minus_sign:  .ascii "-"
//Esta es para concatenar Strings
heap_start: .quad 0 
heap_size:  .quad 4096 
heap_used:  .quad 0

.text
.global _start
_start:
	ADR x10, heap
	// Print statement
	// Operacion de (> | < | >= | <=)
	// Constante: 5
	MOV x0, 5
	STR x0, [sp, #-8]!
	// Constante: 3
	MOV x0, 3
	STR x0, [sp, #-8]!
	LDR x0, [sp], #8
	LDR x1, [sp], #8
	BL comparar_menorIgual_int
	// Pushing resultados de los RELACIONALES (> | < | >= | <=)
	STR x0, [sp, #-8]!
	LDR x0, [sp], #8
	MOV x0, x0
	BL print_bool
	// Print statement
	// Operacion de (> | < | >= | <=)
	// Flotante: 2.5
	LDR x0, =0x4004000000000000
	FMOV d0, x0
	STR d0, [sp, #-8]!
	// Fin de conversion de cadena a Float
	// Final de la lectura del float
	// Flotante: 2.5
	LDR x0, =0x4004000000000000
	FMOV d0, x0
	STR d0, [sp, #-8]!
	// Fin de conversion de cadena a Float
	// Final de la lectura del float
	LDR d0, [sp], #8
	LDR d1, [sp], #8
	BL comparar_menorIgual_float
	// Pushing resultados de los RELACIONALES (> | < | >= | <=)
	STR x0, [sp, #-8]!
	LDR x0, [sp], #8
	MOV x0, x0
	BL print_bool
	// Print statement
	// Operacion de (> | < | >= | <=)
	// Constante: 4
	MOV x0, 4
	STR x0, [sp, #-8]!
	// Flotante: 2.01
	LDR x0, =0x4000147AE0000000
	FMOV d0, x0
	STR d0, [sp, #-8]!
	// Fin de conversion de cadena a Float
	// Final de la lectura del float
	LDR d0, [sp], #8
	LDR x1, [sp], #8
	SCVTF d1, x1
	BL comparar_menorIgual_float
	// Pushing resultados de los RELACIONALES (> | < | >= | <=)
	STR x0, [sp, #-8]!
	LDR x0, [sp], #8
	MOV x0, x0
	BL print_bool
	// Print statement
	// Operacion de (> | < | >= | <=)
	// Flotante: 2.0
	LDR x0, =0x4000000000000000
	FMOV d0, x0
	STR d0, [sp, #-8]!
	// Fin de conversion de cadena a Float
	// Final de la lectura del float
	// Constante: 2
	MOV x0, 2
	STR x0, [sp, #-8]!
	LDR x0, [sp], #8
	LDR d1, [sp], #8
	SCVTF d0, x0
	BL comparar_menorIgual_float
	// Pushing resultados de los RELACIONALES (> | < | >= | <=)
	STR x0, [sp, #-8]!
	LDR x0, [sp], #8
	MOV x0, x0
	BL print_bool
	// Print statement
	// Operacion de (> | < | >= | <=)
	// Rune constante: B
	// Pushing char to heap - (B )
	MOV w0, #'B'
	STR x0, [sp, #-8]!
	// Rune constante: A
	// Pushing char to heap - (A )
	MOV w0, #'A'
	STR x0, [sp, #-8]!
	LDR x0, [sp], #8
	LDR x1, [sp], #8
	BL comparar_mayorIgual_rune
	// Pushing resultados de los RELACIONALES (> | < | >= | <=)
	STR x0, [sp, #-8]!
	LDR x0, [sp], #8
	MOV x0, x0
	BL print_bool
	MOV x0, 0
	MOV x8, 93
	SVC #0



 // Libreria Estandar

//---------------Aca se comparan dos numeros de tipo int
//Primer numero en X0
//Segundo numero en X1
//-----------Inicio de la funcion
comparar_menorIgual_int:
    cmp x1, x0          // Compara X0 y X1
    ble x1_menorIgual         //  Salta si x1 <= x0
    // De lo contrario
    mov x0, #0
    b fin_menorIgual
x1_menorIgual:
    // Código para cuando NO son iguales
    mov x0, #1
    b fin_menorIgual
fin_menorIgual:
    ret 
    



// Input:
//   x0 - The address
//--------------------------------------------------------------
print_bool:
    // Save registers
    stp x29, x30, [sp, #-16]!
    stp x19, x20, [sp, #-16]!
    stp x21, x22, [sp, #-16]!
    stp x23, x24, [sp, #-16]!
    stp x25, x26, [sp, #-16]!
    stp x27, x28, [sp, #-16]!

    cmp x0, #1
    beq print_true

print_false:
    ldr x0, =msg_false
    mov x1, x0         
    mov x2, #6         
    b print_msg_common

print_true:
    ldr x0, =msg_true
    mov x1, x0         
    mov x2, #5         

print_msg_common:
    mov x0, #1         
    mov x8, #64        
    svc #0

    // Restore registers
    ldp x27, x28, [sp], #16
    ldp x25, x26, [sp], #16
    ldp x23, x24, [sp], #16
    ldp x21, x22, [sp], #16
    ldp x19, x20, [sp], #16
    ldp x29, x30, [sp], #16
    ret
    


//---------------Aca se comparan dos numeros de tipos float
//Primer numero en D0
//Segundo numero en D1
//-----------Inicio de la funcion
comparar_menorIgual_float:
    fcmp D1, D0          
    ble d1_menorIgual         //  Salta si d1 <= d0
    // Código para cuando son iguales
    mov x0, #0
    b fin_menorIgual_float
d1_menorIgual:
    // Código para cuando NO son iguales
    mov x0, #1
    b fin_menorIgual_float
fin_menorIgual_float:
    ret 
    


//---------------Aca se comparan dos Runes
//Primer rune en x0
//Segundo rune en x1
//-----------Inicio de la funcion
comparar_mayorIgual_rune:
    cmp x1, x0              
    bge x1_mayorIgual_rune   // Salta si x1 >= x0
    //De lo contrario
    mov x0, #0
    b fin_mayorIgual_rune
x1_mayorIgual_rune:
    // Aquí entra si son diferentes
    mov x0, #1
    b fin_mayorIgual_rune
fin_mayorIgual_rune:
    ret
    
