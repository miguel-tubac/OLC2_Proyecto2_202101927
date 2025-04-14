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
        var varname = context.ID().GetText();
        c.Comment("Variable declaracion: "+varname);

        //Aca se valida que el valor este en la pila
        Visit(context.expr());
        //Aca se asocia el valor al nombre en el stack virtual
        c.TagObject(varname);

        return null;
    }

    //Esta es la declaracion explicita: ID ':=' expr ';' # SegundaDecl
    public override Object VisitSegundaDecl(LanguageParser.SegundaDeclContext context)
    {
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
        return null;
    }

    public override Object VisitMulDiv(LanguageParser.MulDivContext context)
    {
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
        var right = c.PopObject(Register.X1);
        var left = c.PopObject(Register.X0);

        //TODO: aca se manejan los tipos
        switch (right.Type, operation, left.Type){

        }

        if (operation == "+"){
            c.Add(Register.X0, Register.X0, Register.X1);
        }
        else if (operation == "-"){
            c.Sub(Register.X0, Register.X0, Register.X1);
        }

        //Ahora se vuelve a cargar al stack
        c.Comment("Pushing resultados");
        //Esto es a nievel de arm
        c.Push(Register.X0);
        //Esto es a nivel virtual, y se clona el objeto y se tiene que clonar el objeto que tiene predominacia en el tipo
        //En este caso se clona el left
        c.PushObject(c.CloneObject(left));
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

        //Se obtiene un objeto por defecto de tipo Flotante
        var floatObject = c.FloatObject();
        c.PushConstant(floatObject, value);
        //Esto convierte el valor a punto flotante
        //c.PushConstant(floatObject, float.Parse(value, CultureInfo.InvariantCulture));
        c.Comment("Final de la lectura del float");
        return null;
    }

    // VisitString
    public override Object VisitString(LanguageParser.StringContext context)
    {

        string rawText = context.STRING().GetText();
        string processedText = SecuanciasEscape.UnescapeString(rawText);//Procesa las secuancias de escape
        
        c.Comment("String constante: "+processedText);
        //Se generar un objeto de tipo string
        var stringObject = c.StringObject();
        //Se asocia el obejto con el strong
        c.PushConstant(stringObject, processedText);

        return null;
    }

    // VisitBoolean
    public override Object VisitBoolean(LanguageParser.BooleanContext context)
    {
        return null;
    }

    // VisitRune
    public override Object VisitRune(LanguageParser.RuneContext context)
    {
        return null;
    }


     // VisitRelacional
    public override Object VisitRelacional(LanguageParser.RelacionalContext context)
    {
        return null;
    }
    
    // VisitEqualitys
    public override Object VisitEqualitys(LanguageParser.EqualitysContext context)
    {
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
    //VisitAsig_Aumento
    public override Object VisitAsig_Aumento(LanguageParser.Asig_AumentoContext context)
    {   
        return null;
    }

    //VisitAsig_Decre
    public override Object VisitAsig_Decre(LanguageParser.Asig_DecreContext context)
    {
        return null;
    }

    //VisitAsigna
    public override Object VisitAsigna(LanguageParser.AsignaContext context){
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
