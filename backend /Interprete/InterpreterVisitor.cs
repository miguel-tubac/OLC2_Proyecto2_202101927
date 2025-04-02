using System.Net.WebSockets;
using System;
using analyzer;
using Antlr4.Runtime.Misc;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Schema;

public class InterpreterVisitor : LanguageBaseVisitor<ValueWrapper> //Esto quiere decir que retorna un ValueWrapper
{   
    public List<SimboloEntry> simbolos = new List<SimboloEntry>();
    public ValueWrapper defaultValue = new VoidValue();
    public string output = "";

    //Se genera un nuevo entorno es el general
    public Environment currentEnvironment;

    //Aca se agrega las funciones embedidas desde el entorno principal
    public InterpreterVisitor()
    {
        currentEnvironment = new Environment(null); 
        Embedidas.Generate(currentEnvironment);
    }

    // VisitProgram
    public override ValueWrapper VisitProgram(LanguageParser.ProgramContext context)
    {
        foreach (var dcl in context.declaraciones())
        {
            Visit(dcl);
        }
        return defaultValue;
    }

    // VisitBloqueSente
    public override ValueWrapper VisitBloqueSente(LanguageParser.BloqueSenteContext context)
    {
        Environment anteriorEntorno = currentEnvironment;
        currentEnvironment = new Environment(anteriorEntorno);

        foreach (var stmt in context.declaraciones()){
            Visit(stmt);
        }

        currentEnvironment = anteriorEntorno;
        return defaultValue;
    }


    //Esta corresponde a la produccion: 'var' ID tipos ('=' expr)? ';' # PrimeraDecl 
    public override ValueWrapper VisitPrimeraDecl(LanguageParser.PrimeraDeclContext context)
    {
        string id = context.ID().GetText(); //Obtenemos el id
        string tipo = context.tipos().GetText(); //Obtenemos el tipo
        ValueWrapper value = TypeChecker.GetDefaultValue(tipo); //Declaramos un valor por defecto al value
        // Si hay una asignación ('=' expr)
        if (context.expr() != null){ 
            value = Visit(context.expr()); // Evaluamos la expresión

            //Console.WriteLine($"Tipo de '{id}': {value.GetType().Name}");
            if (tipo == "float64" && value is IntValue intValue){
                value = new FloatValue(intValue.Value);//Convertimos el int a float64
            }
            
            if (!TypeChecker.CheckTypeCompatibility(tipo, value)){
                throw new SemanticError($"Error de tipo: Se esperaba '{tipo}' pero se obtuvo '{value.GetType().Name}'", context.Start);
            }
            //Console.WriteLine($"Tipo de '{id}': {value.GetType().Name}");
        }
        // Llamada a la tabla de símbolos (o entorno actual)
        currentEnvironment.DeclararVariable(id, value, context.Start);
        //Esto es para mostrar la tabla de simbolos en el frontend
        //public SimboloEntry(string id, string tipo_simbolo, string tipo_dato, string ambito, int linea, int columna)
        simbolos.Add(new SimboloEntry(id,"Variable",value.ToString(),"Global",context.Start.Line, context.Start.Column));
        return defaultValue;
    }

    //Esta es la declaracion explicita: ID ':=' expr ';' # SegundaDecl
    public override ValueWrapper VisitSegundaDecl(LanguageParser.SegundaDeclContext context)
    {
        //Console.WriteLine("Asignación rápida con valor: ");
        
        string id2 = context.ID().GetText();
        ValueWrapper value2 = Visit(context.expr());
        
        // Llamada a la tabla de símbolos (o entorno actual)
        currentEnvironment.DeclararVariable(id2, value2, context.Start);
        //Esto es para mostrar la tabla de simbolos en el frontend
        //public SimboloEntry(string id, string tipo_simbolo, string tipo_dato, string ambito, int linea, int columna)
        simbolos.Add(new SimboloEntry(id2,"Variable",value2.ToString(),"Global",context.Start.Line, context.Start.Column));

        return defaultValue;
    }

    public override ValueWrapper VisitNegar([NotNull] LanguageParser.NegarContext context)
    {
        ValueWrapper value = Visit(context.expr());
        return value switch
        {
            IntValue i => new IntValue(-i.Value),
            FloatValue f => new FloatValue(-f.Value),
            _ => throw new SemanticError("Negacion Invalida", context.Start)
        };
    }

    //  VisitExpreStmt
    public override ValueWrapper  VisitExpreStmt(LanguageParser.ExpreStmtContext context)
    {
        return Visit(context.expr());
    }

    //VisitPrinStmt
    public override ValueWrapper VisitPrinStmt(LanguageParser.PrinStmtContext context)
    {
        ValueWrapper value = defaultValue;
        foreach(var val in context.expr()){
            value = Visit(val);
            output += value switch
            {
                IntValue i => i.Value.ToString()+" ",
                FloatValue f => f.Value.ToString(CultureInfo.InvariantCulture)+" ",
                StringValue s => s.Value+" ",
                BoolValue b => b.Value+" ",
                CharValue c => c.Value.ToString()+" ",
                VoidValue v => "nil"+" ",
                FunctionValue fn => "<fn>  "+ fn.name + "<fn>"+" ",
                Slices sc => "[" + string.Join(", ", sc.datos.Select(FormatValue)) + "]"+" ",
                StructValue str => str.ToString()+ " ",
                InstanceValue inst => inst.Instance.ToString() + " ",
                MatrixValue mat => FormatMatrix(mat.Values) + " ",
                _ => throw new SemanticError("Parametro del Print Invalido", context.Start)
            };
            //output += "\n";
        }
        output += "\n";
        return defaultValue;//Esto no retorna nada
    }

    private string FormatValue(ValueWrapper value)
    {
        return value switch
        {
            IntValue i => i.Value.ToString(),
            FloatValue f => f.Value.ToString(CultureInfo.InvariantCulture),
            StringValue s => $"\"{s.Value}\"", // Agrega comillas a los strings
            BoolValue b => b.Value.ToString().ToLower(), // true/false en minúsculas
            CharValue c => $"'{c.Value}'", // Muestra los caracteres con comillas simples
            _ => value.ToString() // Si hay otro tipo, usa su ToString() por defecto
        };
    }

    private string FormatMatrix(List<List<ValueWrapper>> matrix)
    {
        return "[\n" + string.Join("\n", matrix.Select(row => "  [" + string.Join(", ", row.Select(FormatValue)) + "]")) + "\n]";
    }


    //VisitIdentifaider    ID                                        # Identifaider
    public override ValueWrapper VisitIdentifaider(LanguageParser.IdentifaiderContext context)
    {
        string id = context.ID().GetText();
        return currentEnvironment.GetVariable(id, context.Start);
    }

    public override ValueWrapper VisitParens(LanguageParser.ParensContext context)
    {
        return Visit(context.expr());
    }

    public override ValueWrapper VisitMulDiv(LanguageParser.MulDivContext context)
    {
        ValueWrapper left = Visit(context.expr(0));
        ValueWrapper right = Visit(context.expr(1));
        var op = context.op.Text;

        if (right is IntValue intRight && intRight.Value == 0 && (op == "/" || op == "%")){
            throw new SemanticError("Operacion invalida: no es posible dividir entre cero.", context.Start);
        }
        if (right is FloatValue floatRight && floatRight.Value == 0.0f && (op == "/" || op == "%")){
            throw new SemanticError("Operacion invalida: no es posible dividir entre cero.", context.Start);
        }

        //Aca se deben de validar los tipos permitidos entre operaciones de tipos
        return (left, right, op) switch
        {
            (IntValue l, IntValue r, "*") => new IntValue(l.Value * r.Value),
            (IntValue l, FloatValue r, "*") => new FloatValue(l.Value * r.Value),
            (FloatValue l, FloatValue r, "*") => new FloatValue(l.Value * r.Value),
            (FloatValue l, IntValue r, "*") => new FloatValue(l.Value * r.Value),//Arriba va la primera tabla
            (IntValue l, IntValue r, "/") => new FloatValue((float)l.Value / r.Value), //Esta validacion esta pendiente ya que devuelve entero 
            (IntValue l, FloatValue r, "/") => new FloatValue(l.Value / r.Value),
            (FloatValue l, FloatValue r, "/") => new FloatValue(l.Value / r.Value),
            (FloatValue l, IntValue r, "/") => new FloatValue(l.Value / r.Value),
            (IntValue l, IntValue r, "%") => new IntValue(l.Value % r.Value),//Este es el calculo del modulo
            _=> throw new SemanticError("Operacion invalida", context.Start)
        };
    }

    public override ValueWrapper VisitSumRes(LanguageParser.SumResContext context)
    {
        ValueWrapper left = Visit(context.expr(0));
        ValueWrapper right = Visit(context.expr(1));
        var op = context.op.Text;

        //Aca se deben de validar los tipos permitidos entre operaciones de tipos
        return (left, right, op) switch
        {
            (IntValue l, IntValue r, "+") => new IntValue(l.Value + r.Value),
            (IntValue l, FloatValue r, "+") => new FloatValue(l.Value + r.Value),
            (FloatValue l, FloatValue r, "+") => new FloatValue(l.Value + r.Value),
            (FloatValue l, IntValue r, "+") => new FloatValue(l.Value + r.Value),
            (StringValue l, StringValue r, "+") => new StringValue(l.Value + r.Value),// Arriba Aca va la primera tabla de sumas
            (IntValue l, IntValue r, "-") => new IntValue(l.Value - r.Value),
            (IntValue l, FloatValue r, "-") => new FloatValue(l.Value - r.Value),
            (FloatValue l, FloatValue r, "-") => new FloatValue(l.Value - r.Value),
            (FloatValue l, IntValue r, "-") => new FloatValue(l.Value - r.Value),
            _=> throw new SemanticError("Operacion invalida", context.Start)
        };
    }

