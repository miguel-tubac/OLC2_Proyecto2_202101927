using System.Globalization;

public class Instance 
{
    public  LanguageStruct languageStruct { get; set;}
    public Dictionary<string, ValueWrapper> Properties { get; set;}

    // Constructor para inicializar con valores dados
    public Instance(LanguageStruct languageStruct)
    {
        this.languageStruct = languageStruct;
        Properties = new Dictionary<string, ValueWrapper>();
    }

    // Método para asignar valores a los atributos
    public void Set(string name, ValueWrapper value)
    {
        // Verificamos si el atributo existe en la definición del struct
        if (!languageStruct.Props.ContainsKey(name))
        {
            throw new SemanticError($"El struct '{languageStruct.Name}' no tiene un atributo llamado '{name}'.", null);
        }

        // Obtenemos el tipo esperado del atributo
        string tipoEsperado = languageStruct.Props[name].tipo;

        // Verificamos que el tipo del valor coincida con el tipo esperado
        if (!EsTipoCompatible(value, tipoEsperado))
        {
            throw new SemanticError($"El atributo MIg '{name}' de '{languageStruct.Name}' esperaba un valor de tipo '{tipoEsperado}', pero se recibió '{value.GetType().Name}'.", null);
        }

        //System.Console.WriteLine($"{name}  = {value}");
        Properties[name] = value;
    }

    // Método para obtener valores de los atributos o métodos
    public ValueWrapper Get(string name, Antlr4.Runtime.IToken token)
    {
        // Si la propiedad existe en la instancia, la retornamos
        if (Properties.ContainsKey(name))
        {
            return Properties[name];
        }

        var method = languageStruct.GetMetodo(name);
        // Verificar si el struct tiene un método con este nombre
        if (method != null)
        {
            // Retornar la función correspondiente
            //TODO: revisar si el nombre que se paso es correcto en el Bind
            return new FunctionValue(method.Bind(this, name),name);
            //return languageStruct.Metodos[name];
        }

        throw new SemanticError($"Propiedad o método '{name}' no encontrado en el struct '{languageStruct.Name}'.", token);
    }



    // Método para validar compatibilidad de tipos
    private bool EsTipoCompatible(ValueWrapper valor, string tipoEsperado)
    {

        if (valor is VoidValue)
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
            _ => false
        };
    }

    public override string ToString()
    {
        var atributos = Properties
            .Select(kv => $"{kv.Key}: {FormatValue(kv.Value)}")
            .ToArray();
        return $"{languageStruct.Name} {{ {string.Join(", ", atributos)} }}";
    }
    private static string FormatValue(ValueWrapper value)
    {
        return value switch
        {
            IntValue i => i.Value.ToString(),
            FloatValue f => f.Value.ToString(CultureInfo.InvariantCulture),
            StringValue s => $"\"{s.Value}\"",
            BoolValue b => b.Value.ToString().ToLower(),
            CharValue c => $"'{c.Value}'",
            VoidValue s => "nil",
            _ => "<unknown>"
        };
    }
}
