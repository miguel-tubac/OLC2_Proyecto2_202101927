using System.Text;

public class ArmGenerator
{
    private readonly List<string> instrucciones = new List<string>();
    private readonly StandardLibrary stdLib = new StandardLibrary();

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
        instrucciones.Add($"MOV X0, {rs}");
        instrucciones.Add($"BL print_integer");
    }

    //Agregar comentarios
    public void Comment(string comment)
    {
        instrucciones.Add($"// {comment}");
    }

    //Sobre escribimos la clase para convertir a string
    public override string ToString()
    {
        //Se agregan las directivas
        var sb = new StringBuilder();
        sb.AppendLine(".text");
        sb.AppendLine(".global _start");
        sb.AppendLine("_start:");

        //Se agrega el final del programa
        EndProgram();
        //Se agregan las instrucciones de la lista
        foreach (var instruction in instrucciones)
        {
            sb.AppendLine(instruction);
        }

        //Esto es para agregar la conversion de int a string
        sb.AppendLine("\n\n\n // Libreria Estandar");
        sb.AppendLine(stdLib.GetFunctionDefinitions());

        return sb.ToString();
    }
}