    //---------------Aca se iran agregando las nuevas visitas--------------------------------------
     // VisitInt
    public override ValueWrapper VisitInt(LanguageParser.IntContext context)
    {
        return new IntValue(int.Parse(context.INT().GetText()));
    }
    
    // VisitFloat
    public override ValueWrapper VisitFloat(LanguageParser.FloatContext context)
    {   
        return new FloatValue(float.Parse(context.FLOAT().GetText(), CultureInfo.InvariantCulture));
    }

    // VisitString
    public override ValueWrapper VisitString(LanguageParser.StringContext context)
    {
        string rawText = context.STRING().GetText();
        string processedText = SecuanciasEscape.UnescapeString(rawText);//Procesa las secuancias de escape
        return new StringValue(processedText);
    }

    // VisitBoolean
    public override ValueWrapper VisitBoolean(LanguageParser.BooleanContext context)
    {
        return new BoolValue(bool.Parse(context.booll().GetText()));
    }

    // VisitRune
    public override ValueWrapper VisitRune(LanguageParser.RuneContext context)
    {
        string rawRune = context.RUNE().GetText();
        char processedRune = SecuanciasEscape.UnescapeRune(rawRune);//Procesa las secuancias de escape
        return new CharValue(processedRune);
    }


     // VisitRelacional
    public override ValueWrapper VisitRelacional(LanguageParser.RelacionalContext context)
    {
        ValueWrapper left = Visit(context.expr(0));
        ValueWrapper right = Visit(context.expr(1));
        var op = context.op.Text;

        //Aca se deben de validar los tipos permitidos entre operaciones de tipos
        return (left, right, op) switch
        {
            (IntValue l, IntValue r, "<") => new BoolValue(l.Value < r.Value),
            (FloatValue l, FloatValue r, "<") => new BoolValue(l.Value < r.Value),
            (IntValue  l, FloatValue r, "<") => new BoolValue(l.Value < r.Value),
            (FloatValue l, IntValue  r, "<") => new BoolValue(l.Value < r.Value),
            (CharValue l, CharValue r, "<") => new BoolValue(l.Value < r.Value), //La parte del <
            (IntValue l, IntValue r, "<=") => new BoolValue(l.Value <= r.Value),
            (FloatValue l, FloatValue r, "<=") => new BoolValue(l.Value <= r.Value),
            (IntValue  l, FloatValue r, "<=") => new BoolValue(l.Value <= r.Value),
            (FloatValue l, IntValue  r, "<=") => new BoolValue(l.Value <= r.Value),
            (CharValue l, CharValue r, "<=") => new BoolValue(l.Value <= r.Value), //ACa esta la parte del <=
            (IntValue l, IntValue r, ">") => new BoolValue(l.Value > r.Value),
            (FloatValue l, FloatValue r, ">") => new BoolValue(l.Value > r.Value),
            (IntValue  l, FloatValue r, ">") => new BoolValue(l.Value > r.Value),
            (FloatValue l, IntValue  r, ">") => new BoolValue(l.Value > r.Value),
            (CharValue l, CharValue r, ">") => new BoolValue(l.Value > r.Value),//Aca esta la parte del >
            (IntValue l, IntValue r, ">=") => new BoolValue(l.Value >= r.Value),
            (FloatValue l, FloatValue r, ">=") => new BoolValue(l.Value >= r.Value),
            (IntValue  l, FloatValue r, ">=") => new BoolValue(l.Value >= r.Value),
            (FloatValue l, IntValue  r, ">=") => new BoolValue(l.Value >= r.Value),
            (CharValue l, CharValue r, ">=") => new BoolValue(l.Value >= r.Value), //Aca esta la validacion del >=
            _=> throw new SemanticError("Operacion invalida", context.Start)
        };
    }
    
    // VisitEqualitys
    public override ValueWrapper VisitEqualitys(LanguageParser.EqualitysContext context)
    {
        ValueWrapper left = Visit(context.expr(0));
        ValueWrapper right = Visit(context.expr(1));
        var op = context.op.Text;

        //Aca se deben de validar los tipos permitidos entre operaciones de tipos
        return (left, right, op) switch
        {
            (IntValue l, IntValue r, "==") => new BoolValue(l.Value == r.Value),
            (FloatValue l, FloatValue r, "==") => new BoolValue(l.Value == r.Value),
            (IntValue  l, FloatValue r, "==") => new BoolValue(l.Value == r.Value),
            (FloatValue l, IntValue  r, "==") => new BoolValue(l.Value == r.Value),
            (BoolValue l, BoolValue r, "==") => new BoolValue(l.Value == r.Value),
            (StringValue l, StringValue r, "==") => new BoolValue(string.Equals(l.Value, r.Value, StringComparison.Ordinal)),
            (CharValue l, CharValue r, "==") => new BoolValue(l.Value == r.Value),
            (VoidValue l, VoidValue r, "==") => new BoolValue(l == r),
            (IntValue l, IntValue r, "!=") => new BoolValue(l.Value != r.Value),
            (FloatValue l, FloatValue r, "!=") => new BoolValue(l.Value != r.Value),
            (IntValue  l, FloatValue r, "!=") => new BoolValue(l.Value != r.Value),
            (FloatValue l, IntValue  r, "!=") => new BoolValue(l.Value != r.Value),
            (BoolValue l, BoolValue r, "!=") => new BoolValue(l.Value != r.Value),
            (StringValue l, StringValue r, "!=") => new BoolValue(!string.Equals(l.Value, r.Value, StringComparison.Ordinal)),
            (CharValue l, CharValue r, "!=") => new BoolValue(l.Value != r.Value),
            _=> throw new SemanticError("Operacion invalida", context.Start)
        };
    }
    
