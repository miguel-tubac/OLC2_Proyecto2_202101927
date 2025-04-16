//Aca estan las instrucciones estandares de ARM64

public class StandardLibrary
{
    private readonly HashSet<string> UsedFunctions = new HashSet<string>();

    public void Use(string function)
    {
        UsedFunctions.Add(function);
    }

    public string GetFunctionDefinitions()
    {
        var functions = new List<string>();
        foreach (var function in UsedFunctions)
        {
            if (FunctionDefinitions.TryGetValue(function, out var definition))
            {
                functions.Add(definition);
            }
        }
        return string.Join("\n\n", functions);
    }

    private readonly static Dictionary<string, string> FunctionDefinitions = new Dictionary<string, string>
    {
        { "print_integer", @"
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
    // Add newline
    mov w24, #10               // Newline character
    strb w24, [x22, x23]       // Add to end of buffer
    add x23, x23, #1           // Increment counter
    
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

    "},
    
    
    { "print_string", @"
//--------------------------------------------------------------
// print_string - Prints a null-terminated string to stdout
//
// Input:
//   x0 - The address of the null-terminated string to print
//--------------------------------------------------------------
print_string:
    // Save link register and other registers we'll use
    stp     x29, x30, [sp, #-16]!
    stp     x19, x20, [sp, #-16]!
    
    // x19 will hold the string address
    mov     x19, x0
    
print_loop:
    // Load a byte from the string
    ldrb    w20, [x19]
    
    // Check if it's the null terminator (0)
    cbz     w20, print_done
    
    // Prepare for write syscall
    mov     x0, #1              // File descriptor: 1 for stdout
    mov     x1, x19             // Address of the character to print
    mov     x2, #1              // Length: 1 byte
    mov     x8, #64             // syscall: write (64 on ARM64)
    svc     #0                  // Make the syscall
    
    // Move to the next character
    add     x19, x19, #1
    
    // Continue the loop
    b       print_loop
    
print_done:
    // Print newline character
    ldr     x1, =newline        // Address of newline character
    mov     x0, #1              // File descriptor: stdout
    mov     x2, #1              // Length of 1 byte
    mov     x8, #64             // syscall: write
    svc     #0

    // Restore saved registers
    ldp     x19, x20, [sp], #16
    ldp     x29, x30, [sp], #16
    ret
    " },

    {"print_double", @"

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
    "},


    {"print_bool", @"

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
    "},

    {"print_char",@"
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
    "},

    {"concat_strings", @"
// Suponiendo que:
// x0 = dirección de la primera cadena (null-terminated)
// x1 = dirección de la segunda cadena (null-terminated)
// El resultado quedará en x0 (debes liberar esta memoria después)
concat_strings:
    // Guardar registros que usaremos
    stp x29, x30, [sp, #-48]!
    stp x19, x20, [sp, #16]
    stp x21, x22, [sp, #32]
    
    // Guardar las cadenas originales
    mov x19, x1  // Cadena 1
    mov x20, x0  // Cadena 2
    
    // Calcular longitud de cadena 1
    mov x0, x19
    bl strlen
    mov x21, x0  // Longitud cadena 1
    
    // Calcular longitud de cadena 2
    mov x0, x20
    bl strlen
    mov x22, x0  // Longitud cadena 2
    
    // Reservar memoria para nueva cadena (long1 + long2 + 1)
    add x0, x21, x22
    add x0, x0, #1
    bl malloc     // Asume que tienes una función malloc implementada
    mov x23, x0   // Guardar puntero a nueva cadena
    
    // Copiar cadena 1
    mov x0, x23
    mov x1, x19
    mov x2, x21
    bl memcpy
    
    // Copiar cadena 2
    add x0, x23, x21  // Posición después de cadena 1
    mov x1, x20
    mov x2, x22
    bl memcpy
    
    // Añadir null-terminator
    add x1, x21, x22
    strb wzr, [x23, x1]
    
    // Devolver resultado en x0
    mov x0, x23
    
    // Restaurar registros
    ldp x21, x22, [sp, #32]
    ldp x19, x20, [sp, #16]
    ldp x29, x30, [sp], #48
    ret

// Función auxiliar strlen
strlen:
    mov x2, #0
1:  ldrb w1, [x0], #1
    cbz w1, 2f
    add x2, x2, #1
    b 1b
2:  mov x0, x2
    ret

// Función auxiliar memcpy
memcpy:
    cbz x2, 2f
1:  ldrb w3, [x1], #1
    strb w3, [x0], #1
    sub x2, x2, #1
    cbnz x2, 1b
2:  ret




//La función malloc (memory allocation) es una de las funciones más fundamentales en programación de sistemas y sirve para:
//Reservar memoria dinámica en tiempo de ejecución - Te permite solicitar bloques de memoria del sistema operativo cuando 
//los necesites, en lugar de tener que pre-reservar todo al inicio del programa.
malloc:
    // Entrada: x0 = tamaño requerido en bytes
    // Salida: x0 = puntero a la memoria asignada (o 0 si falla)
    
    // Guardar registros que modificaremos
    stp x29, x30, [sp, #-16]!
    stp x19, x20, [sp, #-16]!
    
    // Verificar si es la primera llamada
    adrp x1, heap_start
    add x1, x1, :lo12:heap_start
    ldr x2, [x1]             // heap_start
    cbnz x2, check_available // Si ya está inicializado, saltar
    
    // Inicializar el heap por primera vez
    mov x0, #0               // Usar brk para obtener memoria
    mov x8, #214             // syscall number para brk (214 en ARM64)
    svc #0
    
    // Guardar el inicio del heap
    adrp x1, heap_start
    add x1, x1, :lo12:heap_start
    str x0, [x1]
    
    // Calcular fin del heap
    adrp x2, heap_size
    add x2, x2, :lo12:heap_size
    ldr x2, [x2]
    add x0, x0, x2
    
    // Establecer nuevo break
    mov x8, #214             // syscall brk
    svc #0
    
    // Restaurar x0 (tamaño solicitado)
    ldr x0, [sp, #32]        // Recuperar el parámetro original
    
check_available:
    // Verificar si hay suficiente espacio
    adrp x1, heap_start
    add x1, x1, :lo12:heap_start
    ldr x19, [x1]            // heap_start
    
    adrp x2, heap_used
    add x2, x2, :lo12:heap_used
    ldr x20, [x2]            // heap_used
    
    add x3, x19, x20         // Puntero actual
    
    // Calcular nuevo heap_used
    add x4, x20, x0          // heap_used + tamaño solicitado
    
    // Verificar límites
    adrp x5, heap_size
    add x5, x5, :lo12:heap_size
    ldr x5, [x5]
    cmp x4, x5
    b.gt malloc_fail          // Si excede el tamaño del heap, fallar
    
    // Actualizar heap_used
    str x4, [x2]
    
    // Devolver puntero
    mov x0, x3
    
    // Restaurar registros
    ldp x19, x20, [sp], #16
    ldp x29, x30, [sp], #16
    ret

malloc_fail:
    // No hay suficiente memoria
    mov x0, #0               // Devolver NULL
    
    // Restaurar registros
    ldp x19, x20, [sp], #16
    ldp x29, x30, [sp], #16
    ret
    
    "},
    {"comparar_igual_int", @"
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
    
    "},

    {"comparar_igual_float", @"
//---------------Aca se comparan dos numeros de tipos float
//Primer numero en D0
//Segundo numero en D1
//-----------Inicio de la funcion
comparar_igual_float:
    fcmp D1, D0          
    beq iguales_float        // Salta si son iguales
    bne diferentes_float     // Salta si son diferentes
iguales_float:
    // Código para cuando son iguales
    mov x0, #1
    b fin_float
diferentes_float:
    // Código para cuando NO son iguales
    mov x0, #0
    b fin_float
fin_float:
    ret 
    "},

    {"comparar_igual_bool",@"
//---------------Aca se comparan dos numeros de tipos bool
//Primer numero en D0
//Segundo numero en D1
//-----------Inicio de la funcion
comparar_igual_bool:
    cmp x1, x0          
    beq iguales_bool        // Salta si son iguales
    bne diferentes_bool     // Salta si son diferentes
iguales_bool:
    // Código para cuando son iguales
    mov x0, #1
    b fin_bool
diferentes_bool:
    // Código para cuando NO son iguales
    mov x0, #0
    b fin_bool
fin_bool:
    ret 
    "},

    {"comparar_igual_strings", @"
//---------------Aca se comparan dos strings
//Primer string en x0
//Segundo string en x1
//-----------Inicio de la funcion
comparar_igual_strings:
    ldrb    w2, [x0], #1        // Cargar byte de cadena A y avanzar x0
    ldrb    w3, [x1], #1        // Cargar byte de cadena B y avanzar x1
    cmp     w2, w3              // ¿Son iguales los caracteres?
    bne    strings_not_equal   // Si no, las cadenas son diferentes
    cbz     w2, strings_equal   // Si es fin de cadena (0), son iguales
    b       comparar_igual_strings     // Repetir con siguiente carácter

strings_equal:
    // Aquí entra si las cadenas son iguales
    mov x0, #1
    b       fin_string

strings_not_equal:
    // Aquí entra si son diferentes
    mov x0, #0
    b       fin_string

fin_string:
    ret
    "},

    {"comparar_igual_rune", @"
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
    "},

    {"comparar_desigual_int", @"
//---------------Aca se comparan dos numeros de tipo int
//Primer numero en X0
//Segundo numero en X1
//-----------Inicio de la funcion
comparar_desigual_int:
    cmp x1, x0          // Compara X0 y X1
    beq iguales2        // Salta si son iguales
    bne diferentes2     // Salta si son diferentes
iguales2:
    // Código para cuando son iguales
    mov x0, #0
    b fin2
diferentes2:
    // Código para cuando NO son iguales
    mov x0, #1
    b fin2
fin2:
    ret 
    "},

    {"comparar_desigual_float", @"
//---------------Aca se comparan dos numeros de tipos float
//Primer numero en D0
//Segundo numero en D1
//-----------Inicio de la funcion
comparar_desigual_float:
    fcmp D1, D0          
    beq iguales_float2        // Salta si son iguales
    bne diferentes_float2     // Salta si son diferentes
iguales_float2:
    // Código para cuando son iguales
    mov x0, #0
    b fin_float2
diferentes_float2:
    // Código para cuando NO son iguales
    mov x0, #1
    b fin_float2
fin_float2:
    ret 
    "},

    {"comparar_desigual_bool", @"
//---------------Aca se comparan dos numeros de tipos bool
//Primer numero en D0
//Segundo numero en D1
//-----------Inicio de la funcion
comparar_desigual_bool:
    cmp x1, x0          
    beq iguales_bool2        // Salta si son iguales
    bne diferentes_bool2     // Salta si son diferentes
iguales_bool2:
    // Código para cuando son iguales
    mov x0, #0
    b fin_bool2
diferentes_bool2:
    // Código para cuando NO son iguales
    mov x0, #1
    b fin_bool2
fin_bool2:
    ret 
    "},

    {"comparar_desigual_strings", @"
//---------------Aca se comparan dos strings
//Primer string en x0
//Segundo string en x1
//-----------Inicio de la funcion
comparar_desigual_strings:
    ldrb    w2, [x0], #1        // Cargar byte de cadena A y avanzar x0
    ldrb    w3, [x1], #1        // Cargar byte de cadena B y avanzar x1
    cmp     w2, w3              // ¿Son iguales los caracteres?
    bne    strings_not_equal2   // Si no, las cadenas son diferentes
    cbz     w2, strings_equal2   // Si es fin de cadena (0), son iguales
    b       comparar_desigual_strings     // Repetir con siguiente carácter

strings_equal2:
    // Aquí entra si las cadenas son iguales
    mov x0, #0
    b       fin_string2

strings_not_equal2:
    // Aquí entra si son diferentes
    mov x0, #1
    b       fin_string2

fin_string2:
    ret
    "},

    {"comparar_desigual_rune", @"
//---------------Aca se comparan dos Runes
//Primer rune en x0
//Segundo rune en x1
//-----------Inicio de la funcion
comparar_desigual_rune:
    cmp    x0, x1              // ¿Son iguales los caracteres?
    bne    rune_not_equal2   // Si no, las cadenas son diferentes

rune_equal2:
    // Aquí entra si las cadenas son iguales
    mov x0, #0
    b       fin_rune2

rune_not_equal2:
    // Aquí entra si son diferentes
    mov x0, #1
    b       fin_rune2

fin_rune2:
    ret
    "}


    };


}