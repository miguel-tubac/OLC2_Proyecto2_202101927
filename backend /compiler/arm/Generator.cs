using System.Text;

//Este va ha ser el estack vitual para poder manejar tipos 
public class StackObject
{
    public enum StackObjectType {Int, Float, String, Bool, Rune}
    public StackObjectType Type {get; set;}
    public int Length {get; set;}
    //Este es el numeor del entorno
    public int Depth {get; set;}
    public string? Id {get; set;}
}

public class ArmGenerator
{
    private readonly List<string> instrucciones = new List<string>();
    private readonly StandardLibrary stdLib = new StandardLibrary();
    private List<StackObject> stack = new List<StackObject>();
    private int depth = 0;

    /* ----- stack operaciones ----*/
    public void PushObject(StackObject obj)
    {
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
                //TODO: pendiente de implementar
                List<byte> stringArray = Utils.StringTo1ByteArray((string)value);
                //Mantenemos la referencia al stack
                //Push(Register.HP);
                //Se cargan los valores
                for (int i = 0; i < stringArray.Count; i++){
                    var charCode = stringArray[i];
                    Comment($"Pushing char {charCode} to heap - ({(char) charCode})");
                    //Esto nos permite utilizar el strore bayte solo con los de w
                    Mov("w0", charCode);
                    Strb("w0", Register.HP);
                    //Aca se van metiendo caracter por caracter
                    Mov(Register.X0, 1);
                    Add(Register.HP, Register.HP, Register.X0);
                }
                //Convertimos a float el string
                Comment("Conversion de cadena a Float");
                ConvertToFloat(Register.HP);
                //Cargamos a la pila el valor numerico de doble
                Push("d0");
                Comment("Fin de conversion de cadena a Float");
                break;
            case StackObject.StackObjectType.String:
                List<byte> stringArray2 = Utils.StringTo1ByteArray((string)value);
                //Mantenemos la referencia al stack
                Push(Register.HP);
                //Se cargan los valores
                for (int i = 0; i < stringArray2.Count; i++){
                    var charCode = stringArray2[i];
                    Comment($"Pushing char {charCode} to heap - ({(char) charCode})");
                    //Esto nos permite utilizar el strore bayte solo con los de w
                    Mov("w0", charCode);
                    Strb("w0", Register.HP);
                    //Aca se van metiendo caracter por caracter
                    Mov(Register.X0, 1);
                    Add(Register.HP, Register.HP, Register.X0);
                }
                break;
        }

        //Este es el mundo de mi stack viertual
        PushObject(obj);
    }

    public StackObject PopObject(string rd)
    {
        var obj = stack.Last();
        stack.RemoveAt(stack.Count -1);

        //Remueve del reguistro destino
        if(obj.Type == StackObject.StackObjectType.Float){
            Pop("d0");
        }else{
            Pop(rd);
        }
        //Retorno el objeto por si se desea usar
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


    //Esto me permite clonar un objeto
    public StackObject CloneObject(StackObject obj)
    {
        return new StackObject
        {
            Type = obj.Type,
            Length = obj.Length,
            Depth = obj.Depth,
            Id = obj.Id
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

    //Esta es la suma de un numero inmediato
    public void Addi(string rd, string rs1, int imm)
    {
        instrucciones.Add($"ADDI {rd}, {rs1}, #{imm}");
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

    public void Ldr(string rd, string rs1, int offset = 0)
    {
        instrucciones.Add($"LDR {rd}, [{rs1}, #{offset}]");
    }

    public void Mov(string rd, int imm)
    {
        instrucciones.Add($"MOV {rd}, {imm}");
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
        Mov(Register.X0, 0);
        Mov(Register.X8, 93);//llamada al sistema para salir
        Svc(); //Finaliza
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
        instrucciones.Add("adrp x1, heap");
        instrucciones.Add("add x1, x1, :lo12:heap");
        instrucciones.Add("BL string_to_double");
    }

    public void PrintString(string rs)
    {
        stdLib.Use("print_string");
        instrucciones.Add($"MOV x0, {rs}");
        instrucciones.Add($"BL print_string");
    }

    //Agregar comentarios
    public void Comment(string comment)
    {
        instrucciones.Add($"// {comment}");
    }

    //Para negar valores enteros
    public void NegarInt(string rd)
    {
        instrucciones.Add($"neg x0, {rd}");
    }

    //Para negar valores dobles
    public void NegarFloat(string rd)
    {
        instrucciones.Add($"fneg d0, {rd}");
    }

    //Sobre escribimos la clase para convertir a string
    public override string ToString()
    {
        //Se agregan las directivas
        var sb = new StringBuilder();
        sb.AppendLine(".data");
        sb.AppendLine("heap: .space 4096");//se reserva un espacion para aspectos variables Bytes
        sb.AppendLine("newline: .ascii \"\\n\"");//Esto es para ageregar un salto de linea luego de imprimir texto
        sb.AppendLine("zero:           .double 0.0");
        sb.AppendLine("one:            .double 1.0");
        sb.AppendLine("ten:            .double 10.0");
        sb.AppendLine("neg_one:        .double -1.0");
        sb.AppendLine("//Esta es para imprimir dobles");
        sb.AppendLine("point:      .byte '.'");
        sb.AppendLine("round_const:    .double 0.0000005 ");
        sb.AppendLine("half:           .double 0.5 ");
        sb.AppendLine("//Esta es para imprimir Bool");
        sb.AppendLine("msg_true:   .asciz \"true\\n\"");
        sb.AppendLine("msg_false:  .asciz \"false\\n\"");
        
        sb.AppendLine("\n.text");
        sb.AppendLine(".global _start");
        sb.AppendLine("_start:");
        //sb.AppendLine("     adr x10, heap");
        sb.AppendLine("\tadrp x10, heap");
        sb.AppendLine("\tadd x10, x10, :lo12:heap");

        //Se agrega el final del programa
        EndProgram();
        //Se agregan las instrucciones de la lista
        foreach (var instruction in instrucciones)
        {
            sb.AppendLine("\t"+instruction);
        }

        //Esto es para agregar la conversion de int a string
        sb.AppendLine("\n\n\n // Libreria Estandar");
        sb.AppendLine(stdLib.GetFunctionDefinitions());

        return sb.ToString();
    }
}