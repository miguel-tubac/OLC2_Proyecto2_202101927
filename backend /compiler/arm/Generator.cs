using System.Text;

//Este va ha ser el estack vitual para poder manejar tipos 
public class StackObject
{
    public enum StackObjectType {Int, Float, String, Bool, Rune, Slice, Nil}
    public StackObjectType Type {get; set;}
    public int Length {get; set;}
    //Este es el numeor del entorno
    public int Depth {get; set;}
    public string? Id {get; set;}
    public StackObjectType TypeDato {get; set;}
    public int OffsetSlice {get; set;}
    public int Offset {get; set;}
}

public class ArmGenerator
{
    //Esto es para el codigo en general
    public List<string> instrucciones = new List<string>();
    //Esto es para las funciones
    public List<string> funcInstrucions = new List<string>();
    private readonly StandardLibrary stdLib = new StandardLibrary();
    private List<StackObject> stack = new List<StackObject>();
    public int depth = 0;

    //private readonly List<string> slices = new List<string>();

    /*-------Generar nobres para las ramas------------*/
    private int labelCounter = 0;
    //Aca se genera la etiqueta
    public String GetLabel()
    {
        return $"L{labelCounter++}";
    }
    //Aca se agrega la etiqueta
    public void SetLabel(string label){
        instrucciones.Add($"{label}:");
    }

    /* ----- stack operaciones ----*/
    public void PushObject(StackObject obj)
    {   
        Comment($"Pushing object {obj.Type} to stack");
        stack.Add(obj);
    }

    //Esto es para cargar un valor a las dos pilas
    public void PushConstant(StackObject obj, object value)
    {   
        //Esto es el mundo de arm
        switch (obj.Type)
        {
            case StackObject.StackObjectType.Int:
                Mov(Register.X0, (int)value);
                Push(Register.X0);
                break;
            case StackObject.StackObjectType.Bool:
                //Se carga el valor ya sea 1 ó 0
                //1 = true , 0 = false
                Mov(Register.X0, (int)value);
                Push(Register.X0);
                break;
            case StackObject.StackObjectType.Float:
                //Se carga el valor hexadecimal al regusitro x0
                instrucciones.Add($"LDR x0, ={(string)value}");
                //Movemos el valor al reguistro d0
                Fmov(Register.D0, Register.X0);
                //Cargamos a la pila el valor numerico de doble
                Push(Register.D0);
                Comment("Fin de conversion de cadena a Float");
                break;
            case StackObject.StackObjectType.String:
                //Se carga la referencia de data
                //ADR(Register.HP, "heap");
                //Esto obtiene la lista de los caracteres
                List<byte> stringArray2 = Utils.StringTo1ByteArray((string)value);
                //Mantenemos la referencia al stack
                Push(Register.HP);
                //Se cargan los valores
                for (int i = 0; i < stringArray2.Count; i++){
                    var charCode = stringArray2[i];
                    if (charCode == 10){
                        Comment($"Pushing char {charCode} to heap - (\\\\n)");
                    }
                    else if (charCode == 13){
                        Comment($"Pushing char {charCode} to heap - (\\\\r)");
                    }
                    else{
                        Comment($"Pushing char {charCode} to heap - ({(char) charCode})");
                    }
                    //Esto nos permite utilizar el strore bayte solo con los de w
                    Mov("w0", charCode);
                    Strb("w0", Register.HP);
                    //Aca se van metiendo caracter por caracter
                    Mov(Register.X0, 1);
                    Add(Register.HP, Register.HP, Register.X0);
                }
                break;
            case StackObject.StackObjectType.Rune:
                Comment($"Pushing char to heap - ({value} )");
                //Esto nos permite utilizar el strore bayte solo con los de w
                instrucciones.Add($"MOV w0, #'{value}'");
                Push(Register.X0);
                break;
        }

        //Este es el mundo de mi stack viertual
        PushObject(obj);
    }

    public StackObject PopObject(string rd)
    {
        Comment("Popping object from stack: PopObject()");
        var obj = stack.Last();
        stack.RemoveAt(stack.Count -1);

        //Remueve del reguistro destino
        if(obj.Type == StackObject.StackObjectType.Float){
            //Aca se evalua si el registro es de tipo float y se obtiene el correspondiante
            string destino = Utils.ObtenertDestino(rd);
            Pop(destino);
        }else{
            Pop(rd);
        }
        //Retorno el objeto por si se desea usar
        return obj;
    }

    public StackObject PopObject2(string rd)
    {
        Comment("Popping object from stack: PopObject2()");
        var obj = stack.Last();
        stack.RemoveAt(stack.Count -1);
        Pop(rd);
        //Retorno el objeto por si se desea usar
        return obj;
    }