    // VisitAssign     expr '=' expr 
    public override ValueWrapper VisitAssign(LanguageParser.AssignContext context)
    {
        var asignee = context.expr(0);
        ValueWrapper value = Visit(context.expr(1));
        //System.Console.WriteLine(value);
        ValueWrapper retorno =  defaultValue;

        //Esto valida si es una asignacion normal
        if(asignee is LanguageParser.IdentifaiderContext idContext){
            //System.Console.WriteLine("Entro aqui miguelas *************");
            string id = idContext.ID().GetText();
            //ValueWrapper value = Visit(context.expr());
            if(value is Slices dato){
                retorno = currentEnvironment.GetVariable(id, context.Start);
                
                if(retorno is Slices dato2){
                    //Se comparan los tipos para valir si son los mismos
                    switch (dato.tipo, dato2.tipo)
                    {
                        case (IntValue l, IntValue r):
                            //Vaciamos la lista por si tiene algo ya que es una igualacion
                            dato2.datos.Clear();
                            //Recorremos el array a copiar y agregamos el elemento al array principal
                            foreach (ValueWrapper elemento in dato.datos){
                                dato2.datos.Add(elemento);
                            }
                            //Agregamos a la tabla de valores
                            retorno = currentEnvironment.AsignarVariable(id, retorno, context.Start);
                            //Esto es para mostrar la tabla de simbolos en el frontend
                            //public SimboloEntry(string id, string tipo_simbolo, string tipo_dato, string ambito, int linea, int columna)
                            simbolos.Add(new SimboloEntry(id,"Variable",retorno.ToString(),"Global",context.Start.Line, context.Start.Column));
                            break;
                        case (FloatValue l, FloatValue r):
                            //Vaciamos la lista por si tiene algo ya que es una igualacion
                            dato2.datos.Clear();
                            //Recorremos el array a copiar y agregamos el elemento al array principal
                            foreach (ValueWrapper elemento in dato.datos){
                                dato2.datos.Add(elemento);
                            }
                            //Agregamos a la tabla de valores
                            retorno = currentEnvironment.AsignarVariable(id, retorno, context.Start);
                            //Esto es para mostrar la tabla de simbolos en el frontend
                            //public SimboloEntry(string id, string tipo_simbolo, string tipo_dato, string ambito, int linea, int columna)
                            simbolos.Add(new SimboloEntry(id,"Variable",retorno.ToString(),"Global",context.Start.Line, context.Start.Column));
                            break;
                        case (BoolValue l, BoolValue r):
                            //Vaciamos la lista por si tiene algo ya que es una igualacion
                            dato2.datos.Clear();
                            //Recorremos el array a copiar y agregamos el elemento al array principal
                            foreach (ValueWrapper elemento in dato.datos){
                                dato2.datos.Add(elemento);
                            }
                            //Agregamos a la tabla de valores
                            retorno = currentEnvironment.AsignarVariable(id, retorno, context.Start);
                            //Esto es para mostrar la tabla de simbolos en el frontend
                            //public SimboloEntry(string id, string tipo_simbolo, string tipo_dato, string ambito, int linea, int columna)
                            simbolos.Add(new SimboloEntry(id,"Variable",retorno.ToString(),"Global",context.Start.Line, context.Start.Column));
                            break;
                        case (StringValue l, StringValue r):
                            //Vaciamos la lista por si tiene algo ya que es una igualacion
                            dato2.datos.Clear();
                            //Recorremos el array a copiar y agregamos el elemento al array principal
                            foreach (ValueWrapper elemento in dato.datos){
                                dato2.datos.Add(elemento);
                            }
                            //Agregamos a la tabla de valores
                            retorno = currentEnvironment.AsignarVariable(id, retorno, context.Start);
                            //Esto es para mostrar la tabla de simbolos en el frontend
                            //public SimboloEntry(string id, string tipo_simbolo, string tipo_dato, string ambito, int linea, int columna)
                            simbolos.Add(new SimboloEntry(id,"Variable",retorno.ToString(),"Global",context.Start.Line, context.Start.Column));
                            break;
                        case (CharValue l, CharValue r):
                            //Vaciamos la lista por si tiene algo ya que es una igualacion
                            dato2.datos.Clear();
                            //Recorremos el array a copiar y agregamos el elemento al array principal
                            foreach (ValueWrapper elemento in dato.datos){
                                dato2.datos.Add(elemento);
                            }
                            //Agregamos a la tabla de valores
                            retorno = currentEnvironment.AsignarVariable(id, retorno, context.Start);
                            //Esto es para mostrar la tabla de simbolos en el frontend
                            //public SimboloEntry(string id, string tipo_simbolo, string tipo_dato, string ambito, int linea, int columna)
                            simbolos.Add(new SimboloEntry(id,"Variable",retorno.ToString(),"Global",context.Start.Line, context.Start.Column));
                            break;
                        default:
                            throw new SemanticError("Operacion invalida: en asignacion (=) el tipo de expresion no es igual al tipo del Slice", context.Start);
                    }
                    
                    return retorno;
                }
                throw new SemanticError("Operacion invalida: en asignacion de Slice, los dos tipos deben ser Slice y del mismo tipo", context.Start);
            }
            else{
                retorno =  currentEnvironment.AsignarVariable(id, value, context.Start);
                //Esto es para mostrar la tabla de simbolos en el frontend
                //public SimboloEntry(string id, string tipo_simbolo, string tipo_dato, string ambito, int linea, int columna)
                simbolos.Add(new SimboloEntry(id,"Variable",value.ToString(),"Global",context.Start.Line, context.Start.Column));
            }
            return retorno;
        }
        else if(asignee is LanguageParser.LlamadaFuncioContext calleContext)
        {
            //System.Console.WriteLine("Entro aqui miguelas 22222*************");
            //expre = expre ;
            //var asignee = context.expr(0);

            //Esta se utiliza para las llamadas a funciones
            //SOn las llamadas: z.c.f.d().f = das

            // expr llamada+                             # LlamadaFuncio
            // llamada: '('args?')'                            # Llama
            //     | '.' ID                                    # Gets

            // args: expr (','expr)*;
            
            //a.b.c.d = 2   (esto se valida aca)
            ValueWrapper id = Visit(calleContext.expr());//OBtenemos el identificar de la funcion
            //System.Console.WriteLine($"esta es la expre: {id}");
            for(int i = 0; i<calleContext.llamada().Length; i++){
                //Recorremos en cada llamada
                var action = calleContext.llamada(i);
                //Validamos que estemos en la ultima posicion
                if(i == calleContext.llamada().Length-1)
                {
                    if(action is LanguageParser.GetsContext propertyAccess)
                    {
                        //System.Console.WriteLine(id.GetType());
                        //Aca se valida cuando accedo a la propiedas de obtener
                        if(id is InstanceValue instanceValue)
                        {
                            //id = instanceValue.Instance.Get(propertyAccess.ID().GetText(), propertyAccess.Start);
                            var instance = instanceValue.Instance;
                            var propertyName = propertyAccess.ID().GetText();
                            //System.Console.WriteLine(propertyName);
                            //System.Console.WriteLine(value);
                            //System.Console.WriteLine(instance);
                            instance.Set(propertyName, value);
                        }
                        else
                        {
                            throw new SemanticError("Acceso a la Propiedad invalida", context.Start);
                        }
                    }
                    else{
                        //Aca es cuando estoy intentando asignar algo invalido
                        //a.b.c() = 2;    esto no tiene sentido por eso esta exeocion
                        throw new SemanticError("Asignacion Invalida", context.Start);
                    }
                }

                if(action is LanguageParser.LlamaContext funcall)
                {
                    if(id is FunctionValue functionValue){
                        id = EjecutarLLamada(functionValue.invocable, funcall.args());
                    }
                    else{
                        throw new SemanticError("Llamada a funcion invalida", context.Start);
                    }
                }
                else if(action is LanguageParser.GetsContext propertyAccess)
                {
                    if(id is InstanceValue instanceValue)
                    {
                        //System.Console.WriteLine($"name = {propertyAccess.ID().GetText()}");
                        id = instanceValue.Instance.Get(propertyAccess.ID().GetText(), propertyAccess.Start);
                        //System.Console.WriteLine($"valor retorno = {id}");
                    }
                    else{
                        throw new SemanticError("Invalido acceso a la propiedad", context.Start);
                    }
                }
            }

            return id;
            //return defaultValue;
        }
        else{
            throw new SemanticError("Asignacion invalida", context.Start);
        }
    }

    //Validacion de las Operador de asignación
    //VisitAsig_Aumento
    public override ValueWrapper VisitAsig_Aumento(LanguageParser.Asig_AumentoContext context)
    {   
        string id = context.ID().GetText();
        ValueWrapper lefth = currentEnvironment.GetVariable(id, context.Start);
        ValueWrapper rigth = Visit(context.expr());

        //System.Console.WriteLine("Hola miguel");
        //Aca se deben de validar los tipos permitidos entre operaciones de tipos
        ValueWrapper value = (lefth, rigth) switch
        {
            (IntValue l, IntValue r) => new IntValue(l.Value + r.Value),
            (IntValue l, FloatValue r) => new FloatValue(l.Value + r.Value),
            (FloatValue l, FloatValue r) => new FloatValue(l.Value + r.Value),
            (FloatValue l, IntValue r) => new FloatValue(l.Value + r.Value),
            (StringValue l, StringValue r) => new StringValue(l.Value + r.Value),// Arriba Aca va la primera tabla de sumas
            _=> throw new SemanticError("Operacion invalida: en operaciones de asignacion", context.Start)
        };

        //Esto es para mostrar la tabla de simbolos en el frontend
        //public SimboloEntry(string id, string tipo_simbolo, string tipo_dato, string ambito, int linea, int columna)
        simbolos.Add(new SimboloEntry(id,"Variable",value.ToString(),"Global",context.Start.Line, context.Start.Column));
        return currentEnvironment.AsignarVariable(id, value, context.Start);
    }

    //VisitAsig_Decre
    public override ValueWrapper VisitAsig_Decre(LanguageParser.Asig_DecreContext context)
    {
        string id = context.ID().GetText();
        ValueWrapper lefth = currentEnvironment.GetVariable(id, context.Start);
        ValueWrapper rigth = Visit(context.expr());

        //Aca se deben de validar los tipos permitidos entre operaciones de tipos
        ValueWrapper value = (lefth, rigth) switch
        {
            (IntValue l, IntValue r) => new IntValue(l.Value - r.Value),
            (IntValue l, FloatValue r) => new FloatValue(l.Value - r.Value),
            (FloatValue l, FloatValue r) => new FloatValue(l.Value - r.Value),
            (FloatValue l, IntValue r) => new FloatValue(l.Value - r.Value),
            _=> throw new SemanticError("Operacion invalida: en operaciones de asignacion", context.Start)
        };
        //Esto es para mostrar la tabla de simbolos en el frontend
        //public SimboloEntry(string id, string tipo_simbolo, string tipo_dato, string ambito, int linea, int columna)
        simbolos.Add(new SimboloEntry(id,"Variable",value.ToString(),"Global",context.Start.Line, context.Start.Column));
        return currentEnvironment.AsignarVariable(id, value, context.Start);
    }

    //VisitAsigna
    public override ValueWrapper VisitAsigna(LanguageParser.AsignaContext context){
        return Visit(context.asignacion()); // Evaluar la asignación
         //defaultValue; // No retorna nada
    }

    //Solo se visita la producion
    public override ValueWrapper VisitSoloPasar(LanguageParser.SoloPasarContext context)
    {
        return Visit(context.asignacion()); // Evaluar la asignación
    }


    // VisitNegacion
    public override ValueWrapper VisitNegacion(LanguageParser.NegacionContext context){
        ValueWrapper left = Visit(context.expr());
        
        return (left) switch 
        {
            BoolValue l => new BoolValue(!l.Value),
            _=> throw new SemanticError("Operacion invalida: en operaciones de negacion !", context.Start)
        };
    }

    // VisitOr
    public override ValueWrapper VisitOr(LanguageParser.OrContext context){
        ValueWrapper left = Visit(context.expr(0));
        ValueWrapper right = Visit(context.expr(1));

        return (left, right) switch 
        {
            (BoolValue l, BoolValue r) => new BoolValue(l.Value || r.Value),
            _=> throw new SemanticError("Operacion invalida: en operaciones logica OR ", context.Start)
        };
    }

    // VisitAnd
    public override ValueWrapper VisitAnd(LanguageParser.AndContext context){
        ValueWrapper left = Visit(context.expr(0));
        ValueWrapper right = Visit(context.expr(1));

        return (left, right) switch 
        {
            (BoolValue l, BoolValue r) => new BoolValue(l.Value && r.Value),
            _=> throw new SemanticError("Operacion invalida: en operaciones logica AND ", context.Start)
        };
    }

