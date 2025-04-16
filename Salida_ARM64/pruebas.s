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
	// Operacion de (== | !=)
	// Rune constante: A
	// Pushing char to heap - (A )
	MOV w0, #'A'
	STR x0, [sp, #-8]!
	// Rune constante: A
	// Pushing char to heap - (A )
	MOV w0, #'A'
	STR x0, [sp, #-8]!
	LDR x0, [sp], #8
	LDR x1, [sp], #8
	BL comparar_igual_rune
	// Pushing resultados de la IGUALACION(==)
	STR x0, [sp, #-8]!
	LDR x0, [sp], #8
	MOV x0, x0
	BL print_bool
	MOV x0, 0
	MOV x8, 93
	SVC #0



 // Libreria Estandar

//---------------Aca se comparan dos Runes
//Primer rune en x0
//Segundo rune en x1
//-----------Inicio de la funcion
comparar_igual_rune:
    cmp    x0, x1              // ¿Son iguales los caracteres?
    bne    rune_not_equal   // Si no, las cadenas son diferentes

rune_equal:
    // Aquí entra si las cadenas son iguales
    mov x0, #1
    b       fin_rune

rune_not_equal:
    // Aquí entra si son diferentes
    mov x0, #0
    b       fin_rune

fin_rune:
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
    