    //Esto es para las funciones
    public void PopObject3()
    {
        Comment("Popping object from stack: PopObject3()");
        try{
            stack.RemoveAt(stack.Count - 1);
        }
        catch(System.Exception){
            System.Console.WriteLine(this.ToString());
            throw new Exception("stack is empty");
        }
    }

    //Decirele que variabel locale estoy recorriendo 
    public StackObject GetFrameLocal(int index)
    {
        var obj = stack.Where(o => o.Type == StackObject.StackObjectType.Nil).ToList()[index];
        return obj;
    }

    //Estos metodos me retonran datos de cierto tipo
    public StackObject IntObject()
    {
        return new StackObject
        {
            Type = StackObject.StackObjectType.Int,
            Length = 8,//estos son 8 bytes
            Depth = depth,
            Id = null
        };
    }

    public StackObject FloatObject()
    {
        return new StackObject
        {
            Type = StackObject.StackObjectType.Float,
            Length = 8,
            Depth = depth,
            Id = null
        };
    }

    public StackObject StringObject()
    {
        return new StackObject
        {
            Type = StackObject.StackObjectType.String,
            Length = 8,
            Depth = depth,
            Id = null
        };
    }

    //Esto es para los valores por defecto de tipo Bool
    public StackObject BoolObject()
    {
        return new StackObject
        {
            Type = StackObject.StackObjectType.Bool,
            Length = 8,
            Depth = depth,
            Id = null
        };
    }

    public StackObject RuneObject()
    {
        return new StackObject
        {
            Type = StackObject.StackObjectType.Rune,
            Length = 8,
            Depth = depth,
            Id = null
        };
    }

    public StackObject NilObject()
    {
        return new StackObject
        {
            Type = StackObject.StackObjectType.Nil,
            Length = 8,
            Depth = depth,
            Id = null
        };
    }

    public StackObject SliceObject()
    {
        return new StackObject
        {
            Type = StackObject.StackObjectType.Slice,
            Length = 8,
            Depth = depth,
            Id = null,
            TypeDato = StackObject.StackObjectType.Nil,
            OffsetSlice = 0
        };
    }


    //Esto me permitira retornar un objeto de tipo a partir de una cadena
    public StackObject GetDefaultValue(string tipo)
    {
        return tipo switch
        {
            "int" => IntObject(),
            "float64" => FloatObject(),
            "bool" => BoolObject(),
            "string" => StringObject(),
            "rune" => RuneObject(), // Usamos '\0' como valor por defecto
            "nil" => NilObject(),
            _ => throw new Exception($"Tipo desconocido: {tipo}")
        };
    }


    //Esto me permite clonar un objeto
    public StackObject CloneObject(StackObject obj)
    {
        return new StackObject
        {
            Type = obj.Type,
            Length = obj.Length,
            Depth = obj.Depth,
            Id = obj.Id,
            Offset = obj.Offset
        }; 
    }

    //Esto es para generar un nuevo entorno
    public void NewScope()
    {
        depth++;
    }

    //Este es para finalizar un entorno
    public int endScope()
    {
        int byteOffset = 0;
        for(int i = stack.Count-1; i>= 0; i--)
        {
            if (stack[i].Depth == depth){
                byteOffset += stack[i].Length;
                stack.RemoveAt(i);
            }else{
                break;
            }
        }
        //Decrementa en una unidad el valor de los entornos
        depth--;
        //Retona el valor del entorno
        return byteOffset;
    }

    //Esto es para guardar una variable
    public void TagObject(string id)
    {
        stack.Last().Id = id;
    }

    //Esto devuelve la posicion de una variabel en la pila
    public (int, StackObject) GetObject(string id)
    {
        int byteOffset = 0;
        for(int i=stack.Count -1; i>= 0; i--)
        {
            if(stack[i].Id == id)
            {
                return (byteOffset, stack[i]);
            }
            //moverme mas adentro de la pila
            byteOffset += stack[i].Length;
        }

        throw new Exception($"Object {id} no encontrada en el stack");
    }

    /* --------------------- */

    //Esta es la instruccion de la suma
    public void Add(string rd, string rs1, string rs2)
    {
        instrucciones.Add($"ADD {rd}, {rs1}, {rs2}");
    }

    //Esto es para la suma de float
    public void Fadd(string rd, string rs1, string rs2)
    {
        instrucciones.Add($"FADD {rd}, {rs1}, {rs2}");
    }

