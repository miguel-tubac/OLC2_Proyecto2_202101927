.data
heap: .space 4096
newline: .ascii "\n"
zero:           .double 0.0
one:            .double 1.0
ten:            .double 10.0
neg_one:        .double -1.0
//Esta es para imprimir dobles
point:      .byte '.'
round_const:    .double 0.0000005 
half:           .double 0.5 
//Esta es para imprimir Bool
msg_true:   .asciz "true\n"
msg_false:  .asciz "false\n"

.text
.global _start
_start:
	adrp x10, heap
	add x10, x10, :lo12:heap
	// Print statement
	// Rune constante: M
	// Pushing char to heap - (M )
	MOV w0, #'M'
	STR x0, [sp, #-8]!
	LDR x0, [sp], #8
	MOV x0, x0
	BL print_char
	MOV x0, 0
	MOV x8, 93
	SVC #0



 // Libreria Estandar

// ------------------------
// FUNCION PARA IMPRIMIR UN SOLO CARÁCTER
// El valor del caracter viene en el reguistro X0
print_char:
    // Save registers
    stp x29, x30, [sp, #-16]!
    stp x19, x20, [sp, #-16]!
    stp x21, x22, [sp, #-16]!
    stp x23, x24, [sp, #-16]!
    stp x25, x26, [sp, #-16]!
    stp x27, x28, [sp, #-16]!

    sub sp, sp, #16       // Reservar espacio en el stack
    strb w0, [sp]         // Guardar el carácter (1 byte)
    
    mov x1, sp            // Dirección del carácter
    mov x2, #1            // Longitud = 1 byte
    mov x0, #1            // stdout
    mov x8, #64           // syscall write
    svc #0

    // Print newline character
    ldr     x1, =newline        // Address of newline character
    mov     x0, #1              // File descriptor: stdout
    mov     x2, #1              // Length of 1 byte
    mov     x8, #64             // syscall: write
    svc     #0

    add sp, sp, #16       // Liberar el stack

    // Restore registers
    ldp x27, x28, [sp], #16
    ldp x25, x26, [sp], #16
    ldp x23, x24, [sp], #16
    ldp x21, x22, [sp], #16
    ldp x19, x20, [sp], #16
    ldp x29, x30, [sp], #16
    ret
    
