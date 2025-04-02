using System.Collections.Generic;
using analyzer;
using Antlr4.Runtime.Tree;

public class SearchVisitor : LanguageBaseVisitor<object>
{
    public List<LanguageParser.FuncDclContext> Funciones { get; } = new();
    public List<LanguageParser.DeclaracionesContext> Declaraciones { get; } = new();
    public List<LanguageParser.DeclaracionesContext> MainBody { get; } = new();

    /*
        funcDcl: 'func' ID '(' params? ')' tipos? '{' declaraciones* '}'
        ;

        params: ID tipos (',' ID tipos)*
        ;
    */
    public override object VisitFuncDcl1(LanguageParser.FuncDcl1Context context)
    {
        string functionName = context.ID().GetText();
        //System.Console.WriteLine($"Encontrada función: {functionName}");

        if (functionName == "main")
        {
            //System.Console.WriteLine("⚡ ¡Entró en la función main!");
            foreach (var decl in context.declaraciones())
            {
                //System.Console.WriteLine($"📌 Agregando instrucción al main: {decl.GetText()}");
                MainBody.Add(decl);
            }
        }
        else
        {
            Funciones.Add(context);
        }

        return null;
    }

    public override object VisitFuncStruct(LanguageParser.FuncStructContext context)
    {
        
        Funciones.Add(context);
        return null;
    }

    public override object VisitDeclaraciones(LanguageParser.DeclaracionesContext context)
    {
        //System.Console.WriteLine("📢 Visitando declaración...");

        if (context.funcDcl() != null)
        {
            Visit(context.funcDcl());  // Asegurar que se visiten las funciones dentro de declaraciones
            return null;
        }

        Declaraciones.Add(context);
        return null;
    }
}
