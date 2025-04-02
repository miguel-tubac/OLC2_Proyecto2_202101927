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
        return null;
    }


    //Esta corresponde a la produccion: 'var' ID tipos ('=' expr)? ';' # PrimeraDecl 
    public override Object VisitPrimeraDecl(LanguageParser.PrimeraDeclContext context)
    {
        return null;
    }

    //Esta es la declaracion explicita: ID ':=' expr ';' # SegundaDecl
    public override Object VisitSegundaDecl(LanguageParser.SegundaDeclContext context)
    {
        return null;
    }

    public override Object VisitNegar([NotNull] LanguageParser.NegarContext context)
    {
        return null;
    }

    //  VisitExpreStmt
    public override Object  VisitExpreStmt(LanguageParser.ExpreStmtContext context)
    {
        return null;
    }

    //VisitPrinStmt
    public override Object VisitPrinStmt(LanguageParser.PrinStmtContext context)
    {
        c.Comment("Print statement");
        foreach(var val in context.expr())
        {
            Visit(val);
            c.Pop(Register.X0);
            //Llamamos a la funcion de imprimir
            c.PrintInteger(Register.X0);
        }
        return null;
    }


    //VisitIdentifaider    ID                                        # Identifaider
    public override Object VisitIdentifaider(LanguageParser.IdentifaiderContext context)
    {
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
        c.Pop(Register.X1); //Pop 2: TOP --> [1]
        c.Pop(Register.X0); //Pop 1: TOP --> []

        if (operation == "+"){
            c.Add(Register.X0, Register.X0, Register.X1);
        }
        else if (operation == "-"){
            c.Sub(Register.X0, Register.X0, Register.X1);
        }

        //Ahora se vuelve a cargar al stack
        c.Push(Register.X0);
        return null;
    }

    //---------------Aca se iran agregando las nuevas visitas--------------------------------------
     // VisitInt
    public override Object VisitInt(LanguageParser.IntContext context)
    {
        var value = context.INT().GetText();
        c.Comment("Constante: "+value);
        c.Mov(Register.X0, int.Parse(value));
        c.Push(Register.X0);
        return null;
    }
    
    // VisitFloat
    public override Object VisitFloat(LanguageParser.FloatContext context)
    {   
        return null;
    }

    // VisitString
    public override Object VisitString(LanguageParser.StringContext context)
    {
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
