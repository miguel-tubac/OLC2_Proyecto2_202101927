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
	// Variable declaracion Explixita: name1
	// Flotante: 4.2
	LDR x0, =0x4010CCCCC0000000
	FMOV d0, x0
	STR d0, [sp, #-8]!
	// Fin de conversion de cadena a Float
	// Final de la lectura del float
	// DECREMENTO operacion (-=)
	// Constante: 2
	MOV x0, 2
	STR x0, [sp, #-8]!
	LDR x1, [sp], #8
	MOV x0, 0
	ADD x0, sp, x0
	LDR d0, [x0, #0]
	// Se realiza la Resta de los dos valores
	SCVTF d1, x1
	FSUB d0, d0, d1
	// Pushing resultados de la RESTA(-=)
	STR d0, [sp, #-8]!
	STR d0, [sp, #-8]!
	// Popping descartando el valor
	LDR d0, [sp], #8
	// Print statement
	MOV x0, 0
	ADD x0, sp, x0
	LDR x0, [x0, #0]
	STR x0, [sp, #-8]!
	LDR d0, [sp], #8
	FMOV d0, d0
	BL print_double
	MOV x0, 0
	MOV x8, 93
	SVC #0



 // Libreria Estandar


//La endrada debe de ingresar en el registro d0
print_double:
    stp x29, x30, [sp, #-80]!
    stp x10, x19, [sp, #-80]!
    mov x29, sp
    
    // Inicializar buffer
    add x1, sp, #16
    mov x20, x1
    
    // Manejar signo
    adrp x8, zero
    add x8, x8, :lo12:zero
    ldr d1, [x8]
    fcmp d0, d1
    bge positive
    mov w3, #'-'
    strb w3, [x1], #1
    adrp x8, neg_one
    add x8, x8, :lo12:neg_one
    ldr d1, [x8]
    fmul d0, d0, d1
    mov x19, #1
    b after_sign

positive:
    mov x19, #0

after_sign:
    // Redondear
    adrp x21, round_const
    add x21, x21, :lo12:round_const
    ldr d3, [x21]
    fadd d0, d0, d3
    
    // Separar parte entera y decimal
    frintz d1, d0
    fsub d2, d0, d1
    
    // Convertir parte entera
    mov x4, x1

convert_int_loop:
    adrp x22, ten
    add x22, x22, :lo12:ten
    ldr d4, [x22]
    
    fdiv d5, d1, d4
    frintz d5, d5
    fmul d6, d5, d4
    fsub d6, d1, d6
    fcvtzs w5, d6
    
    add w5, w5, #'0'
    strb w5, [x1], #1
    
    fmov d1, d5
    adrp x8, zero
    add x8, x8, :lo12:zero
    ldr d8, [x8]
    fcmp d1, d8
    bne convert_int_loop
    
    // Invertir dígitos
    mov x5, x4
    sub x6, x1, #1

reverse_int_loop:
    cmp x5, x6
    bge reverse_done
    ldrb w7, [x5]
    ldrb w8, [x6]
    strb w8, [x5], #1
    strb w7, [x6], #-1
    b reverse_int_loop

reverse_done:
    // Añadir punto decimal
    adrp x9, point
    add x9, x9, :lo12:point
    ldrb w9, [x9]
    strb w9, [x1], #1
    
    // Convertir parte decimal
    mov w10, #6

convert_decimal_loop:
    cbz w10, decimal_done
    
    adrp x22, ten
    add x22, x22, :lo12:ten
    ldr d4, [x22]
    fmul d2, d2, d4
    
    frintz d5, d2
    fcvtzs w11, d5
    
    cmp w10, #1
    bne no_final_round
    fsub d6, d2, d5
    adrp x23, half
    add x23, x23, :lo12:half
    ldr d7, [x23]
    fcmp d6, d7
    blt no_final_round
    add w11, w11, #1
    
no_final_round:
    cmp w11, #10
    blt store_decimal
    mov w11, #9
    
store_decimal:
    fsub d2, d2, d5
    
    add w11, w11, #'0'
    strb w11, [x1], #1
    
    sub w10, w10, #1
    b convert_decimal_loop

decimal_done:
    // Añadir salto de línea
    adrp x12, newline
    add x12, x12, :lo12:newline
    ldrb w12, [x12]
    strb w12, [x1], #1
    
    // Calcular longitud
    sub x2, x1, x20
    cmp x19, #1
    bne print_output
    sub x20, x20, #1
    add x2, x2, #1
    
print_output:
    // Escribir
    mov x0, #1
    mov x1, x20
    mov x8, #64
    svc #0
    
    ldp x10, x19, [sp], #80
    ldp x29, x30, [sp], #80
    ret
    
