using System.Net.WebSockets;
using System;
using analyzer;
using Antlr4.Runtime.Misc;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Schema;

public class CompilerVisitor : LanguageBaseVisitor<Object?> //Esto quiere decir que retorna un ValueWrapper
{   
    //Se genera una instancia de la clase de generador de arm64
    public ArmGenerator c = new ArmGenerator();

    //Aca se agrega las funciones embedidas desde el entorno principal
    public CompilerVisitor()
    {
    
    }

    // VisitProgram
    public override Object VisitProgram(LanguageParser.ProgramContext context)
    {
        foreach (var dcl in context.declaraciones())
        {
            Visit(dcl);
        }
        return null;
    }

    // VisitBloqueSente
    public override Object VisitBloqueSente(LanguageParser.BloqueSenteContext context)
    {
        c.Comment("Bloque de nuevo entorno");
        c.NewScope();
        foreach (var dcl in context.declaraciones())
        {
            Visit(dcl);
        }

        //Luego de salir del entorno se debe de limpiar la pila
        int bytesToRemove = c.endScope();

        if (bytesToRemove > 0){
            c.Comment($"Removeindo {bytesToRemove} bytes del stack");
            c.Mov(Register.X0, bytesToRemove);
            c.Add(Register.SP, Register.SP, Register.X0);
        }

        return null;
    }


    //Esta corresponde a la produccion: 'var' ID tipos ('=' expr)? ';' # PrimeraDecl 
    public override Object VisitPrimeraDecl(LanguageParser.PrimeraDeclContext context)
    {
        string id = context.ID().GetText(); //Obtenemos el id
        string tipo = context.tipos().GetText(); //Obtenemos el tipo
        c.Comment("Variable declaracion: "+id);

        //Se obtiene un objeto con el tipo
        var objTipo = c.GetDefaultValue(tipo);

        // Si hay una asignación ('=' expr)
        if (context.expr() != null){ 
            //Aca se valida que el valor este en la pila
            Visit(context.expr());
            //Aca se asocia el valor al nombre en el stack virtual
            c.TagObject(id);
        }
        else{
            //Aca se declaran los valores por defecto
            switch (objTipo.Type){
                case StackObject.StackObjectType.Int:
                    c.Comment("Ingresando al valor por defecto int");
                    //Aca se pushe a la pila virtual y tambien a la de arm
                    c.PushConstant(objTipo, 0);
                    //Aca se asocia el valor al nombre en el stack virtual
                    c.TagObject(id);
                    break;
                case StackObject.StackObjectType.Float:
                    c.Comment("Ingresando al valor por defecto float");
                    //Aca se pushe a la pila virtual y tambien a la de arm
                    c.PushConstant(objTipo, "0.0");
                    //Aca se asocia el valor al nombre en el stack virtual
                    c.TagObject(id);
                    break;
                case StackObject.StackObjectType.String:
                    c.Comment("Ingresando al valor por defecto string");
                    //Aca se pushe a la pila virtual y tambien a la de arm
                    c.PushConstant(objTipo, "");
                    //Aca se asocia el valor al nombre en el stack virtual
                    c.TagObject(id);
                    break;
                case StackObject.StackObjectType.Bool:
                    c.Comment("Ingresando al valor por defecto bool");
                    //Aca se pushe a la pila virtual y tambien a la de arm
                    c.PushConstant(objTipo, 0);
                    //Aca se asocia el valor al nombre en el stack virtual
                    c.TagObject(id);
                    break;
                case StackObject.StackObjectType.Rune:
                    c.Comment("Ingresando al valor por defecto rune");
                    //Aca se pushe a la pila virtual y tambien a la de arm
                    c.PushConstant(objTipo, '\0');
                    //Aca se asocia el valor al nombre en el stack virtual
                    c.TagObject(id);
                    break;
            }
        }

        return null;
    }

    //Esta es la declaracion explicita: ID ':=' expr ';' # SegundaDecl
    public override Object VisitSegundaDecl(LanguageParser.SegundaDeclContext context)
    {
        string id = context.ID().GetText(); //Obtenemos el id
        c.Comment("Variable declaracion Explixita: "+id);

         //Aca se valida que el valor este en la pila
        Visit(context.expr());
        //Aca se asocia el valor al nombre en el stack virtual
        c.TagObject(id);
        
        return null;
    }