    //VisitIfstmt
    public override ValueWrapper VisitIfstat(LanguageParser.IfstatContext context){
        ValueWrapper condicion = Visit(context.expr());

        if(condicion is not BoolValue){
            throw new SemanticError("La condicion del IF debe ser booleana", context.Start);
        }

        if((condicion as BoolValue).Value){
            Visit(context.stmt(0));
        }
        else if (context.stmt().Length > 1){
            Visit(context.stmt(1));
        }

        return defaultValue;
    }    


    //VisitWhileStmt    'for' expr stmt                                   # WhileStmt
    public override ValueWrapper VisitWhileStmt(LanguageParser.WhileStmtContext context){
        Environment anteriorEntorno = currentEnvironment;
        currentEnvironment = new Environment(anteriorEntorno);

        ValueWrapper condicion = Visit(context.expr());//Esta es la condicion de enmedio del for

        //Comprovamos que la condicion de enmedio se booleana
        if (condicion is not BoolValue){
            throw new SemanticError("Condicion Invalida para el For", context.Start);
        }

        try
        {
            //Recorremos el cuerpo del for
            while (condicion is BoolValue boolValue && boolValue.Value){
                try{
                    //System.Console.WriteLine("Estoy aqui en el while");
                    Visit(context.stmt());//Recorremos el cuerpo
                    condicion = Visit(context.expr());//Volvemos actulizar la condicion es decir la parte de enmedio del for
                }catch(ContinueException){
                }
            }

        }catch(BreakException){
        }

        currentEnvironment = anteriorEntorno;
        return defaultValue;
    }

    //VisitInstrucSwitch
    public override ValueWrapper VisitInstrucSwitch(LanguageParser.InstrucSwitchContext context){
        //Generamos un nuevo entorno para el switch
        Environment anteriorEntorno = currentEnvironment;
        currentEnvironment = new Environment(anteriorEntorno);

        ValueWrapper switchExpr = Visit(context.expr()); // Evaluamos la expresión del switch

        try
        {
            // Iteramos sobre cada caso del switch
            foreach (var caseCtx in context.instCase()) {
                if (Visit(caseCtx) is CaseResult caseResult && (switchExpr == caseResult.CaseValue)) {
                    // Console.WriteLine("Este es el switch: ");
                    // Console.WriteLine(switchExpr);
                    // Console.WriteLine("Este es el valor del caso: ");
                    // Console.WriteLine(caseResult.CaseValue);
                    //Recorremos el bloque de declaraciones
                    foreach (var stmt in caseResult.ExecuteBlock){
                        Visit(stmt);
                    }
                    //retornamos el entorno principal
                    currentEnvironment = anteriorEntorno;
                    return defaultValue;
                }
            }
            // Si no hubo coincidencia en los cases, ejecutamos el default si existe
            if (context.instDefault() != null)
            {
                ValueWrapper resultado =  Visit(context.instDefault());
                currentEnvironment = anteriorEntorno;
                return resultado;
            }
        }catch(BreakException){

        }
        currentEnvironment = anteriorEntorno;
        return defaultValue; // Si no hay case válido y no hay default, no hacemos nada
    }
    //VisitInstrucCase
    public override ValueWrapper VisitInstrucCase(LanguageParser.InstrucCaseContext context){
        ValueWrapper caseExpr = Visit(context.expr()); // Evaluamos la expresión del case
        ValueWrapper retorno = new CaseResult(caseExpr, context.declaraciones()); // Devolvemos una estructura para manejar el case
        return retorno;
    }
    //VisitInstrucDefault
    public override ValueWrapper VisitInstrucDefault(LanguageParser.InstrucDefaultContext context){
        foreach(var decla in context.declaraciones()){
            Visit(decla);
        }
        return defaultValue; // Ejecutamos las declaraciones del default
    }


    //VisitForStmt
    public override ValueWrapper VisitForStmt(LanguageParser.ForStmtContext context){
        //Generamos un nuevo entorno para el For
        Environment anteriorEntorno = currentEnvironment;
        currentEnvironment = new Environment(anteriorEntorno);

        Visit(context.forInit());

        VisitForBody(context);

        currentEnvironment = anteriorEntorno;
        return defaultValue;
    }

    //Esta recorre el cuerpo del For con validaciones 
    public void VisitForBody(LanguageParser.ForStmtContext context){
        ValueWrapper condicion = Visit(context.expr(0));//Esta es la condicion de enmedio del for

        //Guardamos el ultimo entorno por si en dado caso viene una sentecia break y asi guardar el ultimo entorno
        var lastEnviroment = currentEnvironment;

        //Comprovamos que la condicion de enmedio se booleana
        if (condicion is not BoolValue){
            throw new SemanticError("Condicion Invalida para el For", context.Start);
        }

        try
        {
            //Recorremos el cuerpo del for
            while (condicion is BoolValue boolValue && boolValue.Value){
                Visit(context.stmt());//Recorremos el cuerpo
                Visit(context.expr(1));//Recorremos la condicion numero 3 del cuerpo del for
                condicion = Visit(context.expr(0));//Volvemos actulizar la condicion es decir la parte de enmedio del for
            }

        }catch(BreakException){
            //Aca ucurio un break por lo tanto retornamos el ultimo entorno al prncipal
            currentEnvironment = lastEnviroment;//
        }catch(ContinueException){
            //Si es un continue
            currentEnvironment = lastEnviroment;//Regresamos al actual entorno ya que se volvera a crear otro entorno deltro del visitForBody
            Visit(context.expr(1)); //Visitamos la condicion numero 3 del for, para actualizar
            VisitForBody(context); //Iteramos a la siguiente condicion del for
        }
    }

    //VisitBreakStmt
    public override ValueWrapper VisitBreakStmt(LanguageParser.BreakStmtContext context){
        throw new BreakException();
    }

    //VisitContinueStmt
    public override ValueWrapper VisitContinueStmt(LanguageParser.ContinueStmtContext context){
        throw new ContinueException();
    }

    //VisitReturnStmt
    public override ValueWrapper VisitReturnStmt(LanguageParser.ReturnStmtContext context){
        ValueWrapper value = defaultValue;
        if (context.expr() != null){
            value = Visit(context.expr());
        }
        throw new ReturnException(value);
    }


    //VisitSliceIniciali    ID ':=' '[' ']' tipos '{'expr*'}' ';' 
    //return new IntValue(int.Parse(context.INT().GetText())); 
    public override ValueWrapper VisitSliceIniciali(LanguageParser.SliceInicialiContext context){
        string id = context.ID().GetText(); //Obtenemos el id
        string tipo = context.tipos().GetText(); //Obtenemos el tipo
        ValueWrapper value2 = TypeChecker.GetDefaultValue(tipo); //Declaramos un valor por defecto al value
        // Declarar un arreglo dinámico de ValueWrapper
        List<ValueWrapper> datos = new List<ValueWrapper>();
        ValueWrapper value = defaultValue;

        foreach (var expre in context.expr()){
            value = Visit(expre);
            
            if (!TypeChecker.CheckTypeCompatibility(tipo, value)){
                throw new SemanticError($"Error de tipo en Slice: Se esperaba '{tipo}' pero se obtuvo '{value.GetType().Name}'", context.Start);
            }
            //Console.WriteLine($"Tipo de '{id}': {value.GetType().Name}");
            datos.Add(value);
        }
        // Llamada a la tabla de símbolos (o entorno actual)
        ValueWrapper lista = new Slices(datos, value2);//Generamos un tipo Slice
        currentEnvironment.DeclararVariable(id, lista, context.Start);//Guardamos los datos en la tabla de entorno
        //Esto es para mostrar la tabla de simbolos en el frontend
        //public SimboloEntry(string id, string tipo_simbolo, string tipo_dato, string ambito, int linea, int columna)
        simbolos.Add(new SimboloEntry(id,"Slice","Slice","Global",context.Start.Line, context.Start.Column));
        return defaultValue;
    }

    //VisitSliceNoIncial   'var' ID '[' ']' tipos ';'  
    public override ValueWrapper VisitSliceNoIncial(LanguageParser.SliceNoIncialContext context){
        string id = context.ID().GetText(); //Obtenemos el id
        string tipo = context.tipos().GetText(); //Obtenemos el tipo
        ValueWrapper value = TypeChecker.GetDefaultValue(tipo); //Declaramos un valor por defecto al value
        // Declarar un arreglo dinámico de ValueWrapper
        List<ValueWrapper> datos = new List<ValueWrapper>();

        // Llamada a la tabla de símbolos (o entorno actual)
        ValueWrapper lista = new Slices(datos, value);//Generamos un tipo Slice
        currentEnvironment.DeclararVariable(id, lista, context.Start);//Guardamos los datos en la tabla de entorno
        //Esto es para mostrar la tabla de simbolos en el frontend
        //public SimboloEntry(string id, string tipo_simbolo, string tipo_dato, string ambito, int linea, int columna)
        simbolos.Add(new SimboloEntry(id,"Slice",lista.ToString(),"Global",context.Start.Line, context.Start.Column));
        return defaultValue;
    }

