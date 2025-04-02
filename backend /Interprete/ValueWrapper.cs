
//Esto guarda cualquer tipo de dato (es mutable)
public abstract record ValueWrapper;

//Se definen los tipos de datos
public record IntValue (int Value) : ValueWrapper
{
    public override string ToString()
    {
        return "int";
    }
}
public record FloatValue (float Value) : ValueWrapper
{
    public override string ToString()
    {
        return "float64";
    }
}
public record StringValue (string Value) : ValueWrapper
{
    public override string ToString()
    {
        return "string";
    }
}
public record BoolValue (bool Value) : ValueWrapper
{
    public override string ToString()
    {
        return "bool";
    }
}
public record CharValue(char Value) : ValueWrapper
{
    public override string ToString()
    {
        return "rune";
    }
}

//Para funciones que no retornan nada
public record VoidValue : ValueWrapper
{
    public override string ToString()
    {
        return "nil";
    }
}


// Representa un resultado de un `case` en un switch
public record CaseResult(ValueWrapper CaseValue, analyzer.LanguageParser.DeclaracionesContext[] ExecuteBlock) : ValueWrapper;

// Representa los valores slice
public record Slices(List<ValueWrapper> datos, ValueWrapper tipo) : ValueWrapper
{
    public List<ValueWrapper> datos { get; set; } = datos;
}

public record FunctionValue(Invocable invocable, string name) :ValueWrapper;

public record MatrixValue(string Name, string Type, List<List<ValueWrapper>> Values) : ValueWrapper;


//Este sera usado para los structs
public record InstanceValue(Instance Instance) : ValueWrapper
{
    public override string ToString() => Instance.ToString();
}

public record StructValue(LanguageStruct languageStruct) : ValueWrapper
{
    public override string ToString()
    {
        return $"Struct {languageStruct.Name} {{ {string.Join(", ", languageStruct.Props.Keys)} }}";
    }
}
