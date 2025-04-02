public class LanguageStruct : Invocable
{
    public string Name { get; set; }  // Nombre del struct
    public Dictionary<string, DatosStruct> Props { get; set; } // Atributos del struct
    public Dictionary<string, ForaneaFuncion> Metodos { get; set; } // Métodos del struct

    public Instance instance = new Instance(null);  // Instancia del struct

    public LanguageStruct(string name, Dictionary<string, DatosStruct> props)
    {
        Name = name;
        Props = props;
        Metodos = new Dictionary<string, ForaneaFuncion>();
    }

    public DatosStruct? GetAtributo(string name)
    {
        return Props.ContainsKey(name) ? Props[name] : null;
    }

    public ForaneaFuncion? GetMetodo(string name)
    {
        return Metodos.ContainsKey(name) ? Metodos[name] : null;
    }

    public void AgregarMetodo(string nombre, ForaneaFuncion metodo)
    {
        if (Metodos.ContainsKey(nombre))
        {
            throw new SemanticError($"El método '{nombre}' ya está definido en el struct '{Name}'.",null);
        }
        Metodos[nombre] = metodo;
    }

    public int Arity() => Props.Count;


    // Método para establecer la instancia del struct
    public void SetInstance(Instance newInstance)
    {
        instance = newInstance;
    }

    // Método para obtener la instancia del struct
    public Instance GetInstance()
    {
        return instance;
    }

    public ValueWrapper Invoke(List<ValueWrapper> args, InterpreterVisitor visitor)
    {
        var newInstance = new Instance(this);

        /*foreach (var prop in Props){
            var name = prop.Key;
            var value = prop.Value;

            if(value)
        }*/
        
        // Asignar los valores de los atributos a la instancia
        int i = 0;
        foreach (var atributo in Props)
        {
            var atributoName = atributo.Key;
            var tipo = atributo.Value.tipo;
            var valor = args[i];

            // Asignar el valor al atributo de la nueva instancia
            //System.Console.WriteLine($"valor {atributoName} = {valor}");
            newInstance.Set(atributoName, valor);
            i++;
            //newInstance.Set(atributoName, visitor.defaultValue);
        }
        // Establecer la instancia creada
        instance = newInstance;
        //System.Console.WriteLine($"Miguel {newInstance}");
        return new InstanceValue(newInstance);
    }
}