    public void Fsub(string rd, string rs1, string rs2)
    {
        instrucciones.Add($"FSUB {rd}, {rs1}, {rs2}");
    }

    public void Fmul(string rd, string rs1, string rs2)
    {
        instrucciones.Add($"FMUL {rd}, {rs1}, {rs2}");
    }

    public void Fdiv(string rd, string rs1, string rs2)
    {
        instrucciones.Add($"FDIV {rd}, {rs1}, {rs2}");
    }

    public void Sub(string rd, string rs1, string rs2)
    {
        instrucciones.Add($"SUB {rd}, {rs1}, {rs2}");
    }

    public void Mul(string rd, string rs1, string rs2)
    {
        instrucciones.Add($"MUL {rd}, {rs1}, {rs2}");
    }

    public void Div(string rd, string rs1, string rs2)
    {
        instrucciones.Add($"DIV {rd}, {rs1}, {rs2}");
    }

    public void Sdiv(string rd, string rs1, string rs2)
    {
        instrucciones.Add($"SDIV {rd}, {rs1}, {rs2}");
    }

    public void Msub(string rd, string rs1, string rs2, string rs3)
    {
        instrucciones.Add($"MSUB {rd}, {rs1}, {rs2}, {rs3}");
    }

    //Esta es la suma de un numero inmediato
    public void Addi(string rd, string rs1, int imm)
    {
        instrucciones.Add($"ADDI {rd}, {rs1}, #{imm}");
    }

    //Esto es para convertir un numero int a float
    public void Scvtf(string rd, string rs1)
    {
        instrucciones.Add($"SCVTF {rd}, {rs1}");
    }

    //Para cargar byte por byte
    public void Strb(string rs1, string rs2)
    {
        instrucciones.Add($"STRB {rs1}, [{rs2}]");
    }

    //Operaciones de memoria
    public void Str(string rs1, string rs2, int offset = 0)
    {
        instrucciones.Add($"STR {rs1}, [{rs2}, #{offset}]");
    }

    public void StrPostIncreme(string rs1, string rs2, int offset = 0)
    {
        instrucciones.Add($"STR {rs1}, [{rs2}], #{offset}");
    }

    public void Ldr(string rd, string rs1, int offset = 0)
    {
        instrucciones.Add($"LDR {rd}, [{rs1}, #{offset}]");
    }

    public void Mov(string rd, int imm)
    {
        instrucciones.Add($"MOV {rd}, {imm}");
    }

    public void MovReg(string rd, string rd1)
    {
        instrucciones.Add($"MOV {rd}, {rd1}");
    }

    public void Fmov(string rd, string rd1)
    {
        instrucciones.Add($"FMOV {rd}, {rd1}");
    }
    public void Push(string rs)
    {
        instrucciones.Add($"STR {rs}, [sp, #-8]!");
    }

    public void Pop(string rd)
    {
        instrucciones.Add($"LDR {rd}, [sp], #8");
    }

    //Llamada a valores del sistema
    public void Svc()
    {
        instrucciones.Add($"SVC #0");
    }

    public void EndProgram()
    {
        Comment("Finalizando programa");
        Mov(Register.X0, 0);
        Mov(Register.X8, 93);//llamada al sistema para salir
        Svc(); //Finaliza
    }

    public void PrintNewLine()
    {
        stdLib.Use("print_new_line");
        instrucciones.Add($"BL print_new_line");
    }

    public void PrintEspace()
    {
        stdLib.Use("print_space");
        instrucciones.Add($"BL print_space");
    }


    public void PrintInteger(string rs)
    {
        stdLib.Use("print_integer");
        instrucciones.Add($"MOV x0, {rs}");
        instrucciones.Add($"BL print_integer");
    }

    //Esto es para imprimir tipos Bool
    public void PrintBool(string rs)
    {
        //Cargamos el valor numerico
        stdLib.Use("print_bool");
        instrucciones.Add($"MOV x0, {rs}");
        instrucciones.Add($"BL print_bool");
    }

    public void PrintFloat(string rs)
    {
        stdLib.Use("print_double");
        instrucciones.Add($"FMOV d0, {rs}");
        instrucciones.Add($"BL print_double");
    }

    public void ConvertToFloat(string rs)
    {
        stdLib.Use("string_to_double");
        instrucciones.Add("adrp x1, heap2");
        instrucciones.Add("add x1, x1, :lo12:heap2");
        instrucciones.Add("BL string_to_double");
    }

    public void PrintString(string rs)
    {
        stdLib.Use("print_string");
        instrucciones.Add($"MOV x0, {rs}");
        instrucciones.Add($"BL print_string");
    }