    public override Object VisitNegar([NotNull] LanguageParser.NegarContext context)
    {
        c.Comment("Negacion de un valor");
        //Aca se almacena el valor en el stack
        Visit(context.expr());
        //Se obtiene el valor del stack para compararlo
        var valor = c.PopObject(Register.X0);

        //Aca se manejan los tipos
        switch (valor.Type){
            case StackObject.StackObjectType.Int:
                //Se niega el valor del reguistro x0
                c.NegarInt(Register.X0);
                //Se pushea a la pila de nuevo
                //Ahora se vuelve a cargar al stack
                c.Comment("Pushing resultados");
                //Esto es a nievel de arm
                c.Push(Register.X0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                c.PushObject(c.CloneObject(valor));
                break;
            case StackObject.StackObjectType.Float:
                //Se niega el valor del reguistro x0
                c.NegarFloat("d0");
                //Se pushea a la pila de nuevo
                //Ahora se vuelve a cargar al stack
                c.Comment("Pushing resultados");
                //Esto es a nievel de arm
                c.Push("d0");
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                c.PushObject(c.CloneObject(valor));
                break;
        }

        return null;
    }

    //  VisitExpreStmt
    public override Object  VisitExpreStmt(LanguageParser.ExpreStmtContext context)
    {
        //Aca unicamente se deve de dejar la pila basillas
        Visit(context.expr());
        c.Comment("Popping descartando el valor");
        c.PopObject(Register.X0);
        return null;
    }

    //VisitPrinStmt
    public override Object VisitPrinStmt(LanguageParser.PrinStmtContext context)
    {
        c.Comment("Print statement");
        foreach(var val in context.expr())
        {
            Visit(val);
            var value = c.PopObject(Register.X0);
            //Llamamos a la funcion de imprimir
            switch (value.Type){
                case StackObject.StackObjectType.Int:
                    c.PrintInteger(Register.X0);
                    break;
                case StackObject.StackObjectType.Float:
                    c.PrintFloat("d0");
                    break;
                case StackObject.StackObjectType.String:
                    c.PrintString(Register.X0);
                    break;
                case StackObject.StackObjectType.Bool:
                    c.PrintBool(Register.X0);
                    break;
                case StackObject.StackObjectType.Rune:
                    c.PrintRune(Register.X0);
                    break;
            }
            
        }
        return null;
    }


    //VisitIdentifaider    ID                                        # Identifaider
    public override Object VisitIdentifaider(LanguageParser.IdentifaiderContext context)
    {
        var id = context.ID().GetText();

        //Ahora calcular cuanto me tengo que mover relativo a la variable en el stack
        var (offset, obj) = c.GetObject(id);

        //Aca se obtiene la direccion
        c.Mov(Register.X0, offset);
        c.Add(Register.X0, Register.SP, Register.X0);

        //Aca se consigue hace una copia del valor
        c.Ldr(Register.X0, Register.X0);
        c.Push(Register.X0);

        //Aca se carga a la pila virtual y no necesitamos el valor del id
        var newObject = c.CloneObject(obj);
        newObject.Id = null;
        c.PushObject(newObject);

        return null;
    }

    public override Object VisitParens(LanguageParser.ParensContext context)
    {
        //Solo necesito visitar el valor y guardarlo en la pila
        c.Comment("(Operacion Agrupada)");
        Visit(context.expr());
        return null;
    }

    public override Object VisitMulDiv(LanguageParser.MulDivContext context)
    {
        c.Comment("MUL/DIV operaciones");
        //Obtenemos la operacion
        var operation = context.op.Text;
        //Visitamos el lado izquierdo
        // 1*|/|% 2
        //TOP --> []
        Visit(context.expr(0)); //Visit 1: TOP --> [1]
        Visit(context.expr(1)); //Visit 2: TOP --> [2, 1]

        //Se obtinen los valores de la pila
        var left = c.PopObject(Register.X0);
        var right = c.PopObject(Register.X1);

        //Aca se manejan los tipos
        switch ((right.Type, operation, left.Type))
        {
            //Esta es la parte de la suma:
            case (StackObject.StackObjectType.Int, "*", StackObject.StackObjectType.Int):
                //Se realiza la suma ya que los valores ya se encuentran en los reguistros x0 y x1
                c.Mul(Register.X0, Register.X1, Register.X0);
                //Ahora se vuelve a cargar al stack
                c.Comment("Pushing resultados de la MULTIPLICACION");
                //Esto es a nievel de arm
                c.Push(Register.X0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                c.PushObject(c.CloneObject(left));
                break;
            case (StackObject.StackObjectType.Int, "*", StackObject.StackObjectType.Float):
                // Primero se convierte el valor del tipo int a float
                c.Scvtf(Register.D1, Register.X1);
                //Se realiza la suma entre valores de tipo float
                c.Fmul(Register.D0, Register.D1, Register.D0);
                c.Comment("Pushing resultados de la MULTIPLICACION");
                //Esto es a nievel de arm
                c.Push(Register.D0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                c.PushObject(c.CloneObject(left));
                break;
            case (StackObject.StackObjectType.Float, "*", StackObject.StackObjectType.Float):
                //Se realiza la suma entre valores de tipo float
                c.Fmul(Register.D0, Register.D1, Register.D0);
                c.Comment("Pushing resultados de la MULTIPLICACION");
                //Esto es a nievel de arm
                c.Push(Register.D0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                c.PushObject(c.CloneObject(left));
                break;
            case (StackObject.StackObjectType.Float, "*", StackObject.StackObjectType.Int):
                // Primero se convierte el valor del tipo int a float
                c.Scvtf(Register.D0, Register.X0);
                //Se realiza la suma entre valores de tipo float
                c.Fmul(Register.D0, Register.D1, Register.D0);
                c.Comment("Pushing resultados de la MULTIPLICACION");
                //Esto es a nievel de arm
                c.Push(Register.D0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                c.PushObject(c.CloneObject(right));
                break;
            //----------------------Esta es la parte de la resta-------------------------------
            case (StackObject.StackObjectType.Int, "/", StackObject.StackObjectType.Int):
                // Primero se convierte el valor del tipo int a float
                c.Scvtf(Register.D1, Register.X1);
                // Primero se convierte el valor del tipo int a float
                c.Scvtf(Register.D0, Register.X0);
                //Se realiza la suma entre valores de tipo float
                c.Fdiv(Register.D0, Register.D1, Register.D0);
                //Se realiza la resta ya que los valores ya se encuentran en los reguistros x0 y x1
                //c.Div(Register.X0, Register.X1, Register.X0);
                //Ahora se vuelve a cargar al stack
                c.Comment("Pushing resultados de la DIVICION");
                //Esto es a nievel de arm
                c.Push(Register.D0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                var copia = c.CloneObject(left);
                copia.Type = StackObject.StackObjectType.Float;
                c.PushObject(copia);
                break;
            case (StackObject.StackObjectType.Int, "/", StackObject.StackObjectType.Float):
                // Primero se convierte el valor del tipo int a float
                c.Scvtf(Register.D1, Register.X1);
                //Se realiza la suma entre valores de tipo float
                c.Fdiv(Register.D0, Register.D1, Register.D0);
                c.Comment("Pushing resultados de la DIVICION");
                //Esto es a nievel de arm
                c.Push(Register.D0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                c.PushObject(c.CloneObject(left));
                break;
            case (StackObject.StackObjectType.Float, "/", StackObject.StackObjectType.Float):
                //Se realiza la suma entre valores de tipo float
                c.Fdiv(Register.D0, Register.D1, Register.D0);
                c.Comment("Pushing resultados de la DIVICION");
                //Esto es a nievel de arm
                c.Push(Register.D0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                c.PushObject(c.CloneObject(left));
                break;
            case (StackObject.StackObjectType.Float, "/", StackObject.StackObjectType.Int):
                // Primero se convierte el valor del tipo int a float
                c.Scvtf(Register.D0, Register.X0);
                //Se realiza la suma entre valores de tipo float
                c.Fdiv(Register.D0, Register.D1, Register.D0);
                c.Comment("Pushing resultados de la DIVICION");
                //Esto es a nievel de arm
                c.Push(Register.D0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                c.PushObject(c.CloneObject(right));
                break;
            //--------------------------------Esto es para el modulo
            case (StackObject.StackObjectType.Int, "%", StackObject.StackObjectType.Int):
                //Se realiza: x2 = cociente (x0 / x1)
                c.Sdiv(Register.X2, Register.X1, Register.X0);
                //Ahora se realiza x0 = x0 - (x2 * x1)
                c.Msub(Register.X0, Register.X2, Register.X0, Register.X1);
                //Ahora se vuelve a cargar al stack
                c.Comment("Pushing resultados del MODULO");
                //Esto es a nievel de arm
                c.Push(Register.X0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                c.PushObject(c.CloneObject(left));
                break;
        }

        return null;
    }

    public override Object VisitSumRes(LanguageParser.SumResContext context)
    {
        c.Comment("ADD/SUB operaciones");
        //Obtenemos la operacion
        var operation = context.op.Text;
        //Visitamos el lado izquierdo
        // 1+2
        //TOP --> []
        Visit(context.expr(0)); //Visit 1: TOP --> [1]
        Visit(context.expr(1)); //Visit 2: TOP --> [2, 1]

        //Se obtinen los valores de la pila
        var left = c.PopObject(Register.X0);
        var right = c.PopObject(Register.X1);

        //Aca se manejan los tipos
        switch ((right.Type, operation, left.Type))
        {
            //Esta es la parte de la suma:
            case (StackObject.StackObjectType.Int, "+", StackObject.StackObjectType.Int):
                //Se realiza la suma ya que los valores ya se encuentran en los reguistros x0 y x1
                c.Add(Register.X0, Register.X1, Register.X0);
                //Ahora se vuelve a cargar al stack
                c.Comment("Pushing resultados de la SUMA");
                //Esto es a nievel de arm
                c.Push(Register.X0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                c.PushObject(c.CloneObject(left));
                break;
            case (StackObject.StackObjectType.Int, "+", StackObject.StackObjectType.Float):
                // Primero se convierte el valor del tipo int a float
                c.Scvtf(Register.D1, Register.X1);
                //Se realiza la suma entre valores de tipo float
                c.Fadd(Register.D0, Register.D1, Register.D0);
                c.Comment("Pushing resultados de la SUMA");
                //Esto es a nievel de arm
                c.Push(Register.D0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                c.PushObject(c.CloneObject(left));
                break;
            case (StackObject.StackObjectType.Float, "+", StackObject.StackObjectType.Float):
                //Se realiza la suma entre valores de tipo float
                c.Fadd(Register.D0, Register.D1, Register.D0);
                c.Comment("Pushing resultados de la SUMA");
                //Esto es a nievel de arm
                c.Push(Register.D0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                c.PushObject(c.CloneObject(left));
                break;
            case (StackObject.StackObjectType.Float, "+", StackObject.StackObjectType.Int):
                // Primero se convierte el valor del tipo int a float
                c.Scvtf(Register.D0, Register.X0);
                //Se realiza la suma entre valores de tipo float
                c.Fadd(Register.D0, Register.D1, Register.D0);
                c.Comment("Pushing resultados de la SUMA");
                //Esto es a nievel de arm
                c.Push(Register.D0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                c.PushObject(c.CloneObject(right));
                break;
            case (StackObject.StackObjectType.String, "+", StackObject.StackObjectType.String):
                //Esto llama a la funcion de concat_strings
                c.UnirStrings();
                c.Comment("Pushing resultados de la SUMA");
                //El resultado queda en el regusitro x0, por lo tanto se guarda en la pila
                c.Push(Register.X0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                c.PushObject(c.CloneObject(left));
                break;
            //----------------------Esta es la parte de la resta-------------------------------
            case (StackObject.StackObjectType.Int, "-", StackObject.StackObjectType.Int):
                //Se realiza la resta ya que los valores ya se encuentran en los reguistros x0 y x1
                c.Sub(Register.X0, Register.X1, Register.X0);
                //Ahora se vuelve a cargar al stack
                c.Comment("Pushing resultados de la RESTA");
                //Esto es a nievel de arm
                c.Push(Register.X0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                c.PushObject(c.CloneObject(left));
                break;
            case (StackObject.StackObjectType.Int, "-", StackObject.StackObjectType.Float):
                // Primero se convierte el valor del tipo int a float
                c.Scvtf(Register.D1, Register.X1);
                //Se realiza la suma entre valores de tipo float
                c.Fsub(Register.D0, Register.D1, Register.D0);
                c.Comment("Pushing resultados de la RESTA");
                //Esto es a nievel de arm
                c.Push(Register.D0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                c.PushObject(c.CloneObject(left));
                break;
            case (StackObject.StackObjectType.Float, "-", StackObject.StackObjectType.Float):
                //Se realiza la suma entre valores de tipo float
                c.Fsub(Register.D0, Register.D1, Register.D0);
                c.Comment("Pushing resultados de la RESTA");
                //Esto es a nievel de arm
                c.Push(Register.D0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                c.PushObject(c.CloneObject(left));
                break;
            case (StackObject.StackObjectType.Float, "-", StackObject.StackObjectType.Int):
                // Primero se convierte el valor del tipo int a float
                c.Scvtf(Register.D0, Register.X0);
                //Se realiza la suma entre valores de tipo float
                c.Fsub(Register.D0, Register.D1, Register.D0);
                c.Comment("Pushing resultados de la RESTA");
                //Esto es a nievel de arm
                c.Push(Register.D0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                c.PushObject(c.CloneObject(right));
                break;
        }

        return null;
    }

    //---------------Aca se iran agregando las nuevas visitas--------------------------------------
     // VisitInt
    public override Object VisitInt(LanguageParser.IntContext context)
    {
        var value = context.INT().GetText();
        c.Comment("Constante: "+value);
        
        //Aca se genera un valor por defecto
        var intObject = c.IntObject();
        //Aca se pushe a la pila virtual y tambien a la de arm
        c.PushConstant(intObject, int.Parse(value));

        return null;
    }
    
    // VisitFloat
    public override Object VisitFloat(LanguageParser.FloatContext context)
    { 
        var value = context.FLOAT().GetText();
        c.Comment("Flotante: "+value);

        long ieee754bits = BitConverter.DoubleToInt64Bits(float.Parse(value, CultureInfo.InvariantCulture));
        //System.Console.WriteLine($"IEEE 754 (double) as long: {ieee754bits}");
        //System.Console.WriteLine($"Hex: 0x{ieee754bits:X16}");

        //Esta cadena:  insertar directamente ese número como constante binaria 
        string cadena = $"0x{ieee754bits:X16}";
        //Se obtiene un objeto por defecto de tipo Flotante
        var floatObject = c.FloatObject();
        c.PushConstant(floatObject, cadena);
        
        c.Comment("Final de la lectura del float");
        return null;
    }

    // VisitString
    public override Object VisitString(LanguageParser.StringContext context)
    {

        string rawText = context.STRING().GetText();
        string processedText = SecuanciasEscape.UnescapeString(rawText);//Procesa las secuancias de escape
        
        if (processedText.Equals("\n")){
            c.Comment("String constante: \\\\n");
        }else{
            c.Comment("String constante: "+processedText);
        }
        //Se generar un objeto de tipo string
        var stringObject = c.StringObject();
        //Se asocia el obejto con el strong
        c.PushConstant(stringObject, processedText);

        return null;
    }

    // VisitBoolean
    public override Object VisitBoolean(LanguageParser.BooleanContext context)
    {
        string valor = context.booll().GetText();
        if (valor.Equals("true"))
        {
            c.Comment("Cargando valor bool: "+valor);
            var boolObject = c.BoolObject();
            c.PushConstant(boolObject, 1);
        }
        else if(valor.Equals("false"))
        {
            c.Comment("Cargando valor bool: "+valor);
            var boolObject = c.BoolObject();
            c.PushConstant(boolObject, 0);
        }
        return null;
    }

    // VisitRune
    public override Object VisitRune(LanguageParser.RuneContext context)
    {
        string rawRune = context.RUNE().GetText();
        char processedRune = SecuanciasEscape.UnescapeRune(rawRune);//Procesa las secuancias de escape
        
        c.Comment("Rune constante: "+processedRune);
        //Se generar un objeto de tipo rune
        var runeObject = c.RuneObject();
        //Se asocia el obejto con el strong
        c.PushConstant(runeObject, processedRune);

        return null;
    }


     // VisitRelacional: expr op = ('>' | '<' | '>=' | '<=') expr 
    public override Object VisitRelacional(LanguageParser.RelacionalContext context)
    {
        c.Comment("Operacion de (> | < | >= | <=)");
        //Se obtiene la opracion
        var op = context.op.Text;
        //Se visitan las dos expresiones por ende ya estan en la pila sp
        Visit(context.expr(0)); //Visit 1: TOP --> [1]
        Visit(context.expr(1)); //Visit 2: TOP --> [2, 1]

        //Se obtinen los valores de la pila
        var left = c.PopObject(Register.X0);
        var right = c.PopObject(Register.X1);
        //Aca se manejan los tipos
        switch ((right.Type, op, left.Type)){
             //Esta es la parte de la Igulacion:
            case (StackObject.StackObjectType.Int, ">", StackObject.StackObjectType.Int):
                //Se realiza la comparacion entre los valores de x0 y x1
                c.BoolBranchMayor_Int();
                break;
            case (StackObject.StackObjectType.Float, ">", StackObject.StackObjectType.Float):
                //Se realiza la llamada a la funcion para comparar
                c.BoolBranchMayor_Float();
                break;
            case (StackObject.StackObjectType.Int, ">", StackObject.StackObjectType.Float):
                // Primero se convierte el valor del tipo int a float
                c.Scvtf(Register.D1, Register.X1);
                //Se realiza la llamada a la funcion para comparar
                c.BoolBranchMayor_Float();
                break;
            case (StackObject.StackObjectType.Float, ">", StackObject.StackObjectType.Int):
                // Primero se convierte el valor del tipo int a float
                c.Scvtf(Register.D0, Register.X0);
                //Se realiza la llamada a la funcion para comparar
                c.BoolBranchMayor_Float();
                break;
            case (StackObject.StackObjectType.Rune, ">", StackObject.StackObjectType.Rune):
                //Esto llama a la funcion de concat_strings
                c.BoolBranchMayor_Rune();
                break;
            //----------------------Esta es la parte de la diferencia-------------------------------
            case (StackObject.StackObjectType.Int, ">=", StackObject.StackObjectType.Int):
                //Se realiza la comparacion entre los valores de x0 y x1
                c.BoolBranchMayorIgual_Int();
                break;
            case (StackObject.StackObjectType.Float, ">=", StackObject.StackObjectType.Float):
                //Se realiza la llamada a la funcion para comparar
                c.BoolBranchMayorIgual_Float();
                break;
            case (StackObject.StackObjectType.Int, ">=", StackObject.StackObjectType.Float):
                // Primero se convierte el valor del tipo int a float
                c.Scvtf(Register.D1, Register.X1);
                //Se realiza la llamada a la funcion para comparar
                c.BoolBranchMayorIgual_Float();
                break;
            case (StackObject.StackObjectType.Float, ">=", StackObject.StackObjectType.Int):
                // Primero se convierte el valor del tipo int a float
                c.Scvtf(Register.D0, Register.X0);
                //Se realiza la llamada a la funcion para comparar
                c.BoolBranchMayorIgual_Float();
                break;
            case (StackObject.StackObjectType.Rune, ">=", StackObject.StackObjectType.Rune):
                //Esto llama a la funcion de concat_strings
                c.BoolBranchMayorIgual_Rune();
                break;
            //----------------------Esta es la parte del menor-------------------------------
            case (StackObject.StackObjectType.Int, "<", StackObject.StackObjectType.Int):
                //Se realiza la comparacion entre los valores de x0 y x1
                c.BoolBranchMenor_Int();
                break;
            case (StackObject.StackObjectType.Float, "<", StackObject.StackObjectType.Float):
                //Se realiza la llamada a la funcion para comparar
                c.BoolBranchMenor_Float();
                break;
            case (StackObject.StackObjectType.Int, "<", StackObject.StackObjectType.Float):
                // Primero se convierte el valor del tipo int a float
                c.Scvtf(Register.D1, Register.X1);
                //Se realiza la llamada a la funcion para comparar
                c.BoolBranchMenor_Float();
                break;
            case (StackObject.StackObjectType.Float, "<", StackObject.StackObjectType.Int):
                // Primero se convierte el valor del tipo int a float
                c.Scvtf(Register.D0, Register.X0);
                //Se realiza la llamada a la funcion para comparar
                c.BoolBranchMenor_Float();
                break;
            case (StackObject.StackObjectType.Rune, "<", StackObject.StackObjectType.Rune):
                //Esto llama a la funcion de concat_strings
                c.BoolBranchMenor_Rune();
                break;
            //----------------------Esta es la parte del menor Igual-------------------------------
            case (StackObject.StackObjectType.Int, "<=", StackObject.StackObjectType.Int):
                //Se realiza la comparacion entre los valores de x0 y x1
                c.BoolBranchMenorIgual_Int();
                break;
            case (StackObject.StackObjectType.Float, "<=", StackObject.StackObjectType.Float):
                //Se realiza la llamada a la funcion para comparar
                c.BoolBranchMenorIgual_Float();
                break;
            case (StackObject.StackObjectType.Int, "<=", StackObject.StackObjectType.Float):
                // Primero se convierte el valor del tipo int a float
                c.Scvtf(Register.D1, Register.X1);
                //Se realiza la llamada a la funcion para comparar
                c.BoolBranchMenorIgual_Float();
                break;
            case (StackObject.StackObjectType.Float, "<=", StackObject.StackObjectType.Int):
                // Primero se convierte el valor del tipo int a float
                c.Scvtf(Register.D0, Register.X0);
                //Se realiza la llamada a la funcion para comparar
                c.BoolBranchMenorIgual_Float();
                break;
            case (StackObject.StackObjectType.Rune, "<=", StackObject.StackObjectType.Rune):
                //Esto llama a la funcion de concat_strings
                c.BoolBranchMenorIgual_Rune();
                break;
        }

        //Ahora se vuelve a cargar al stack
        c.Comment("Pushing resultados de los RELACIONALES (> | < | >= | <=)");
        //Esto es a nievel de arm
        c.Push(Register.X0);
        //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
        var objec = c.CloneObject(left);
        objec.Type = StackObject.StackObjectType.Bool;
        c.PushObject(objec);

        return null;
    }
    
    // VisitEqualitys : expr op = ('==' | '!=') expr  
    public override Object VisitEqualitys(LanguageParser.EqualitysContext context)
    {
        c.Comment("Operacion de (== | !=)");
        //Se obtiene la opracion
        var op = context.op.Text;
        //Se visitan las dos expresiones por ende ya estan en la pila sp
        Visit(context.expr(0)); //Visit 1: TOP --> [1]
        Visit(context.expr(1)); //Visit 2: TOP --> [2, 1]

        //Se obtinen los valores de la pila
        var left = c.PopObject(Register.X0);
        var right = c.PopObject(Register.X1);

        //Aca se manejan los tipos
        switch ((right.Type, op, left.Type)){
             //Esta es la parte de la Igulacion:
            case (StackObject.StackObjectType.Int, "==", StackObject.StackObjectType.Int):
                //Se realiza la comparacion entre los valores de x0 y x1
                c.BoolBranchIgualacion();
                break;
            case (StackObject.StackObjectType.Float, "==", StackObject.StackObjectType.Float):
                //Se realiza la llamada a la funcion para comparar
                c.BoolBranchIgualacion_Float();
                break;
            case (StackObject.StackObjectType.Int, "==", StackObject.StackObjectType.Float):
                // Primero se convierte el valor del tipo int a float
                c.Scvtf(Register.D1, Register.X1);
                //Se realiza la llamada a la funcion para comparar
                c.BoolBranchIgualacion_Float();
                break;
            case (StackObject.StackObjectType.Float, "==", StackObject.StackObjectType.Int):
                // Primero se convierte el valor del tipo int a float
                c.Scvtf(Register.D0, Register.X0);
                //Se realiza la llamada a la funcion para comparar
                c.BoolBranchIgualacion_Float();
                break;
            case (StackObject.StackObjectType.Bool, "==", StackObject.StackObjectType.Bool):
                //Se realiza la llamada a la funcion para comparar
                c.BoolBranchIgualacion_Bool();
                break;
            case (StackObject.StackObjectType.String, "==", StackObject.StackObjectType.String):
                //Esto llama a la funcion de concat_strings
                c.BoolBranchIgualacion_String();
                break;
            case (StackObject.StackObjectType.Rune, "==", StackObject.StackObjectType.Rune):
                //Esto llama a la funcion de concat_strings
                c.BoolBranchIgualacion_Rune();
                break;
            //----------------------Esta es la parte de la diferencia-------------------------------
            case (StackObject.StackObjectType.Int, "!=", StackObject.StackObjectType.Int):
                //Se realiza la comparacion entre los valores de x0 y x1
                c.BoolBranchDesigualacion_Int();
                break;
            case (StackObject.StackObjectType.Float, "!=", StackObject.StackObjectType.Float):
                //Se realiza la llamada a la funcion para comparar
                c.BoolBranchDesigualacion_Float();
                break;
            case (StackObject.StackObjectType.Int, "!=", StackObject.StackObjectType.Float):
                // Primero se convierte el valor del tipo int a float
                c.Scvtf(Register.D1, Register.X1);
                //Se realiza la llamada a la funcion para comparar
                c.BoolBranchDesigualacion_Float();
                break;
            case (StackObject.StackObjectType.Float, "!=", StackObject.StackObjectType.Int):
                // Primero se convierte el valor del tipo int a float
                c.Scvtf(Register.D0, Register.X0);
                //Se realiza la llamada a la funcion para comparar
                c.BoolBranchDesigualacion_Float();
                break;
            case (StackObject.StackObjectType.Bool, "!=", StackObject.StackObjectType.Bool):
                //Se realiza la llamada a la funcion para comparar
                c.BoolBranchDesigualacion_Bool();
                break;
            case (StackObject.StackObjectType.String, "!=", StackObject.StackObjectType.String):
                //Esto llama a la funcion de concat_strings
                c.BoolBranchDesigualacion_String();
                break;
            case (StackObject.StackObjectType.Rune, "!=", StackObject.StackObjectType.Rune):
                //Esto llama a la funcion de concat_strings
                c.BoolBranchDesigualacion_Rune();
                break;
        }

        //Ahora se vuelve a cargar al stack
        c.Comment("Pushing resultados de la IGUALACION(==)");
        //Esto es a nievel de arm
        c.Push(Register.X0);
        //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
        var objec = c.CloneObject(left);
        objec.Type = StackObject.StackObjectType.Bool;
        c.PushObject(objec);

        return null;
    }
    
    // VisitAssign     expr '=' expr 
    public override Object VisitAssign(LanguageParser.AssignContext context)
    {
        var assigne = context.expr(0);

        if(assigne is LanguageParser.IdentifaiderContext idContext)
        {
            string varName = idContext.ID().GetText();
            c.Comment("Assignacion a la variable: "+varName);
            //Esto dejo el valor en el stack
            Visit(context.expr(1));

            //Esto obtiene la copia del objeto}
            var valueObject = c.PopObject(Register.X0);
            //Aca obtenemos de cuanto debemos realizar el ofsset para encontrar el valor
            var (offset, varObject) = c.GetObject(varName);

            //Aca caegamo en el x1 el offset
            c.Mov(Register.X1, offset);
            //Ahora retrocedo en el stack pionter
            c.Add(Register.X1, Register.SP, Register.X1);

            //Por ultimo se vuelve a guardar el valor en la pila
            c.Str(Register.X0, Register.X1);

            //Esto es solo para resignar el tipo
            varObject.Type = valueObject.Type;

            //Aca se carga el valor en las dos pilas
            c.Push(Register.X0);
            c.PushObject(c.CloneObject(varObject));
        }
        return null;
    }

    //Validacion de las Operador de asignación
    //VisitAsig_Aumento: ID '+=' expr
    public override Object VisitAsig_Aumento(LanguageParser.Asig_AumentoContext context)
    {   
        c.Comment("AUMENTO operacion (+=)");
        var id = context.ID().GetText();
        //-------Esto es para la expresion-----------------------------------
        //Visitamos el lado derecho
        Visit(context.expr()); //Visit 1: TOP --> [1]
        //Se obtinen los valores de la pila
        var right = c.PopObject(Register.X1);


        //-------Esto es para el ID-----------------------------------
        //Ahora calcular cuanto me tengo que mover relativo a la variable en el stack
        var (offset, obj) = c.GetObject(id);

        //Aca se obtiene la direccion
        c.Mov(Register.X0, offset);
        c.Add(Register.X0, Register.SP, Register.X0);

        if (obj.Type == StackObject.StackObjectType.Float){
            //Aca se consigue hace una copia del valor
            c.Ldr(Register.D0, Register.X0);
        }else{
            //Aca se consigue hace una copia del valor
            c.Ldr(Register.X0, Register.X0);
        }

        //Aca se manejan los tipos
        //Objeto id = x0, d0 
        //expresion rigth = x1, d1
        switch ((obj.Type, right.Type))
        {
            //Esta es la parte de la suma:
            case (StackObject.StackObjectType.Int, StackObject.StackObjectType.Int):
                c.Comment("Se realiza la suma de los dos valores");
                //Se realiza la suma ya que los valores ya se encuentran en los reguistros x0 y x1
                c.Add(Register.X0, Register.X0, Register.X1);
                //Ahora se vuelve a cargar al stack
                c.Comment("Pushing resultados de la SUMA(+=)");
                //Esto es a nievel de arm
                c.Push(Register.X0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //Aca se carga a la pila virtual y no necesitamos el valor del id
                obj.Type = right.Type;
                c.PushObject(c.CloneObject(obj));
                //Esto es a nievel de arm
                c.Push(Register.X0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //Aca se carga a la pila virtual y no necesitamos el valor del id
                obj.Type = right.Type;
                c.PushObject(c.CloneObject(obj));
                break;
            case (StackObject.StackObjectType.Int, StackObject.StackObjectType.Float):
                c.Comment("Se realiza la suma de los dos valores");
                // Primero se convierte el valor del tipo int a float
                c.Scvtf(Register.D0, Register.X0);
                //Se realiza la suma entre valores de tipo float
                c.Fadd(Register.D0, Register.D0, Register.D1);
                c.Comment("Pushing resultados de la SUMA(+=)");
                //Esto es a nievel de arm
                c.Push(Register.D0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                obj.Type = right.Type;
                c.PushObject(c.CloneObject(obj));
                //Esto es a nievel de arm
                c.Push(Register.D0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                obj.Type = right.Type;
                c.PushObject(c.CloneObject(obj));
                break;
            case (StackObject.StackObjectType.Float, StackObject.StackObjectType.Float):
                c.Comment("Se realiza la suma de los dos valores");
                //Se realiza la suma entre valores de tipo float
                c.Fadd(Register.D0, Register.D0, Register.D1);
                c.Comment("Pushing resultados de la SUMA(+=)");
                //Esto es a nievel de arm
                c.Push(Register.D0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                obj.Type = right.Type;
                c.PushObject(c.CloneObject(obj));
                //Esto es a nievel de arm
                c.Push(Register.D0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                obj.Type = right.Type;
                c.PushObject(c.CloneObject(obj));
                break;
            case (StackObject.StackObjectType.Float, StackObject.StackObjectType.Int):
                c.Comment("Se realiza la suma de los dos valores");
                // Primero se convierte el valor del tipo int a float
                c.Scvtf(Register.D1, Register.X1);
                //Se realiza la suma entre valores de tipo float
                c.Fadd(Register.D0, Register.D0, Register.D1);
                c.Comment("Pushing resultados de la SUMA(+=)");
                //Esto es a nievel de arm
                c.Push(Register.D0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                c.PushObject(c.CloneObject(obj));
                //Esto es a nievel de arm
                c.Push(Register.D0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                c.PushObject(c.CloneObject(obj));
                break;
            case (StackObject.StackObjectType.String, StackObject.StackObjectType.String):
                c.Comment("Se realiza la suma de los dos valores");
                c.Comment("Esto ordena las palabras en el orden correcto");
                c.MovReg(Register.X2, Register.X0);
                c.MovReg(Register.X3, Register.X1);
                c.MovReg(Register.X0, Register.X3);
                c.MovReg(Register.X1, Register.X2);
                //Esto llama a la funcion de concat_strings
                c.UnirStrings();
                c.Comment("Pushing resultados de la SUMA");
                //El resultado queda en el regusitro x0, por lo tanto se guarda en la pila
                c.Push(Register.X0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                c.PushObject(c.CloneObject(obj));
                //El resultado queda en el regusitro x0, por lo tanto se guarda en la pila
                c.Push(Register.X0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                c.PushObject(c.CloneObject(obj));
                break;
        }

        return null;
    }

    //VisitAsig_Decre: ID '-=' expr 
    public override Object VisitAsig_Decre(LanguageParser.Asig_DecreContext context)
    {
        c.Comment("DECREMENTO operacion (-=)");
        var id = context.ID().GetText();
        //-------Esto es para la expresion-----------------------------------
        //Visitamos el lado derecho
        Visit(context.expr()); //Visit 1: TOP --> [1]
        //Se obtinen los valores de la pila
        var right = c.PopObject(Register.X1);


        //-------Esto es para el ID-----------------------------------
        //Ahora calcular cuanto me tengo que mover relativo a la variable en el stack
        var (offset, obj) = c.GetObject(id);

        //Aca se obtiene la direccion
        c.Mov(Register.X0, offset);
        c.Add(Register.X0, Register.SP, Register.X0);

        if (obj.Type == StackObject.StackObjectType.Float){
            //Aca se consigue hace una copia del valor
            c.Ldr(Register.D0, Register.X0);
        }else{
            //Aca se consigue hace una copia del valor
            c.Ldr(Register.X0, Register.X0);
        }

        //Aca se manejan los tipos
        //Objeto id = x0, d0 
        //expresion rigth = x1, d1
        switch ((obj.Type, right.Type))
        {
            //Esta es la parte de la suma:
            case (StackObject.StackObjectType.Int, StackObject.StackObjectType.Int):
                c.Comment("Se realiza la Resta de los dos valores");
                //Se realiza la suma ya que los valores ya se encuentran en los reguistros x0 y x1
                c.Sub(Register.X0, Register.X0, Register.X1);
                //Ahora se vuelve a cargar al stack
                c.Comment("Pushing resultados de la RESTA(-=)");
                //Esto es a nievel de arm
                c.Push(Register.X0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //Aca se carga a la pila virtual y no necesitamos el valor del id
                obj.Type = right.Type;
                c.PushObject(c.CloneObject(obj));
                //Esto es a nievel de arm
                c.Push(Register.X0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //Aca se carga a la pila virtual y no necesitamos el valor del id
                obj.Type = right.Type;
                c.PushObject(c.CloneObject(obj));
                break;
            case (StackObject.StackObjectType.Int, StackObject.StackObjectType.Float):
                c.Comment("Se realiza la Resta de los dos valores");
                // Primero se convierte el valor del tipo int a float
                c.Scvtf(Register.D0, Register.X0);
                //Se realiza la suma entre valores de tipo float
                c.Fsub(Register.D0, Register.D0, Register.D1);
                c.Comment("Pushing resultados de la RESTA(-=)");
                //Esto es a nievel de arm
                c.Push(Register.D0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                obj.Type = right.Type;
                c.PushObject(c.CloneObject(obj));
                //Esto es a nievel de arm
                c.Push(Register.D0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                obj.Type = right.Type;
                c.PushObject(c.CloneObject(obj));
                break;
            case (StackObject.StackObjectType.Float, StackObject.StackObjectType.Float):
                c.Comment("Se realiza la Resta de los dos valores");
                //Se realiza la suma entre valores de tipo float
                c.Fsub(Register.D0, Register.D0, Register.D1);
                c.Comment("Pushing resultados de la RESTA(-=)");
                //Esto es a nievel de arm
                c.Push(Register.D0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                obj.Type = right.Type;
                c.PushObject(c.CloneObject(obj));
                //Esto es a nievel de arm
                c.Push(Register.D0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                obj.Type = right.Type;
                c.PushObject(c.CloneObject(obj));
                break;
            case (StackObject.StackObjectType.Float, StackObject.StackObjectType.Int):
                c.Comment("Se realiza la Resta de los dos valores");
                // Primero se convierte el valor del tipo int a float
                c.Scvtf(Register.D1, Register.X1);
                //Se realiza la suma entre valores de tipo float
                c.Fsub(Register.D0, Register.D0, Register.D1);
                c.Comment("Pushing resultados de la RESTA(-=)");
                //Esto es a nievel de arm
                c.Push(Register.D0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                c.PushObject(c.CloneObject(obj));
                //Esto es a nievel de arm
                c.Push(Register.D0);
                //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
                //En este caso se clona el left
                c.PushObject(c.CloneObject(obj));
                break;
        }

        return null;
    }

    //VisitAsigna
    public override Object VisitAsigna(LanguageParser.AsignaContext context){
        //Solo devo de evaluar la asignacion
        Visit(context.asignacion());
        return null;
    }

    //Solo se visita la producion
    public override Object VisitSoloPasar(LanguageParser.SoloPasarContext context)
    {
        return null;
    }


    // VisitNegacion
    public override Object VisitNegacion(LanguageParser.NegacionContext context){
        return null;
    }

    // VisitOr
    public override Object VisitOr(LanguageParser.OrContext context){
        return null;
    }

    // VisitAnd
    public override Object VisitAnd(LanguageParser.AndContext context){
        return null;
    }

    //VisitIfstmt
    public override Object VisitIfstat(LanguageParser.IfstatContext context){
        return null;
    }    


    //VisitWhileStmt    'for' expr stmt                                   # WhileStmt
    public override Object VisitWhileStmt(LanguageParser.WhileStmtContext context){
        return null;
    }

    //VisitInstrucSwitch
    public override Object VisitInstrucSwitch(LanguageParser.InstrucSwitchContext context){
        return null;
    }
    //VisitInstrucCase
    public override Object VisitInstrucCase(LanguageParser.InstrucCaseContext context){
        return null;
    }
    //VisitInstrucDefault
    public override Object VisitInstrucDefault(LanguageParser.InstrucDefaultContext context){
        return null;
    }


    //VisitForStmt
    public override Object VisitForStmt(LanguageParser.ForStmtContext context){
        return null;
    }

    //VisitBreakStmt
    public override Object VisitBreakStmt(LanguageParser.BreakStmtContext context){
        return null;
    }

    //VisitContinueStmt
    public override Object VisitContinueStmt(LanguageParser.ContinueStmtContext context){
        return null;
    }

    //VisitReturnStmt
    public override Object VisitReturnStmt(LanguageParser.ReturnStmtContext context){
        return null;
    }


    //VisitSliceIniciali    ID ':=' '[' ']' tipos '{'expr*'}' ';' 
    //return new IntValue(int.Parse(context.INT().GetText())); 
    public override Object VisitSliceIniciali(LanguageParser.SliceInicialiContext context){
        return null;
    }

    //VisitSliceNoIncial   'var' ID '[' ']' tipos ';'  
    public override Object VisitSliceNoIncial(LanguageParser.SliceNoIncialContext context){
        return null;
    }

    //VisitForSlice     'for' ID ',' ID ':=' 'range' ID stmt 
    public override Object VisitForSlice(LanguageParser.ForSliceContext context){
        return null;
    }

    //VisitAumento_Uno     ID '++'
    public override Object VisitAumento_Uno(LanguageParser.Aumento_UnoContext context)
    {   
        return null;
    }


    //VisitDecreme_Uno     ID '--'
    public override Object VisitDecreme_Uno(LanguageParser.Decreme_UnoContext context)
    {   
        return null;
    }


    //VisitLlamadaFuncio
    //expr llamada+                             # LlamadaFuncio
    // llamada: '('args?')'                     # Llama
    // | '.' ID                                 # Gets

    // args: expr (','expr)*;
    public override Object VisitLlamadaFuncio(LanguageParser.LlamadaFuncioContext context)
    {
        return null;
    }

    //********************************Funciones Embedias******************************
    //VisitFuncAtoi      'strconv.Atoi' '('expr')'                 # FuncAtoi
    public override Object VisitFuncAtoi(LanguageParser.FuncAtoiContext context)
    {
        return null;
    }


    //VisitFuncParFloat     'strconv.ParseFloat' '('expr')'           # FuncParFloat
    public override Object VisitFuncParFloat(LanguageParser.FuncParFloatContext context)
    {
        return null;
    }


    //VisitFuncTypeOf       'reflect.TypeOf' '('expr')'               # FuncTypeOf
    /*
        string id = context.ID().GetText();
        return currentEnvironment.GetVariable(id, context.Start);
        retorna ValueWraper   []int
    */
    public override Object VisitFuncTypeOf(LanguageParser.FuncTypeOfContext context)
    {
        return null;
    }

    //VisitFuncIndex    'slices.Index' '(' ID ',' expr')'        # FuncIndex
    public override Object VisitFuncIndex(LanguageParser.FuncIndexContext context)
    {
        return null;
    }



    //VisitFuncJoin    'strings.Join' '('ID ',' expr')'          # FuncJoin
    public override Object VisitFuncJoin(LanguageParser.FuncJoinContext context)
    {
        return null;
    }

    //VisitFuncLen   'len' '('expre')'                            # FuncLen
    /*
            | ID '[' expr ']'                           # ObtenerPos
            | ID '[' expr ']' '[' expr ']'              # ObtenerMatris
            | ID                                        # Identifaider
    */
    public override Object VisitFuncLen(LanguageParser.FuncLenContext context)
    {
        return null;
    }

    // Falta el visitor
    //numeros = []int{10, 20, 30, 40, 50};
    //ReasignarSlice

    //ID '=' '[' ']' tipos '{'(expr ','?)*'}' ';'               # ReasignarSlice

    public override Object VisitReasignarSlice(LanguageParser.ReasignarSliceContext context)
    {
        return null;
    }


    //VisitFuncAppend     'append' '('ID',' expr')'                 # FuncAppend
    public override Object VisitFuncAppend(LanguageParser.FuncAppendContext context)
    {
        return null;
    }

    //VisitObtenerPos    ID '[' expr ']'                           # ObtenerPos
    public override Object VisitObtenerPos(LanguageParser.ObtenerPosContext context)
    {
        return null;
    }


    //VisitAsignaList   ID '['expr']' '=' expr                    # AsignaList
    public override Object VisitAsignaList(LanguageParser.AsignaListContext context)
    {
        return null;
    }



    /*
        ID ':=' '[' ']' '[' ']' tipos '{' filasMatriz '}' ';'     # Matris
        
        filasMatriz
            : filaMatriz (',' filaMatriz?)* 
        
        filaMatriz
            : '{' expr (',' expr)* '}'
    */
    //VisitMatris
    public override Object VisitMatris(LanguageParser.MatrisContext context)
    {
        return null;
    }

    //VisitObtenerMatris     ID '[' expr ']' '[' expr ']'              # ObtenerMatris
    public override Object VisitObtenerMatris(LanguageParser.ObtenerMatrisContext context)
    {
        return null;
    }

    //VisitAsignaMatris      ID '['expr']' '['expr']' '=' expr         # AsignaMatris
    public override Object VisitAsignaMatris (LanguageParser.AsignaMatrisContext context)
    {
        return null;
    }


    //VisitFuncDcl
    //'func' ID '(' params? ')' tipos? '{' declaraciones* '}'    # FuncDcl1
    public override Object VisitFuncDcl1(LanguageParser.FuncDcl1Context context)
    {   
        return null;
    }


    //VisitInstStruct
    /*
        instStruct
            : 'type' ID 'struct' '{' listaAtributos* '}'   
        ;

        listaAtributos
            ID (tipos|ID) ';'     
        ;
    */

    public override Object VisitInstStruct(LanguageParser.InstStructContext context)
    {
        return null;
    }


    //VisitStructParam 
    /*
        ID ':=' ID '{' datos* '}'                                # StructParam 

        datos: ID ':' expr (',' (ID ':' expr)?)*    
    */
    public override Object VisitStructParam(LanguageParser.StructParamContext context)
    {
        return null;
    }

    //VisitTipoNil
    public override Object VisitTipoNil(LanguageParser.TipoNilContext context)
    {
        return null;
    }



    //VisitFuncStruct
    /*
        'func' '(' ID ID ')' ID '(' params? ')' tipos? '{' declaraciones* '}' # FuncStruct

        params: ID tipos? (',' ID tipos?)*
    */
    public override Object VisitFuncStruct(LanguageParser.FuncStructContext context)
    {
        return null;
    }


}