    //VisitForSlice     'for' ID ',' ID ':=' 'range' ID stmt 
    public override ValueWrapper VisitForSlice(LanguageParser.ForSliceContext context){
        //Generamos un nuevo entorno para el For
        Environment anteriorEntorno = currentEnvironment;
        currentEnvironment = new Environment(anteriorEntorno);

        string id1 = context.ID(0).GetText(); //Obtenemos el id
        string id2 = context.ID(1).GetText(); //Obtenemos el id
        string name = context.ID(2).GetText(); //Obtenemos el id

        // Declarar un arreglo dinámico de ValueWrapper
        List<ValueWrapper> datos = new List<ValueWrapper>();

        //Obtenemos el slice para recorrerlo
        ValueWrapper slice = currentEnvironment.GetVariable(name, context.Start);

        //Declaramos un valor por defecto al id1 y lo agregamos a la tabla de simbolos
        ValueWrapper indice = TypeChecker.GetDefaultValue("int");
        currentEnvironment.DeclararVariable(id1, indice, context.Start);
        //Esto es para mostrar la tabla de simbolos en el frontend
        //public SimboloEntry(string id, string tipo_simbolo, string tipo_dato, string ambito, int linea, int columna)
        simbolos.Add(new SimboloEntry(id1,"For Range",indice.ToString(),"Global",context.Start.Line, context.Start.Column));

        //Declaramos el valor y el tipo en la tabla de simbolos 
        ValueWrapper valor = defaultValue;
        if (slice is Slices slicesValue) {
            valor = slicesValue.tipo;
            datos = slicesValue.datos;
        } else {
            throw new SemanticError($"La variable '{name}' no es un slice.", context.Start);
        }
        currentEnvironment.DeclararVariable(id2, valor, context.Start);
        //Esto es para mostrar la tabla de simbolos en el frontend
        //public SimboloEntry(string id, string tipo_simbolo, string tipo_dato, string ambito, int linea, int columna)
        simbolos.Add(new SimboloEntry(id2,"For Range",valor.ToString(),"Global",context.Start.Line, context.Start.Column));

        //Analizamos el cuerpo del for
        VisitForRangeBody(datos, context, id1, id2);

        currentEnvironment = anteriorEntorno;
        return defaultValue;
    }

    //Esta recorre el cuerpo del For range con validaciones 
    public void VisitForRangeBody(List<ValueWrapper> datos, LanguageParser.ForSliceContext context, string indice, string value) {
        var lastEnviroment = currentEnvironment;
        
        try {
            int index = 0; // Inicializamos el índice
            
            foreach (ValueWrapper elemento in datos) {
                try {
                    // Actualizamos el valor del dato en la tabla de símbolos
                    currentEnvironment.AsignarVariable(value, elemento, context.Start);
                    // Actualizamos el valor del índice en la tabla de símbolos 
                    currentEnvironment.AsignarVariable(indice, new IntValue(index), context.Start);
                    // Visitamos el cuerpo del for
                    Visit(context.stmt());
                } catch (ContinueException) {
                    // Capturamos el continue y dejamos que el bucle continúe
                }
                index++; // Incrementamos el contador después de cada iteración válida
            }
        } catch (BreakException) {
            // Si ocurre un break, simplemente terminamos el bucle
        } finally {
            // Restauramos el entorno después de la ejecución
            currentEnvironment = lastEnviroment;
        }
    }


    //VisitAumento_Uno     ID '++'
    public override ValueWrapper VisitAumento_Uno(LanguageParser.Aumento_UnoContext context)
    {   
        string id = context.ID().GetText();
        ValueWrapper lefth = currentEnvironment.GetVariable(id, context.Start);
        
        //Aca se deben de validar los tipos permitidos entre operaciones de tipos
        ValueWrapper value = (lefth) switch
        {
            (IntValue l) => new IntValue(l.Value + 1),
            (FloatValue l) => new FloatValue(l.Value + 1),
            _=> throw new SemanticError("Operacion invalida: en operaciones de incremento ++", context.Start)
        };
        //Esto es para mostrar la tabla de simbolos en el frontend
        //public SimboloEntry(string id, string tipo_simbolo, string tipo_dato, string ambito, int linea, int columna)
        simbolos.Add(new SimboloEntry(id,"Variable incremento",value.ToString(),"Global",context.Start.Line, context.Start.Column));
        return currentEnvironment.AsignarVariable(id, value, context.Start);
    }


    //VisitDecreme_Uno     ID '--'
    public override ValueWrapper VisitDecreme_Uno(LanguageParser.Decreme_UnoContext context)
    {   
        string id = context.ID().GetText();
        ValueWrapper lefth = currentEnvironment.GetVariable(id, context.Start);
        
        //Aca se deben de validar los tipos permitidos entre operaciones de tipos
        ValueWrapper value = (lefth) switch
        {
            (IntValue l) => new IntValue(l.Value - 1),
            (FloatValue l) => new FloatValue(l.Value - 1),
            _=> throw new SemanticError("Operacion invalida: en operaciones de decremento --", context.Start)
        };
        //Esto es para mostrar la tabla de simbolos en el frontend
        //public SimboloEntry(string id, string tipo_simbolo, string tipo_dato, string ambito, int linea, int columna)
        simbolos.Add(new SimboloEntry(id,"Variable decremento",value.ToString(),"Global",context.Start.Line, context.Start.Column));
        return currentEnvironment.AsignarVariable(id, value, context.Start);
    }


    //VisitLlamadaFuncio
    //expr llamada+                             # LlamadaFuncio
    // llamada: '('args?')'                     # Llama
    // | '.' ID                                 # Gets

    // args: expr (','expr)*;
    public override ValueWrapper VisitLlamadaFuncio(LanguageParser.LlamadaFuncioContext context)
    {
        ValueWrapper calle = Visit(context.expr()); // Obtenemos el identificador de la función
        //System.Console.WriteLine("ingresando aca");
        foreach (var action in context.llamada())
        {
            if (action is LanguageParser.LlamaContext funcall)
            {
                // Llamada a función
                if (calle is FunctionValue functionValue)
                {
                    calle = EjecutarLLamada(functionValue.invocable, funcall.args());
                }
                else
                {
                    throw new SemanticError("Llamada a funcion invalida", context.Start);
                }
            }
            // Esto es cuando es un acceso (.)
            else if (action is LanguageParser.GetsContext propertyAccess)
            {
                //System.Console.WriteLine($"Este es el tipo: {calle is InstanceValue}");
                
                // Acceso a propiedad de una instancia de struct
                if (calle is InstanceValue instanceValue)
                {
                    calle = instanceValue.Instance.Get(propertyAccess.ID().GetText(), propertyAccess.Start);
                    //System.Console.WriteLine($"Esta es el retorno: {calle}");
                }
                else
                {
                    throw new SemanticError("Acceso a la Propiedad Invalida", context.Start);
                }
            }
        }
        return calle;
    }

    //Esto es para recorrer el cuarpo de la funcion
    public ValueWrapper EjecutarLLamada(Invocable invocable, LanguageParser.ArgsContext context){
        //Aca se genera una lista de valuewraper
        List<ValueWrapper> argumen = new List<ValueWrapper>();
        if(context != null){
            //Recorremos la lista de argumentos
            foreach(var expr in context.expr()){
                argumen.Add(Visit(expr));
            }
        }
        //System.Console.WriteLine($"Cantida de elementos: {argumen.Count}");
        //Aca se valida cuantos parametros se mandan
        if(context != null && argumen.Count != invocable.Arity()){
            throw new SemanticError("Invalido numero de argumentos para la llamada a la funcion", context.Start);
        }
        //System.Console.WriteLine($"Estos son lo argumentos: {argumen}");
        return invocable.Invoke(argumen, this);
    }




    //********************************Funciones Embedias******************************
    //VisitFuncAtoi      'strconv.Atoi' '('expr')'                 # FuncAtoi
    public override ValueWrapper VisitFuncAtoi(LanguageParser.FuncAtoiContext context)
    {
        //Obtenemos el valor de la exprecion "1234"
        ValueWrapper valor = Visit(context.expr());
        //System.Console.WriteLine(valor);
        int result = 0;
        if(valor is StringValue cadena){
            //Guardamos en una variable el valor en cadena
            string input = cadena.Value; 
            try
            {
                result = int.Parse(input);
                //Console.WriteLine($"Número convertido: {result}");
            }
            catch (FormatException)
            {
                throw new SemanticError("Error: La cadena no tiene un formato válido para plicar strconv.Atoi",context.Start);
            }
            catch (OverflowException)
            {
                throw new SemanticError("Error: El número es demasiado grande o pequeño para plicar strconv.Atoi", context.Start);
            }
        }
        else if(valor is CharValue caracter){
            //Guardamos en una variable el valor en cadena
            string input = (caracter.Value).ToString(); 
            try
            {
                result = int.Parse(input);
                Console.WriteLine($"Número convertido: {result}");
            }
            catch (FormatException)
            {
                throw new SemanticError("Error: La cadena no tiene un formato válido para plicar strconv.Atoi",context.Start);
            }
            catch (OverflowException)
            {
                throw new SemanticError("Error: El número es demasiado grande o pequeño para plicar strconv.Atoi", context.Start);
            }
        }
        else{
            throw new SemanticError("Error: el tipo a conversion debe de ser string en strconv.Atoi", context.Start);
        }
        //Retornamos la variable con el tipo Valuewraper IntValue
        return new IntValue(result);
    }


    //VisitFuncParFloat     'strconv.ParseFloat' '('expr')'           # FuncParFloat
    public override ValueWrapper VisitFuncParFloat(LanguageParser.FuncParFloatContext context)
    {
        //Obtenemos el valor de la exprecion "1234"
        ValueWrapper valor = Visit(context.expr());
        //System.Console.WriteLine(valor);
        float result = 0;
        if(valor is StringValue cadena){
            string input = cadena.Value; // Prueba con "abc" para ver el error
            try
            {
                result = float.Parse(input, CultureInfo.InvariantCulture);
                Console.WriteLine($"Número convertido: {result}");
            }
            catch (FormatException)
            {
                //Console.WriteLine("Error: La cadena no tiene un formato válido.");
                throw new SemanticError("Error: La cadena no tiene un formato válido para plicar strconv.ParseFloat", context.Start);
            }
            catch (OverflowException)
            {
                //Console.WriteLine("Error: El número es demasiado grande o pequeño.");
                throw new SemanticError("Error: El número es demasiado grande o pequeño para plicar strconv.ParseFloat", context.Start);
            }
        }
        else{
            throw new SemanticError("Error: el tipo a conversion debe de ser string en strconv.ParseFloat", context.Start);
        }
        //Retornamos la variable con el tipo Valuewraper IntValue
        return new FloatValue(result);
    }


