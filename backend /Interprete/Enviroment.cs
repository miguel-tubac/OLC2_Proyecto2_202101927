public class Environment
{

    public Dictionary<string, ValueWrapper> variables = new Dictionary<string, ValueWrapper>();

    private Environment? parent;

    //Esto es para los entornos apilados
    public Environment(Environment? parent)
    {
        this.parent = parent;
    }

    // Esta funcion cambio a Get()
    public ValueWrapper GetVariable(string id, Antlr4.Runtime.IToken token)
    {
        if (variables.ContainsKey(id)){
            return variables[id];
        }
        if (parent != null){
            return parent.GetVariable(id, token);
        }
        throw new SemanticError("Variable \"" + id + "\" not found", token);
    }

    // Esta funcion cambio a Declare()
    public void DeclararVariable(string id, ValueWrapper value, Antlr4.Runtime.IToken? token)
    {
        if (variables.ContainsKey(id)){
            if(token != null) throw new SemanticError("Variable "+id+" ya fue declarada", token);
        }else{
            variables[id] = value;
        }
    }

    // Esta funcion cambio a Assign()
    public ValueWrapper AsignarVariable(string id, ValueWrapper value, Antlr4.Runtime.IToken token){
        if (variables.ContainsKey(id)){
            variables[id] = value;
            return value; //Reronamos el valor para trabajar con el en el futuro
        }

        if (parent != null){
            return parent.AsignarVariable(id, value, token);
        }

        throw new SemanticError("Variable "+id+" no esta declarada", token);
    }

}