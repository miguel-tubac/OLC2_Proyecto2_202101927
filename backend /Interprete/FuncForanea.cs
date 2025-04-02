using analyzer;

public class ForaneaFuncion : Invocable
{
    private Environment clousure;
    private LanguageParser.FuncDcl1Context? normalContext;
    private LanguageParser.FuncStructContext? structContext;

    // Constructor para funciones normales
    public ForaneaFuncion(Environment clousure, LanguageParser.FuncDcl1Context context)
    {
        this.clousure = clousure;
        this.normalContext = context;
    }

    // Constructor para métodos de struct
    public ForaneaFuncion(Environment clousure, LanguageParser.FuncStructContext context)
    {
        this.clousure = clousure;
        this.structContext = context;
    }

    // Determinar el número de parámetros
    public int Arity()
    {
        if (normalContext != null)
        {
            return normalContext.@params() == null ? 0 : normalContext.@params().ID().Length;
        }
        else if (structContext != null)
        {
            return structContext.@params() == null ? 0 : structContext.@params().ID().Length; // +1 porque incluye el "this"
        }
        return 0;
    }

    public ValueWrapper Invoke(List<ValueWrapper> args, InterpreterVisitor visitor)
    {
        Environment anteriorEntorno = visitor.currentEnvironment;
        Environment newEnv = new Environment(clousure);
        visitor.currentEnvironment = newEnv;

        try
        {
            if (normalContext != null)
            {
                // Manejo de funciones normales
                if (normalContext.@params() != null)
                {
                    for (int i = 0; i < normalContext.@params().ID().Length; i++)
                    {
                        newEnv.DeclararVariable(normalContext.@params().ID(i).GetText(), args[i], null);
                    }
                }

                foreach (var stm in normalContext.declaraciones())
                {
                    visitor.Visit(stm);
                }
            }
            else if (structContext != null)
            {
                //VisitFuncStruct
                /*
                    'func' '(' ID ID ')' ID '(' params? ')' tipos? '{' declaraciones* '}' # FuncStruct

                    params: ID tipos? (',' ID tipos?)*
                */

                //Primero delcarar el ID(0) como el struct del ID(0)
                StructValue estrutura = (StructValue)anteriorEntorno.GetVariable(structContext.ID(1).GetText(),null);
                var instancia = estrutura.languageStruct.GetInstance();

                newEnv.DeclararVariable(structContext.ID(0).GetText(), new InstanceValue(instancia), null);
                System.Console.WriteLine($"ID: {structContext.ID(0).GetText()}, instancia: {instancia}");

                System.Console.WriteLine("*******Declaracion********");
                // Registrar los parámetros del método (si los hay)
                if (structContext.@params() != null)
                {
                    //System.Console.WriteLine(structContext.@params().ID().Length);
                    for (int i = 0; i < structContext.@params().ID().Length; i++)
                    {
                        newEnv.DeclararVariable(structContext.@params().ID(i).GetText(), args[i], null);
                    }
                }
                
                System.Console.WriteLine("*******Visitando********");
                // Ejecutar las declaraciones del método
                foreach (var stm in structContext.declaraciones())
                {
                    visitor.Visit(stm);
                }
            }
        }
        catch (ReturnException e)
        {
            visitor.currentEnvironment = anteriorEntorno;
            return e.Value;
        }

        visitor.currentEnvironment = anteriorEntorno;
        return visitor.defaultValue;
    }


    // Para métodos de struct, asocia la instancia del struct al método
    /*public ForaneaFuncion Bind(Instance instance, string methodName)
    {
        var hiddenEnv = new Environment(clousure);
        // Dynamically decide the variable name based on method name (like 'p', or any other custom logic)
        string variableName = methodName; // You can decide dynamically if you want to use different names for different methods
        
        hiddenEnv.DeclararVariable(variableName, new InstanceValue(instance), null);
        
        return normalContext != null ? new ForaneaFuncion(hiddenEnv, normalContext) : new ForaneaFuncion(hiddenEnv, structContext!);
    }*/

    public ForaneaFuncion Bind(Instance instance, string methodName)
    {
        //var hiddenEnv = new Environment(clousure);

        // Dynamically decide the variable name based on method name (like 'p', or any other custom logic)
        string variableName = methodName; // You can decide dynamically if you want to use different names for different methods
        
        clousure.DeclararVariable(variableName, new InstanceValue(instance), null);
        
        return normalContext != null 
            ? new ForaneaFuncion(clousure, normalContext) 
            : new ForaneaFuncion(clousure, structContext!);
    }

}