    //VisitFuncTypeOf       'reflect.TypeOf' '('expr')'               # FuncTypeOf
    /*
        string id = context.ID().GetText();
        return currentEnvironment.GetVariable(id, context.Start);
        retorna ValueWraper   []int
    */
    public override ValueWrapper VisitFuncTypeOf(LanguageParser.FuncTypeOfContext context)
    {
        //Obtenemos el valor de la exprecion "1234"
        ValueWrapper valor = Visit(context.expr());

        ValueWrapper resul = (valor) switch
        {
            IntValue I => new StringValue("int"),
            FloatValue F => new StringValue("float64"),
            StringValue S => new StringValue("string"),
            BoolValue B => new StringValue("bool"),
            CharValue C => new StringValue("rune"),
            Slices S => new StringValue("[]"+RetornarTipo(S.tipo)),
            InstanceValue st => new StringValue(st.Instance.languageStruct.Name),
            _=> throw new SemanticError($"Operacion invalida: en reflect.TypeOf ya que el tipo: {valor} no es de los permitidos", context.Start)
        };

        return resul;
    }


    private string RetornarTipo(ValueWrapper value)
    {
        return value switch
        {
            IntValue i => "int",
            FloatValue f => "float64",
            StringValue s => "string", // Agrega comillas a los strings
            BoolValue b => "bool", // true/false en minúsculas
            CharValue c => "rune", // Muestra los caracteres con comillas simples
            _ => value.ToString() // Si hay otro tipo, usa su ToString() por defecto
        };
    }

    //VisitFuncIndex    'slices.Index' '(' ID ',' expr')'        # FuncIndex
    public override ValueWrapper VisitFuncIndex(LanguageParser.FuncIndexContext context)
    {
        string name = context.ID().GetText();
        ValueWrapper expresion = Visit(context.expr());

        int indice = 0;
        ValueWrapper value = currentEnvironment.GetVariable(name, context.Start);
        //Aca se comprueba que sea un slice lo que se obtuvo con el id
        if(value is Slices lista){
            //Aca se comprueba si el tipo de la expresion es el mismo con el tipo del slice
            indice = (lista.tipo, expresion) switch
            {
                (IntValue l, IntValue r) => lista.datos.IndexOf(expresion),
                (FloatValue l, FloatValue r) => lista.datos.IndexOf(expresion),
                (BoolValue l, BoolValue r) => lista.datos.IndexOf(expresion),
                (StringValue l, StringValue r) => lista.datos.IndexOf(expresion),
                (CharValue l, CharValue r) => lista.datos.IndexOf(expresion),
                _=> throw new SemanticError("Operacion invalida: en slices.Index el tipo de espresion no es igual al tipo de Slice", context.Start)
            };
        }
        else{
            throw new SemanticError("Operacion invalida: no se puede aplicar slices.Index a una variable que no se SLICE", context.Start);
        }

        return new IntValue(indice);
    }



    //VisitFuncJoin    'strings.Join' '('ID ',' expr')'          # FuncJoin
    public override ValueWrapper VisitFuncJoin(LanguageParser.FuncJoinContext context)
    {
        string name = context.ID().GetText();
        ValueWrapper expresion = Visit(context.expr());

        string union = "";
        ValueWrapper value = currentEnvironment.GetVariable(name, context.Start);
        if(value is Slices lista && expresion is StringValue sep && lista.tipo is StringValue){
            union += string.Join(sep.Value,lista.datos.Select(FormatValue2));
        }
        else{
            throw new SemanticError("Operacion invalida: no se puede aplicar strings.Join a una variable que no se SLICE de tipo string", context.Start);
        }
        return new StringValue(union);
    }
    //Esto me retorna el string que se agrega a la cadena
    private string FormatValue2(ValueWrapper value)
    {
        return value switch
        {
            StringValue s => $"{s.Value}", 
            _ => value.ToString() 
        };
    }


    //VisitFuncLen   'len' '('expre')'                            # FuncLen
    /*
            | ID '[' expr ']'                           # ObtenerPos
            | ID '[' expr ']' '[' expr ']'              # ObtenerMatris
            | ID                                        # Identifaider
    */
    public override ValueWrapper VisitFuncLen(LanguageParser.FuncLenContext context)
    {
        int longitud = 0;
        if(context.expr() is LanguageParser.IdentifaiderContext context0){
            string name = context0.ID().GetText();
            ValueWrapper value = currentEnvironment.GetVariable(name, context0.Start);
            
            if(value is Slices lista){
                longitud = lista.datos.Count;
            }
            else if(value is MatrixValue mat){
                longitud = mat.Values.Count;
            }
            else{
                throw new SemanticError("Operacion invalida: no se puede aplicar len a una variable que no sea SLICEs", context.Start);
            }
        }
        else if(context.expr() is LanguageParser.ObtenerMatrisContext context3){
                // Validar que sea obtener el len de una matris
                string name = context3.ID().GetText();
                ValueWrapper value = currentEnvironment.GetVariable(name, context3.Start);
                if(value is MatrixValue lista){
                    longitud = lista.Values.Count;
                }
        }
        else if(context.expr() is LanguageParser.ObtenerPosContext context4){
                //ID '[' expr ']'                           # ObtenerPos
                string name = context4.ID().GetText();
                ValueWrapper indice = Visit(context4.expr());
                ValueWrapper value = currentEnvironment.GetVariable(name, context4.Start);
                if(value is Slices lista){
                    longitud = lista.datos.Count;
                }
                else if(value is MatrixValue matr && indice is IntValue indice2){
                    longitud = matr.Values[indice2.Value].Count;
                }
        }
        else{
            throw new SemanticError("Operacion invalida: no se puede aplicar len a una variable que no sea SLICE", context.Start);
        }

        return new IntValue(longitud);
    }

    // Falta el visitor
    //numeros = []int{10, 20, 30, 40, 50};
    //ReasignarSlice

    //ID '=' '[' ']' tipos '{'(expr ','?)*'}' ';'               # ReasignarSlice

    public override ValueWrapper VisitReasignarSlice(LanguageParser.ReasignarSliceContext context)
    {
        string id = context.ID().GetText(); //Obtenemos el id
        string tipo = context.tipos().GetText(); //Obtenemos el tipo
        ValueWrapper value2 = TypeChecker.GetDefaultValue(tipo); //Declaramos un valor por defecto al value
        // Declarar un arreglo dinámico de ValueWrapper
        List<ValueWrapper> datos = new List<ValueWrapper>();
        ValueWrapper value = defaultValue;

        foreach (var expre in context.expr()){
            value = Visit(expre);
            
            if (!TypeChecker.CheckTypeCompatibility(tipo, value)){
                throw new SemanticError($"Error de tipo en Slice: Se esperaba '{tipo}' pero se obtuvo '{value.GetType().Name}'", context.Start);
            }
            //Console.WriteLine($"Tipo de '{id}': {value.GetType().Name}");
            datos.Add(value);
        }
        // Llamada a la tabla de símbolos (o entorno actual)
        ValueWrapper lista = new Slices(datos, value2);//Generamos un tipo Slice
        //Esto es para mostrar la tabla de simbolos en el frontend
        //public SimboloEntry(string id, string tipo_simbolo, string tipo_dato, string ambito, int linea, int columna)
        simbolos.Add(new SimboloEntry(id,"Slice",lista.ToString(),"Global",context.Start.Line, context.Start.Column));
        currentEnvironment.AsignarVariable(id, lista, context.Start);//Guardamos los datos en la tabla de entorno
        return defaultValue;
    }


