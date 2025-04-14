.data
input_buffer:   .skip 100             // Espacio para la entrada
zero:           .double 0.0
one:            .double 1.0
ten:            .double 10.0
neg_one:        .double -1.0
point:      .byte '.'
newline:    .byte '\n'
// Añadir estas constantes
round_const:    .double 0.0000005     // Constante para redondeo
half:           .double 0.5           // Media unidad para redondeo

.text
.global _start
_start:
    BL read_double
    BL print_double    // Imprime el resultado
    MOV x0, 0
    MOV x8, 93
    SVC #0



read_double:
    // Guardamos registros
    stp     x29, x30, [sp, #-16]!
    mov     x29, sp

    // Leer texto del usuario
    mov     x0, #0                  // stdin
    ldr     x1, =input_buffer       // buffer destino
    mov     x2, #100                // tamaño máximo
    mov     x8, #63                 // syscall read
    svc     #0

    // Inicializar registros
    ldr     x1, =input_buffer       // x1 = puntero actual
    ldr     d0, zero                // d0 = 0.0 (resultado)
    ldr     d2, one                 // d2 = 1.0 (divisor)
    mov     w2, #0                  // w2 = modo decimal (0 = entero)
    mov     w4, #0                  // w4 = signo (0 = positivo, 1 = negativo)

    // Verificar si hay signo negativo
    ldrb    w3, [x1]
    cmp     w3, #'-'
    bne     read_loop
    mov     w4, #1                  // Marcar como negativo
    add     x1, x1, #1              // Saltar el signo -

read_loop:
    ldrb    w3, [x1], #1            // Leer siguiente char y avanzar
    cmp     w3, #10                 // fin de entrada (salto de línea)
    beq     end_read
    cmp     w3, #0                  // fin de string
    beq     end_read

    cmp     w3, #'.'                // ¿es punto?
    bne     not_decimal
    mov     w2, #1                  // Activar modo decimal
    b       read_loop

not_decimal:
    // Verificar que es un dígito válido
    cmp     w3, #'0'
    blt     read_loop               // Ignorar caracteres no válidos
    cmp     w3, #'9'
    bgt     read_loop

    // Convertir char ASCII a dígito
    sub     w3, w3, #'0'            // Convertir a número (char - '0')
    scvtf   d3, w3                  // d3 = float(dígito)

    cmp     w2, #0                  // ¿modo entero?
    beq     int_part

    // Parte decimal
    ldr     d4, ten
    fmul    d2, d2, d4              // divisor *= 10
    fdiv    d3, d3, d2              // d3 = dígito / divisor
    fadd    d0, d0, d3              // acumular en resultado
    b       read_loop

int_part:
    ldr     d4, ten
    fmul    d0, d0, d4              // resultado *= 10
    fadd    d0, d0, d3              // resultado += dígito
    b       read_loop

end_read:
    // Aplicar signo negativo si es necesario
    cmp     w4, #1
    bne     done
    ldr     d1, neg_one
    fmul    d0, d0, d1

done:
    ldp     x29, x30, [sp], #16
    ret













print_double:
    // Guardar registros y preparar stack frame
    stp     x29, x30, [sp, #-80]!
    mov     x29, sp
    
    // Inicializar buffer de salida (64 bytes)
    add     x1, sp, #16             // x1 = puntero al buffer
    mov     x20, x1                  // x20 = inicio del buffer (para calcular longitud)
    
    // Manejar signo negativo
    ldr     d1, zero                // Cargar 0.0
    fcmp    d0, d1                  // Comparar con el número de entrada
    bge     positive                // Si es positivo, saltar
    mov     w3, #'-'                // Es negativo, preparar signo '-'
    strb    w3, [x1], #1            // Escribir signo en buffer
    ldr     d1, neg_one             // Cargar -1.0
    fmul    d0, d0, d1              // Convertir a positivo
    mov     x19, #1                 // Bandera: número era negativo
    b       after_sign

positive:
    mov     x19, #0                 // Bandera: número era positivo

after_sign:
    // Redondear a 6 decimales (ajuste de precisión)
    adrp    x21, round_const        // Cargar dirección de round_const
    add     x21, x21, :lo12:round_const
    ldr     d3, [x21]               // d3 = 0.0000005
    fadd    d0, d0, d3              // Aplicar redondeo
    
    // Separar parte entera y decimal
    frintz  d1, d0                  // Parte entera truncada
    fsub    d2, d0, d1              // Parte decimal
    
    // Convertir parte entera a dígitos ASCII
    mov     x4, x1                  // x4 = inicio de los dígitos enteros

convert_int_loop:
    adrp    x22, ten                // Cargar 10.0
    add     x22, x22, :lo12:ten
    ldr     d4, [x22]
    
    fdiv    d5, d1, d4              // d5 = entera / 10
    frintz  d5, d5                  // Parte entera del resultado
    fmul    d6, d5, d4              // d6 = parte entera * 10
    fsub    d6, d1, d6              // d6 = dígito (entera % 10)
    fcvtzs  w5, d6                  // Convertir dígito a entero
    
    add     w5, w5, #'0'            // Convertir a ASCII
    strb    w5, [x1], #1            // Almacenar en buffer
    
    fmov    d1, d5                  // Actualizar parte entera
    ldr     d8, zero
    fcmp    d1, d8                  // ¿Terminamos?
    bne     convert_int_loop        // Si no, continuar
    
    // Los dígitos enteros están en orden inverso, necesitamos invertirlos
    mov     x5, x4                  // x5 = inicio
    sub     x6, x1, #1              // x6 = fin
    
reverse_int_loop:
    cmp     x5, x6
    bge     reverse_done
    ldrb    w7, [x5]                // Intercambiar bytes
    ldrb    w8, [x6]
    strb    w8, [x5], #1
    strb    w7, [x6], #-1
    b       reverse_int_loop

reverse_done:
    // Añadir punto decimal
    ldr     x9, =point
    ldrb    w9, [x9]
    strb    w9, [x1], #1
    
    // Convertir parte decimal (6 dígitos)
    mov     w10, #6                 // Contador de dígitos decimales

convert_decimal_loop:
    cbz     w10, decimal_done       // ¿Terminamos?
    
    // Multiplicar por 10 para obtener siguiente dígito
    adrp    x22, ten
    add     x22, x22, :lo12:ten
    ldr     d4, [x22]
    fmul    d2, d2, d4
    
    // Obtener dígito (parte entera)
    frintz  d5, d2                  // Parte entera truncada
    fcvtzs  w11, d5                 // Dígito como entero
    
    // Redondear solo el último dígito
    cmp     w10, #1
    bne     no_final_round
    fsub    d6, d2, d5              // Parte fraccional restante
    adrp    x23, half
    add     x23, x23, :lo12:half
    ldr     d7, [x23]               // d7 = 0.5
    fcmp    d6, d7
    blt     no_final_round
    add     w11, w11, #1            // Redondear arriba
    
no_final_round:
    // Asegurar que el dígito sea válido (0-9)
    cmp     w11, #10
    blt     store_decimal
    mov     w11, #9
    
store_decimal:
    fsub    d2, d2, d5              // Actualizar parte decimal
    
    add     w11, w11, #'0'          // Convertir a ASCII
    strb    w11, [x1], #1           // Almacenar en buffer
    
    sub     w10, w10, #1            // Decrementar contador
    b       convert_decimal_loop

decimal_done:
    // Añadir salto de línea
    ldr     x12, =newline
    ldrb    w12, [x12]
    strb    w12, [x1], #1
    
    // Calcular longitud de la cadena
    sub     x2, x1, x20             // Longitud total
    cmp     x19, #1                 // ¿Era negativo?
    bne     print_output
    sub     x20, x20, #1            // Ajustar inicio para incluir '-'
    add     x2, x2, #1              // Ajustar longitud
    
print_output:
    // Escribir por stdout
    mov     x0, #1                  // stdout
    mov     x1, x20                 // buffer
    mov     x8, #64                 // syscall write
    svc     #0
    
    // Restaurar registros y retornar
    ldp     x29, x30, [sp], #80
    ret






/* 
//Este si funciona con solo 6 decimales no imprime mas
print_double:
    // Guardar registros y preparar stack frame
    stp     x29, x30, [sp, #-80]!
    mov     x29, sp
    
    // Inicializar buffer de salida (64 bytes)
    add     x1, sp, #16             // x1 = puntero al buffer
    mov     x20, x1                  // x20 = inicio del buffer (para calcular longitud)
    
    // Manejar signo negativo
    ldr     d1, zero                // Cargar 0.0
    fcmp    d0, d1                  // Comparar con el número de entrada
    bge     positive                // Si es positivo, saltar
    mov     w3, #'-'                // Es negativo, preparar signo '-'
    strb    w3, [x1], #1            // Escribir signo en buffer
    ldr     d1, neg_one             // Cargar -1.0
    fmul    d0, d0, d1              // Convertir a positivo
    mov     x19, #1                 // Bandera: número era negativo
    b       after_sign

positive:
    mov     x19, #0                 // Bandera: número era positivo

after_sign:
    // Redondear a 6 decimales (ajuste de precisión)
    adrp    x21, round_const        // Cargar dirección de round_const
    add     x21, x21, :lo12:round_const
    ldr     d3, [x21]               // d3 = 0.0000005
    fadd    d0, d0, d3              // Aplicar redondeo
    
    // Separar parte entera y decimal
    frintz  d1, d0                  // Parte entera truncada
    fsub    d2, d0, d1              // Parte decimal
    
    // Convertir parte entera a dígitos ASCII
    mov     x4, x1                  // x4 = inicio de los dígitos enteros

convert_int_loop:
    adrp    x22, ten                // Cargar 10.0
    add     x22, x22, :lo12:ten
    ldr     d4, [x22]
    
    fdiv    d5, d1, d4              // d5 = entera / 10
    frintz  d5, d5                  // Parte entera del resultado
    fmul    d6, d5, d4              // d6 = parte entera * 10
    fsub    d6, d1, d6              // d6 = dígito (entera % 10)
    fcvtzs  w5, d6                  // Convertir dígito a entero
    
    add     w5, w5, #'0'            // Convertir a ASCII
    strb    w5, [x1], #1            // Almacenar en buffer
    
    fmov    d1, d5                  // Actualizar parte entera
    ldr     d8, zero
    fcmp    d1, d8                  // ¿Terminamos?
    bne     convert_int_loop        // Si no, continuar
    
    // Los dígitos enteros están en orden inverso, necesitamos invertirlos
    mov     x5, x4                  // x5 = inicio
    sub     x6, x1, #1              // x6 = fin
    
reverse_int_loop:
    cmp     x5, x6
    bge     reverse_done
    ldrb    w7, [x5]                // Intercambiar bytes
    ldrb    w8, [x6]
    strb    w8, [x5], #1
    strb    w7, [x6], #-1
    b       reverse_int_loop

reverse_done:
    // Añadir punto decimal
    ldr     x9, =point
    ldrb    w9, [x9]
    strb    w9, [x1], #1
    
    // Convertir parte decimal (6 dígitos)
    mov     w10, #6                 // Contador de dígitos decimales

convert_decimal_loop:
    cbz     w10, decimal_done       // ¿Terminamos?
    
    // Multiplicar por 10 para obtener siguiente dígito
    adrp    x22, ten
    add     x22, x22, :lo12:ten
    ldr     d4, [x22]
    fmul    d2, d2, d4
    
    // Obtener dígito (parte entera)
    frintz  d5, d2                  // Parte entera truncada
    fcvtzs  w11, d5                 // Dígito como entero
    
    // Redondear solo el último dígito
    cmp     w10, #1
    bne     no_final_round
    fsub    d6, d2, d5              // Parte fraccional restante
    adrp    x23, half
    add     x23, x23, :lo12:half
    ldr     d7, [x23]               // d7 = 0.5
    fcmp    d6, d7
    blt     no_final_round
    add     w11, w11, #1            // Redondear arriba
    
no_final_round:
    // Asegurar que el dígito sea válido (0-9)
    cmp     w11, #10
    blt     store_decimal
    mov     w11, #9
    
store_decimal:
    fsub    d2, d2, d5              // Actualizar parte decimal
    
    add     w11, w11, #'0'          // Convertir a ASCII
    strb    w11, [x1], #1           // Almacenar en buffer
    
    sub     w10, w10, #1            // Decrementar contador
    b       convert_decimal_loop

decimal_done:
    // Añadir salto de línea
    ldr     x12, =newline
    ldrb    w12, [x12]
    strb    w12, [x1], #1
    
    // Calcular longitud de la cadena
    sub     x2, x1, x20             // Longitud total
    cmp     x19, #1                 // ¿Era negativo?
    bne     print_output
    sub     x20, x20, #1            // Ajustar inicio para incluir '-'
    add     x2, x2, #1              // Ajustar longitud
    
print_output:
    // Escribir por stdout
    mov     x0, #1                  // stdout
    mov     x1, x20                 // buffer
    mov     x8, #64                 // syscall write
    svc     #0
    
    // Restaurar registros y retornar
    ldp     x29, x30, [sp], #80
    ret*/