    //Imprimir Runes(chars)
    public void PrintRune(string rs)
    {
        stdLib.Use("print_char");
        instrucciones.Add($"MOV x0, {rs}");
        instrucciones.Add($"BL print_char");
    }

    //Agregar comentarios
    public void Comment(string comment)
    {
        instrucciones.Add($"// {comment}");
    }

    //Para negar valores enteros
    public void NegarInt(string rd)
    {
        instrucciones.Add($"NEG x0, {rd}");
    }

    //Para negar valores dobles
    public void NegarFloat(string rd)
    {
        instrucciones.Add($"FNEG d0, {rd}");
    }

    //Para cargar una etiqueta a un reguistro
    public void ADR(string rd, string name)
    {
        Comment("\t//Esto es para leer los strings");
        instrucciones.Add($"ADR {rd}, {name}");
    }

    //Esta funcion me permite concatenar strings
    public void UnirStrings()
    {
        stdLib.Use("concat_strings");
        instrucciones.Add($"BL concat_strings");
    }

    //Esto es para comparar las cadenas
    public void CmpImm(string rd, int imm)
    {
        instrucciones.Add($"CMP {rd}, #{imm}");
    }

    public void CmpReg(string rd, string rd1)
    {
        instrucciones.Add($"CMP {rd}, {rd1}");
    }

    public void Fcmp(string rd, string rd1)
    {
        instrucciones.Add($"FCMP {rd}, {rd1}");
    }

    //Para saltar a la etiqueta si es igual
    public void Beq(string label)
    {
        instrucciones.Add($"BEQ {label}");
    }

    //-------------------------------Esta parte se utiliza para las operacion de (== | !=)
    public void BoolBranchIgualacion()
    {
        stdLib.Use("comparar_igual_int");
        instrucciones.Add($"BL comparar_igual_int");
    }

    public void BoolBranchIgualacion_Float()
    {
        stdLib.Use("comparar_igual_float");
        instrucciones.Add($"BL comparar_igual_float");
    }

    public void BoolBranchIgualacion_Bool()
    {
        stdLib.Use("comparar_igual_bool");
        instrucciones.Add($"BL comparar_igual_bool");
    }

    public void BoolBranchIgualacion_String()
    {
        stdLib.Use("comparar_igual_strings");
        instrucciones.Add($"BL comparar_igual_strings");
    }

    public void BoolBranchIgualacion_Rune()
    {
        stdLib.Use("comparar_igual_rune");
        instrucciones.Add($"BL comparar_igual_rune");
    }

    public void BoolBranchDesigualacion_Int()
    {
        stdLib.Use("comparar_desigual_int");
        instrucciones.Add($"BL comparar_desigual_int");
    }

    public void BoolBranchDesigualacion_Float()
    {
        stdLib.Use("comparar_desigual_float");
        instrucciones.Add($"BL comparar_desigual_float");
    }

    public void BoolBranchDesigualacion_Bool()
    {
        stdLib.Use("comparar_desigual_bool");
        instrucciones.Add($"BL comparar_desigual_bool");
    }

    public void BoolBranchDesigualacion_String()
    {
        stdLib.Use("comparar_desigual_strings");
        instrucciones.Add($"BL comparar_desigual_strings");
    }

    public void BoolBranchDesigualacion_Rune()
    {
        stdLib.Use("comparar_desigual_rune");
        instrucciones.Add($"BL comparar_desigual_rune");
    }

    //---------------------------------fin de las opreacionde---------------------------------------

    //Aca van las operaciones de los RELACIONALES ('>' | '<' | '>=' | '<=')
    public void BoolBranchMayor_Int()
    {
        stdLib.Use("comparar_mayor_int");
        instrucciones.Add($"BL comparar_mayor_int");
    }

    public void BoolBranchMayor_Float()
    {
        stdLib.Use("comparar_mayor_float");
        instrucciones.Add($"BL comparar_mayor_float");
    }

    public void BoolBranchMayor_Rune()
    {
        stdLib.Use("comparar_mayor_rune");
        instrucciones.Add($"BL comparar_mayor_rune");
    }

    public void BoolBranchMayorIgual_Int()
    {
        stdLib.Use("comparar_mayorIgual_int");
        instrucciones.Add($"BL comparar_mayorIgual_int");
    }

    public void BoolBranchMayorIgual_Float()
    {
        stdLib.Use("comparar_mayorIgual_float");
        instrucciones.Add($"BL comparar_mayorIgual_float");
    }