    //VisitFuncAppend     'append' '('ID',' expr')'                 # FuncAppend
    public override ValueWrapper VisitFuncAppend(LanguageParser.FuncAppendContext context)
    {
        string name = context.ID().GetText();
        ValueWrapper expresion = Visit(context.expr());

        ValueWrapper value = currentEnvironment.GetVariable(name, context.Start);
        //Aca se comprueba que sea un slice lo que se obtuvo con el id
        if(value is Slices lista){
            List<ValueWrapper> nuevaLista;
            //Se comprueba que cada tipo se del mismo a asignar
            switch (lista.tipo, expresion)
            {
                case (IntValue l, IntValue r):
                    //Se crea una nueva lista a partir de la anterior
                    nuevaLista = new List<ValueWrapper>(lista.datos);
                    nuevaLista.Add(expresion);//Se agrega el valor
                    break;
                case (FloatValue f, FloatValue g):
                    //Se crea una nueva lista a partir de la anterior
                    nuevaLista = new List<ValueWrapper>(lista.datos);
                    nuevaLista.Add(expresion);//Se agrega el valor
                    break;
                case (BoolValue l, BoolValue r):
                    //Se crea una nueva lista a partir de la anterior
                    nuevaLista = new List<ValueWrapper>(lista.datos);
                    nuevaLista.Add(expresion);//Se agrega el valor
                    break;
                case (StringValue l, StringValue r):
                    //Se crea una nueva lista a partir de la anterior
                    nuevaLista = new List<ValueWrapper>(lista.datos);
                    nuevaLista.Add(expresion);//Se agrega el valor
                    break;
                case (CharValue l, CharValue r):
                    //Se crea una nueva lista a partir de la anterior
                    nuevaLista = new List<ValueWrapper>(lista.datos);
                    nuevaLista.Add(expresion);//Se agrega el valor
                    break;
                default:
                    throw new SemanticError("Operacion invalida: en append el tipo de expresion no es igual al tipo del Slice", context.Start);
            }
            //Se genera un nuevo slice con el tipo de la expresion y un nuevo slice
            return new Slices(nuevaLista, expresion);
        }
        else if(value is MatrixValue matrix && expresion is Slices list){
            List<List<ValueWrapper>> union;
            //Se comprueba que cada tipo se del mismo a asignar
            switch (matrix.Type, list.tipo)
            {
                case ("int", IntValue r):
                    //Se crea una nueva lista a partir de la anterior
                    union = matrix.Values.Select(fila => new List<ValueWrapper>(fila)).ToList();
                    union.Add(list.datos);
                    break;
                case ("float64", FloatValue g):
                    //Se crea una nueva lista a partir de la anterior
                    union = matrix.Values.Select(fila => new List<ValueWrapper>(fila)).ToList();
                    union.Add(list.datos);
                    break;
                case ("bool", BoolValue r):
                    //Se crea una nueva lista a partir de la anterior
                    union = matrix.Values.Select(fila => new List<ValueWrapper>(fila)).ToList();
                    union.Add(list.datos);
                    break;
                case ("string", StringValue r):
                    //Se crea una nueva lista a partir de la anterior
                    union = matrix.Values.Select(fila => new List<ValueWrapper>(fila)).ToList();
                    union.Add(list.datos);
                    break;
                case ("rune", CharValue r):
                    //Se crea una nueva lista a partir de la anterior
                    union = matrix.Values.Select(fila => new List<ValueWrapper>(fila)).ToList();
                    union.Add(list.datos);
                    break;
                default:
                    throw new SemanticError("Operacion invalida: en append el tipo de expresion no es igual al tipo de la Matriz", context.Start);
            }
            return new MatrixValue(name, matrix.Type, union);
        }
        else{
            throw new SemanticError("Operacion invalida: no se puede aplicar append a una variable que no sea SLICE o Matriz", context.Start);
        }
        //return defaultValue;
    }



    //VisitObtenerPos    ID '[' expr ']'                           # ObtenerPos
    public override ValueWrapper VisitObtenerPos(LanguageParser.ObtenerPosContext context)
    {
        string name = context.ID().GetText();
        ValueWrapper expresion = Visit(context.expr());

        ValueWrapper retorno = defaultValue;
        ValueWrapper value = currentEnvironment.GetVariable(name, context.Start);
        if(value is MatrixValue matris && expresion is IntValue pos1){
            if (pos1.Value >= 0 && pos1.Value < matris.Values.Count)
            {
                //TODO: Aca se cambio para retornar una fila de una matriz
                ValueWrapper tipo = TypeChecker.GetDefaultValue(matris.Type);
                retorno = new Slices(matris.Values[pos1.Value], tipo); // Retorna la fila como una lista
            }
            else
            {
                throw new SemanticError("Indice invalida: el indice para acceder a la psocion de la Matris es incorrecto", context.Start);
            }
        }
        else if(value is Slices lista && expresion is IntValue pos){
            //Aca se valida si el indice no supera la longitud del slice y tampoco sea negativo
            if(pos.Value >= 0 && pos.Value < lista.datos.Count){
                retorno = lista.datos[pos.Value];
            }
            else
            {
                throw new SemanticError("Indice invalida: el indice para acceder a la psocion del SLICE es incorrecto", context.Start);
            }
        }
        else{
            throw new SemanticError("Operacion invalida: la variable tiene que ser de tipo SLICE y la expresion int", context.Start);
        }
        //Retornamos unicamnete la posicion ya que la misma es un Valuewrapper
        return retorno;
    }


    //VisitAsignaList   ID '['expr']' '=' expr                    # AsignaList
    public override ValueWrapper VisitAsignaList(LanguageParser.AsignaListContext context)
    {
        string name = context.ID().GetText();
        ValueWrapper expresion1 = Visit(context.expr(0));
        ValueWrapper expresion2 = Visit(context.expr(1));

        ValueWrapper retorno = defaultValue;
        ValueWrapper value = currentEnvironment.GetVariable(name, context.Start);
        if(value is Slices lista && expresion1 is IntValue pos){
            switch (lista.tipo, expresion2)
            {
                case (IntValue l, IntValue r):
                    lista.datos[pos.Value] = expresion2;
                    break;
                case (FloatValue l, FloatValue r):
                    lista.datos[pos.Value] = expresion2;
                    break;
                case (BoolValue l, BoolValue r):
                    lista.datos[pos.Value] = expresion2;
                    break;
                case (StringValue l,StringValue r):
                    lista.datos[pos.Value] = expresion2;
                    break;
                case (CharValue l,CharValue r):
                    lista.datos[pos.Value] = expresion2;
                    break;
                default:
                    throw new SemanticError("Operacion invalida: la asignacion del SLICE no es del mismo tipo del SLICE", context.Start);
            }
            //Esto es para mostrar la tabla de simbolos en el frontend
            //public SimboloEntry(string id, string tipo_simbolo, string tipo_dato, string ambito, int linea, int columna)
            simbolos.Add(new SimboloEntry(name,"Slice",value.ToString(),"Global",context.Start.Line, context.Start.Column));
            currentEnvironment.AsignarVariable(name, value, context.Start);
        }
        else{
            throw new SemanticError("Operacion invalida: la variable tiene que ser de tipo SLICE y la expresion del indice int", context.Start);
        }

        //No es necesario devolver alguna valor ya que no se trabajara con la misa
        return defaultValue;
    }



    /*
        ID ':=' '[' ']' '[' ']' tipos '{' filasMatriz '}' ';'     # Matris
        
        filasMatriz
            : filaMatriz (',' filaMatriz?)* 
        
        filaMatriz
            : '{' expr (',' expr)* '}'
    */
    //VisitMatris
    public override ValueWrapper VisitMatris(LanguageParser.MatrisContext context)
    {
        // 1. Obtener el nombre de la variable y el tipo de la matriz
        string name = context.ID().GetText();
        string tipo = context.tipos().GetText();

        // 2. Crear una lista de listas para almacenar los valores
        List<List<ValueWrapper>> matriz = new List<List<ValueWrapper>>();

        // 3. Obtener todas las filas de la matriz
        foreach (var filaContext in context.filasMatriz().filaMatriz())
        {
            List<ValueWrapper> fila = new List<ValueWrapper>();

            // 4. Recorrer los valores de la fila
            foreach (var exprContext in filaContext.expr())
            {
                ValueWrapper valor = Visit(exprContext); 
                // Evalua el tipo de la expresión con el tipo declarado
                if(TypeChecker.CheckTypeCompatibility(tipo, valor)){
                    fila.Add(valor);
                }
                else{
                    throw new SemanticError("Error de tipos: se esta asignado un tipo de expresion que no es el mismo tipo de la matriz", context.Start);
                }
            }

            // 5. Agregar la fila a la matriz
            matriz.Add(fila);
        }

        // 6. Retornar la matriz envuelta en un ValueWrapper (ajusta según tu estructura)
        currentEnvironment.DeclararVariable(name, new MatrixValue(name, tipo, matriz), context.Start);
        //Esto es para mostrar la tabla de simbolos en el frontend
        //public SimboloEntry(string id, string tipo_simbolo, string tipo_dato, string ambito, int linea, int columna)
        simbolos.Add(new SimboloEntry(name,"Matris",tipo,"Global",context.Start.Line, context.Start.Column));
        return defaultValue;
    }

    //VisitObtenerMatris     ID '[' expr ']' '[' expr ']'              # ObtenerMatris
    public override ValueWrapper VisitObtenerMatris(LanguageParser.ObtenerMatrisContext context)
    {
        string name = context.ID().GetText();
        ValueWrapper expresion1 = Visit(context.expr(0));
        ValueWrapper expresion2 = Visit(context.expr(1));

        ValueWrapper retorno = defaultValue;
        ValueWrapper value = currentEnvironment.GetVariable(name, context.Start);
        if(value is MatrixValue matrix && expresion1 is IntValue pos1 && expresion2 is IntValue pos2){
            //Aca se valida si el indice no supera la longitud del slice y tampoco sea negativo
            if(pos1.Value >= 0 && pos2.Value >= 0){
                retorno = matrix.Values[pos1.Value][pos2.Value];
            }
            else
            {
                throw new SemanticError("Indice invalida: el indice para acceder a la psocion de la Matris es incorrecto", context.Start);
            }
        }
        else{
            throw new SemanticError("Operacion invalida: la variable tiene que ser de tipo Matriz y las expresiones int", context.Start);
        }
        //Retornamos unicamnete la posicion ya que la misma es un Valuewrapper
        return retorno;
    }

