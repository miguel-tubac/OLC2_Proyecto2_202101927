.data
//Esto es el espacio para los strings
heap: .space 4096
//Esto lo voy a usar para Float
zero:           .double 0.0
ten:            .double 10.0
neg_one:        .double -1.0
//Esta es para imprimir dobles
point:      .byte '.'
round_const:    .double 0.0000005 
half:           .double 0.5 
//Esta es para imprimir Bool
msg_true:   .asciz "true"
msg_false:  .asciz "false"
//Esta es para imprimir Int
minus_sign:  .ascii "-"
//Esta es para concatenar Strings
heap_start: .quad 0 
heap_size:  .quad 4096 
heap_used:  .quad 0
//Esto es para imprimir en la consola
space_char: .ascii " " 
newline: .ascii "\n"

.text
.global _start
_start:
	//Esto es para leer Strings
	ADR x10, heap
	// Print statement
	MOV x0, 16
	SUB sp, sp, x0
	// Visitando parametros de la funcion
	// Constante: 0
	MOV x0, 0
	STR x0, [sp, #-8]!
	// Pushing object Int to stack
	// Constante: 0
	MOV x0, 0
	STR x0, [sp, #-8]!
	// Pushing object Int to stack
	MOV x0, 32
	ADD sp, sp, x0
	MOV x0, 8
	SUB x0, sp, x0
	// 	//Esto es para leer los strings
	ADR x1, L8
	STR x1, [sp, #-8]!
	STR x29, [sp, #-8]!
	ADD x29, x0, xzr
	MOV x0, 24
	SUB sp, sp, x0
	// Calling function: ackermann
	BL ackermann
	// Function call Completed
	L8:
	MOV x4, 32
	SUB x4, sp, x4
	LDR x4, [x4, #0]
	MOV x1, 8
	SUB x1, x29, x1
	LDR x29, [x1, #0]
	MOV x0, 40
	ADD sp, sp, x0
	STR x4, [sp, #-8]!
	// Pushing object Int to stack
	// End of function Call: ackermann
	LDR x0, [sp], #8
	MOV x0, x0
	BL print_integer
	BL print_space
	BL print_new_line
	// Finalizando programa
	MOV x0, 0
	MOV x8, 93
	SVC #0



 // Funciones Foraneas
// Pushing object Int to stack
// Pushing object Int to stack
// Function Declaration: ackermann
ackermann:
// Sentencia IF - ELSE
// Operacion de (== | !=)
MOV x0, 16
SUB x0, x29, x0
LDR x0, [x0, #0]
STR x0, [sp, #-8]!
// Pushing object Int to stack
// Constante: 0
MOV x0, 0
STR x0, [sp, #-8]!
// Pushing object Int to stack
LDR x0, [sp], #8
LDR x1, [sp], #8
BL comparar_igual_int
// Pushing resultados de la IGUALACION(==)
STR x0, [sp, #-8]!
// Pushing object Bool to stack
LDR x0, [sp], #8
CBZ x0, L1
// Bloque de nuevo entorno
// Return Stament
// ADD/SUB operaciones
MOV x0, 24
SUB x0, x29, x0
LDR x0, [x0, #0]
STR x0, [sp, #-8]!
// Pushing object Int to stack
// Constante: 1
MOV x0, 1
STR x0, [sp, #-8]!
// Pushing object Int to stack
LDR x0, [sp], #8
LDR x1, [sp], #8
ADD x0, x1, x0
// Pushing resultados de la SUMA
STR x0, [sp, #-8]!
// Pushing object Int to stack
LDR x0, [sp], #8
MOV x1, 32
SUB x1, x29, x1
STR x0, [x1, #0]
B L0
// End of return statement
B L2
L1:
// Sentencia IF - ELSE
// Operacion Logica And(&&)
// Operacion de (> | < | >= | <=)
MOV x0, 16
SUB x0, x29, x0
LDR x0, [x0, #0]
STR x0, [sp, #-8]!
// Pushing object Int to stack
// Constante: 0
MOV x0, 0
STR x0, [sp, #-8]!
// Pushing object Int to stack
LDR x0, [sp], #8
LDR x1, [sp], #8
BL comparar_mayor_int
// Pushing resultados de los RELACIONALES (> | < | >= | <=)
STR x0, [sp, #-8]!
// Pushing object Bool to stack
// Operacion de (== | !=)
MOV x0, 24
SUB x0, x29, x0
LDR x0, [x0, #0]
STR x0, [sp, #-8]!
// Pushing object Int to stack
// Constante: 0
MOV x0, 0
STR x0, [sp, #-8]!
// Pushing object Int to stack
LDR x0, [sp], #8
LDR x1, [sp], #8
BL comparar_igual_int
// Pushing resultados de la IGUALACION(==)
STR x0, [sp, #-8]!
// Pushing object Bool to stack
LDR x0, [sp], #8
LDR x1, [sp], #8
AND x0, x1, x0
// Pushing resultados de AND(&&)
STR x0, [sp, #-8]!
// Pushing object Bool to stack
LDR x0, [sp], #8
CBZ x0, L3
// Bloque de nuevo entorno
// Return Stament
MOV x0, 16
SUB sp, sp, x0
// Visitando parametros de la funcion
// ADD/SUB operaciones
MOV x0, 16
SUB x0, x29, x0
LDR x0, [x0, #0]
STR x0, [sp, #-8]!
// Pushing object Int to stack
// Constante: 1
MOV x0, 1
STR x0, [sp, #-8]!
// Pushing object Int to stack
LDR x0, [sp], #8
LDR x1, [sp], #8
SUB x0, x1, x0
// Pushing resultados de la RESTA
STR x0, [sp, #-8]!
// Pushing object Int to stack
// Constante: 1
MOV x0, 1
STR x0, [sp, #-8]!
// Pushing object Int to stack
MOV x0, 32
ADD sp, sp, x0
MOV x0, 8
SUB x0, sp, x0
// 	//Esto es para leer los strings
ADR x1, L5
STR x1, [sp, #-8]!
STR x29, [sp, #-8]!
ADD x29, x0, xzr
MOV x0, 24
SUB sp, sp, x0
// Calling function: ackermann
BL ackermann
// Function call Completed
L5:
MOV x4, 32
SUB x4, sp, x4
LDR x4, [x4, #0]
MOV x1, 8
SUB x1, x29, x1
LDR x29, [x1, #0]
MOV x0, 40
ADD sp, sp, x0
STR x4, [sp, #-8]!
// Pushing object Int to stack
// End of function Call: ackermann
LDR x0, [sp], #8
MOV x1, 32
SUB x1, x29, x1
STR x0, [x1, #0]
B L0
// End of return statement
// Removeindo 16 bytes del stack
MOV x0, 16
ADD sp, sp, x0
B L4
L3:
// Bloque de nuevo entorno
// Return Stament
MOV x0, 16
SUB sp, sp, x0
// Visitando parametros de la funcion
// ADD/SUB operaciones
MOV x0, 16
SUB x0, x29, x0
LDR x0, [x0, #0]
STR x0, [sp, #-8]!
// Pushing object Int to stack
// Constante: 1
MOV x0, 1
STR x0, [sp, #-8]!
// Pushing object Int to stack
LDR x0, [sp], #8
LDR x1, [sp], #8
SUB x0, x1, x0
// Pushing resultados de la RESTA
STR x0, [sp, #-8]!
// Pushing object Int to stack
MOV x0, 16
SUB sp, sp, x0
// Visitando parametros de la funcion
MOV x0, 16
SUB x0, x29, x0
LDR x0, [x0, #0]
STR x0, [sp, #-8]!
// Pushing object Int to stack
// ADD/SUB operaciones
MOV x0, 24
SUB x0, x29, x0
LDR x0, [x0, #0]
STR x0, [sp, #-8]!
// Pushing object Int to stack
// Constante: 1
MOV x0, 1
STR x0, [sp, #-8]!
// Pushing object Int to stack
LDR x0, [sp], #8
LDR x1, [sp], #8
SUB x0, x1, x0
// Pushing resultados de la RESTA
STR x0, [sp, #-8]!
// Pushing object Int to stack
MOV x0, 32
ADD sp, sp, x0
MOV x0, 8
SUB x0, sp, x0
// 	//Esto es para leer los strings
ADR x1, L7
STR x1, [sp, #-8]!
STR x29, [sp, #-8]!
ADD x29, x0, xzr
MOV x0, 24
SUB sp, sp, x0
// Calling function: ackermann
BL ackermann
// Function call Completed
L7:
MOV x4, 32
SUB x4, sp, x4
LDR x4, [x4, #0]
MOV x1, 8
SUB x1, x29, x1
LDR x29, [x1, #0]
MOV x0, 40
ADD sp, sp, x0
STR x4, [sp, #-8]!
// Pushing object Int to stack
// End of function Call: ackermann
MOV x0, 32
ADD sp, sp, x0
MOV x0, 8
SUB x0, sp, x0
// 	//Esto es para leer los strings
ADR x1, L6
STR x1, [sp, #-8]!
STR x29, [sp, #-8]!
ADD x29, x0, xzr
MOV x0, 24
SUB sp, sp, x0
// Calling function: ackermann
BL ackermann
// Function call Completed
L6:
MOV x4, 32
SUB x4, sp, x4
LDR x4, [x4, #0]
MOV x1, 8
SUB x1, x29, x1
LDR x29, [x1, #0]
MOV x0, 40
ADD sp, sp, x0
STR x4, [sp, #-8]!
// Pushing object Int to stack
// End of function Call: ackermann
LDR x0, [sp], #8
MOV x1, 32
SUB x1, x29, x1
STR x0, [x1, #0]
B L0
// End of return statement
L4:
L2:
L0:
ADD x0, x29, xzr
LDR x30, [x0, #0]
BR x30
// End of Function: ackermann
// Popping object from stack
// Popping object from stack



 // Libreria Estandar

//---------------Aca se comparan dos numeros de tipo int
//Primer numero en X0
//Segundo numero en X1
//-----------Inicio de la funcion
comparar_igual_int:
    cmp x1, x0          // Compara X0 y X1
    beq iguales        // Salta si son iguales
    bne diferentes     // Salta si son diferentes
iguales:
    // Código para cuando son iguales
    mov x0, #1
    b fin
diferentes:
    // Código para cuando NO son iguales
    mov x0, #0
    b fin
fin:
    ret    
    
    


//---------------Aca se comparan dos numeros de tipo int
//Primer numero en X0
//Segundo numero en X1
//-----------Inicio de la funcion
comparar_mayor_int:
    cmp x1, x0          // Compara X0 y X1
    bgt x1_mayor         //  Salta si x1 > x0
    // De lo contrario
    mov x0, #0
    b fin_mayor
x1_mayor:
    // Código para cuando NO son iguales
    mov x0, #1
    b fin_mayor
fin_mayor:
    ret 
    


//--------------------------------------------------------------
// print_integer - Prints a signed integer to stdout
//
// Input:
//   x0 - The integer value to print
//--------------------------------------------------------------
print_integer:
    // Save registers
    stp x29, x30, [sp, #-16]!  // Save frame pointer and link register
    stp x19, x20, [sp, #-16]!  // Save callee-saved registers
    stp x21, x22, [sp, #-16]!
    stp x23, x24, [sp, #-16]!
    stp x25, x26, [sp, #-16]!
    stp x27, x28, [sp, #-16]!
    
    // Check if number is negative
    mov x19, x0                // Save original number
    cmp x19, #0                // Compare with zero
    bge positive_number        // Branch if greater or equal to zero
    
    // Handle negative number
    mov x0, #1                 // fd = 1 (stdout)
    adr x1, minus_sign         // Address of minus sign
    mov x2, #1                 // Length = 1
    mov w8, #64                // Syscall write
    svc #0
    
    neg x19, x19               // Make number positive
    
positive_number:
    // Prepare buffer for converting result to ASCII
    sub sp, sp, #32            // Reserve space on stack
    mov x22, sp                // x22 points to buffer
    
    // Initialize digit counter
    mov x23, #0                // Digit counter
    
    // Handle special case for zero
    cmp x19, #0
    bne convert_loop
    
    // If number is zero, just write '0'
    mov w24, #48               // ASCII '0'
    strb w24, [x22, x23]       // Store in buffer
    add x23, x23, #1           // Increment counter
    b print_result             // Skip conversion loop
    
convert_loop:
    // Divide the number by 10
    mov x24, #10
    udiv x25, x19, x24         // x25 = x19 / 10 (quotient)
    msub x26, x25, x24, x19    // x26 = x19 - (x25 * 10) (remainder)
    
    // Convert remainder to ASCII and store in buffer
    add x26, x26, #48          // Convert to ASCII ('0' = 48)
    strb w26, [x22, x23]       // Store digit in buffer
    add x23, x23, #1           // Increment digit counter
    
    // Prepare for next iteration
    mov x19, x25               // Quotient becomes the new number
    cbnz x19, convert_loop     // If number is not zero, continue
    
    // Reverse the buffer since digits are in reverse order
    mov x27, #0                // Start index
reverse_loop:
    sub x28, x23, x27          // x28 = length - current index
    sub x28, x28, #1           // x28 = length - current index - 1
    
    cmp x27, x28               // Compare indices
    bge print_result           // If crossed, finish reversing
    
    // Swap characters
    ldrb w24, [x22, x27]       // Load character from start
    ldrb w25, [x22, x28]       // Load character from end
    strb w25, [x22, x27]       // Store end character at start
    strb w24, [x22, x28]       // Store start character at end
    
    add x27, x27, #1           // Increment start index
    b reverse_loop             // Continue reversing
    
print_result:    
    // Print the result
    mov x0, #1                 // fd = 1 (stdout)
    mov x1, x22                // Buffer address
    mov x2, x23                // Buffer length
    mov w8, #64                // Syscall write
    svc #0
    
    // Clean up and restore registers
    add sp, sp, #32            // Free buffer space
    ldp x27, x28, [sp], #16    // Restore callee-saved registers
    ldp x25, x26, [sp], #16
    ldp x23, x24, [sp], #16
    ldp x21, x22, [sp], #16
    ldp x19, x20, [sp], #16
    ldp x29, x30, [sp], #16    // Restore frame pointer and link register
    ret                        // Return to caller

    


print_space:
    // Print space character (ASCII 32)
    ldr     x1, =space_char     // Dirección del carácter espacio
    mov     x0, #1              // File descriptor: stdout
    mov     x2, #1              // Longitud: 1 byte
    mov     x8, #64             // syscall: write
    svc     #0

    ret
    


print_new_line:
    // Print newline character
    ldr     x1, =newline        // Address of newline character
    mov     x0, #1              // File descriptor: stdout
    mov     x2, #1              // Length of 1 byte
    mov     x8, #64             // syscall: write
    svc     #0

    ret
    