    public void BoolBranchMayorIgual_Rune()
    {
        stdLib.Use("comparar_mayorIgual_rune");
        instrucciones.Add($"BL comparar_mayorIgual_rune");
    }

    public void BoolBranchMenor_Int()
    {
        stdLib.Use("comparar_menor_int");
        instrucciones.Add($"BL comparar_menor_int");
    }

    public void BoolBranchMenor_Float()
    {
        stdLib.Use("comparar_menor_float");
        instrucciones.Add($"BL comparar_menor_float");
    }

    public void BoolBranchMenor_Rune()
    {
        stdLib.Use("comparar_menor_rune");
        instrucciones.Add($"BL comparar_menor_rune");
    }

    public void BoolBranchMenorIgual_Int()
    {
        stdLib.Use("comparar_menorIgual_int");
        instrucciones.Add($"BL comparar_menorIgual_int");
    }

    public void BoolBranchMenorIgual_Float()
    {
        stdLib.Use("comparar_menorIgual_float");
        instrucciones.Add($"BL comparar_menorIgual_float");
    }

    public void BoolBranchMenorIgual_Rune()
    {
        stdLib.Use("comparar_menorIgual_rune");
        instrucciones.Add($"BL comparar_menorIgual_rune");
    }
    //---------------------------------Fin de las operaciones

    public void And(string rd, string rd1, string rd2)
    {
        instrucciones.Add($"AND {rd}, {rd1}, {rd2}");
    }

    public void Or(string rd, string rd1, string rd2)
    {
        instrucciones.Add($"ORR {rd}, {rd1}, {rd2}");
    }

    public void Not(string rd, string rd1, string rd2)
    {
        Mov(Register.X1, 1);
        instrucciones.Add($"EOR {rd}, {rd1}, {rd2}");
    }

    //------------------Saltos a etiquetas
    public void B(string label)
    {
        instrucciones.Add($"B {label}");
    }

    public void Br(string rd)
    {
        instrucciones.Add($"BR {rd}");
    }
    
    public void Bl(string rd)
    {
        instrucciones.Add($"BL {rd}");
    }

    //Sireve para comparar con el valor cero
    public void Cbz(string rs, string label){
        instrucciones.Add($"CBZ {rs}, {label}");
    }

    //Sobre escribimos la clase para convertir a string
    public override string ToString()
    {
        //Se agregan las directivas
        var sb = new StringBuilder();
        sb.AppendLine(".data");
        sb.AppendLine("//Esto es el espacio para los strings");
        sb.AppendLine("heap: .space 4096");//se reserva un espacion para aspectos variables Bytes
        sb.AppendLine("//Esto lo voy a usar para Float");
        sb.AppendLine("zero:           .double 0.0");
        sb.AppendLine("ten:            .double 10.0");
        sb.AppendLine("neg_one:        .double -1.0");
        sb.AppendLine("//Esta es para imprimir dobles");
        sb.AppendLine("point:      .byte '.'");
        sb.AppendLine("round_const:    .double 0.0000005 ");
        sb.AppendLine("half:           .double 0.5 ");
        sb.AppendLine("//Esta es para imprimir Bool");
        sb.AppendLine("msg_true:   .asciz \"true\"");
        sb.AppendLine("msg_false:  .asciz \"false\"");
        sb.AppendLine("//Esta es para imprimir Int");
        sb.AppendLine("minus_sign:  .ascii \"-\"");
        sb.AppendLine("//Esta es para concatenar Strings");
        sb.AppendLine("heap_start: .quad 0 ");
        sb.AppendLine("heap_size:  .quad 4096 ");
        sb.AppendLine("heap_used:  .quad 0");
        sb.AppendLine("//Esto es para imprimir en la consola");
        sb.AppendLine("space_char: .ascii \" \" ");
        sb.AppendLine("newline: .ascii \"\\n\"");
        
        sb.AppendLine("\n.text");
        sb.AppendLine(".global _start");
        sb.AppendLine("_start:");
        sb.AppendLine("\t//Esto es para leer Strings");
        sb.AppendLine("\tADR x10, heap");

        //Se agrega el final del programa
        EndProgram();
        //Se agregan las instrucciones de la lista
        foreach (var instruction in instrucciones)
        {
            sb.AppendLine("\t"+instruction);
        }

        //Esto agrega las funciones
        sb.AppendLine("\n\n\n// Funciones Foraneas");
        funcInstrucions.ForEach(i => sb.AppendLine(i));

        //Esto es para agregar la conversion de int a string
        sb.AppendLine("\n\n\n // Libreria Estandar");
        sb.AppendLine(stdLib.GetFunctionDefinitions());

        return sb.ToString();
    }
}