    //VisitAsignaMatris      ID '['expr']' '['expr']' '=' expr         # AsignaMatris
    public override ValueWrapper VisitAsignaMatris (LanguageParser.AsignaMatrisContext context)
    {
        string name = context.ID().GetText();
        ValueWrapper expresion1 = Visit(context.expr(0));
        ValueWrapper expresion2 = Visit(context.expr(1));
        ValueWrapper igualacion = Visit(context.expr(2));

        ValueWrapper retorno = defaultValue;
        ValueWrapper value = currentEnvironment.GetVariable(name, context.Start);
        if(value is MatrixValue lista && expresion1 is IntValue pos1 && expresion2 is IntValue pos2){
            switch (lista.Type, igualacion)
            {
                case ("int" , IntValue r):
                    lista.Values[pos1.Value][pos2.Value] = igualacion;
                    break;
                case ("float64" , FloatValue r):
                    lista.Values[pos1.Value][pos2.Value] = igualacion;
                    break;
                case ("bool" , BoolValue r):
                    lista.Values[pos1.Value][pos2.Value] = igualacion;
                    break;
                case ("string",StringValue r):
                    lista.Values[pos1.Value][pos2.Value] = igualacion;
                    break;
                case ("rune" ,CharValue r):
                    lista.Values[pos1.Value][pos2.Value] = igualacion;
                    break;
                default:
                    throw new SemanticError("Operacion invalida: la asignacion de la Matris no es del mismo tipo que la expresion", context.Start);
            }
            currentEnvironment.AsignarVariable(name, value, context.Start);
        }
        else{
            throw new SemanticError("Operacion invalida: la variable tiene que ser de tipo Matris y la expresion de los indices int", context.Start);
        }

        //No es necesario devolver alguna valor ya que no se trabajara con la misa
        return defaultValue;
    }


    //VisitFuncDcl
    //'func' ID '(' params? ')' tipos? '{' declaraciones* '}'    # FuncDcl1
    public override ValueWrapper VisitFuncDcl1(LanguageParser.FuncDcl1Context context)
    {   
        //Unicamente se hace la llamada a la clase de la funcion foranea
        var foranea = new ForaneaFuncion(currentEnvironment, context);
        //Declaramos la funcion dentro del entorno global
        currentEnvironment.DeclararVariable(context.ID().GetText(), new FunctionValue(foranea, context.ID().GetText()), context.Start);
        //Esto es para mostrar la tabla de simbolos en el frontend
        //public SimboloEntry(string id, string tipo_simbolo, string tipo_dato, string ambito, int linea, int columna)
        simbolos.Add(new SimboloEntry(context.ID().GetText(),"Funcion","Funcion","Global",context.Start.Line, context.Start.Column));
        return defaultValue;
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

    public override ValueWrapper VisitInstStruct(LanguageParser.InstStructContext context)
    {
        //System.Console.WriteLine("Primer paso");
        string structName = context.ID().GetText();
        var atributos = new Dictionary<string, DatosStruct>();

        foreach (var prop in context.listaAtributos())
        {
            string tipo = ""; // Primer ID es el tipo
            if(prop.ID(1) != null){
                tipo = prop.ID(1).GetText();
            }
            else{
                tipo = prop.tipos().GetText();
            }
            string nombre = prop.ID(0).GetText(); // Segundo ID es el nombre del atributo
            atributos[nombre] = new DatosStruct(tipo, nombre);
        }

        // Guardamos el struct en nuestra tabla de símbolos
        LanguageStruct languageStruct = new LanguageStruct(structName, atributos);
        currentEnvironment.DeclararVariable(structName, new StructValue(languageStruct), context.Start);
        //Esto es para mostrar la tabla de simbolos en el frontend
        //public SimboloEntry(string id, string tipo_simbolo, string tipo_dato, string ambito, int linea, int columna)
        simbolos.Add(new SimboloEntry(structName,"Struct",structName,"Global",context.Start.Line, context.Start.Column));
        return defaultValue;
    }


    //VisitStructParam 
    /*
        ID ':=' ID '{' datos* '}'                                # StructParam 

        datos: ID ':' expr (',' (ID ':' expr)?)*    
    */
    public override ValueWrapper VisitStructParam(LanguageParser.StructParamContext context)
    {
        //System.Console.WriteLine("Tercero paso");
        string structName = context.ID(1).GetText();
        string variable = context.ID(0).GetText();

        // Obtenemos el struct de la tabla de símbolos
        ValueWrapper structValue = currentEnvironment.GetVariable(structName, context.Start);

        // Verificamos que realmente sea un struct
        if (structValue is not StructValue structDef)
        {
            throw new SemanticError($"El tipo '{structName}' no está definido como struct.", context.Start);
        }

        // Diccionario para almacenar los valores de los atributos
        var valoresAtributos = new Dictionary<string, ValueWrapper>();
        List<ValueWrapper> argumen = new List<ValueWrapper>();

        // Recorremos los datos que vienen en la inicialización del struct
        foreach (var dato in context.datos())
        {   
            for (int i = 0; i < dato.ID().Length; i++) // Iteramos sobre todos los atributos en 'datos'
            {
                string atributo = dato.ID(i).GetText();  // Nombre del atributo
                ValueWrapper valor = Visit(dato.expr(i)); // Evaluamos la expresión del atributo

                // Si el valor es `nil`, lo tratamos como un `VoidValue`
                /*if (valor is VoidValue && structDef.languageStruct.Props[atributo].tipo != "nil")
                {
                    valor = new InstanceValue(null); // Representamos nil como una instancia vacía
                }*/

                // Verificamos que el atributo exista en la definición del struct
                if (!structDef.languageStruct.Props.ContainsKey(atributo))
                {
                    throw new SemanticError($"El struct '{structName}' no tiene un atributo llamado '{atributo}'.", context.Start);
                }

                // Obtenemos el tipo esperado del struct
                string tipoEsperado = structDef.languageStruct.Props[atributo].tipo;

                // Validamos que el tipo del valor coincida con el tipo esperado
                if (!EsTipoCompatible(valor, tipoEsperado))
                {
                    throw new SemanticError($"El atributo '{atributo}' de '{structName}' esperaba un valor de tipo '{tipoEsperado}', pero se recibió '{valor.GetType().Name}'.", context.Start);
                }

                // Guardamos el valor en el diccionario de atributos
                //System.Console.WriteLine($"{atributo} = {valor}");
                valoresAtributos[atributo] = valor;
            }
        }

        
        // Creamos la nueva instancia del struct
        argumen = valoresAtributos.Values.ToList();
        InstanceValue asociacion = (InstanceValue)structDef.languageStruct.Invoke(argumen, this);
        
        //System.Console.WriteLine($"Aca esta el retorno: {asociacion}");

        //structDef.languageStruct.Props
        currentEnvironment.DeclararVariable(variable, asociacion, context.Start);

        //Esto es para mostrar la tabla de simbolos en el frontend
        //public SimboloEntry(string id, string tipo_simbolo, string tipo_dato, string ambito, int linea, int columna)
        simbolos.Add(new SimboloEntry(variable,"Struct",structName,"Global",context.Start.Line, context.Start.Column));
        // Devolvemos la instancia envuelta en `InstanceValue`
        return defaultValue;
    }

    private bool EsTipoCompatible(ValueWrapper valor, string tipoEsperado)
    {
        if ( valor is VoidValue)
        {
            return true; // Permitir nil en cualquier struct
        }

        if (valor is InstanceValue instance)
        {
            return instance.Instance.languageStruct.Name == tipoEsperado;
        }

        return tipoEsperado switch
        {
            "int" => valor is IntValue,
            "float64" => valor is FloatValue,
            "string" => valor is StringValue,
            "bool" => valor is BoolValue,
            "rune" => valor is CharValue,
            "nil" => valor is VoidValue,
            _ => false
        };
    }




    //VisitTipoNil
    public override ValueWrapper VisitTipoNil(LanguageParser.TipoNilContext context)
    {
        return defaultValue;
    }



    //VisitFuncStruct
    /*
        'func' '(' ID ID ')' ID '(' params? ')' tipos? '{' declaraciones* '}' # FuncStruct

        params: ID tipos? (',' ID tipos?)*
    */
    public override ValueWrapper VisitFuncStruct(LanguageParser.FuncStructContext context)
    {
        //System.Console.WriteLine("Segundo paso");
        // Nombre del struct al que pertenece el método
        string structName = context.ID(1).GetText();
        
        // Nombre del método dentro del struct
        string methodName = context.ID(0).GetText();

        // Nombre de la función
        string fucName = context.ID(2).GetText();

        // Verificar si el struct está definido en el entorno
        ValueWrapper structValue = currentEnvironment.GetVariable(structName, context.Start);
        //System.Console.WriteLine($"Miguel {structValue is StructValue}");
        if (structValue is not StructValue structDef)
        {
            throw new SemanticError($"El tipo '{structName}' no está definido como struct.", context.Start);
        }
        
        // Crear la función como un método del struct
        ForaneaFuncion metodoForaneo = new ForaneaFuncion(currentEnvironment, context);
        // Agregar el método al struct
        structDef.languageStruct.AgregarMetodo(fucName,  metodoForaneo);

        //Aca se debe de asociar la funcion con el 
        //var structInstance = new Instance(structDef.languageStruct); 
        //var structInstance = structDef.languageStruct.GetInstance();

        //var metodoForaneoConInstancia = metodoForaneo.Bind(structInstance, methodName);
        
        // Guardamos la referencia del método sin instanciarlo todavía
        //FunctionValue metodo = new FunctionValue(structDef.languageStruct, fucName);

        return defaultValue;
    }


}





/*
    List<List<int>> matrizLista = new List<List<int>>()
    {
        new List<int> { 1, 2, 3 },
        new List<int> { 4, 5, 6 },
        new List<int> { 7, 8, 9 }
    };

    // Acceder al elemento en la segunda fila, tercera columna
    Console.WriteLine(matrizLista[1][2]); // Imprime "6"

    // Modificar un valor
    matrizLista[0][1] = 99;
*/