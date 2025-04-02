public static class TypeChecker
{
    // Obtiene valores por defecto según el tipo
    public static ValueWrapper GetDefaultValue(string tipo)
    {
        return tipo switch
        {
            "int" => new IntValue(0),
            "float64" => new FloatValue(0.0f),
            "bool" => new BoolValue(false),
            "string" => new StringValue(""),
            "rune" => new CharValue('\0'), // Usamos '\0' como valor por defecto
            _ => throw new Exception($"Tipo desconocido: {tipo}")
        };
    }

    // Verifica la compatibilidad de tiposs
    public static bool CheckTypeCompatibility(string tipo, ValueWrapper value)
    {
        /*if (tipo == "float64" && value is IntValue){
            return true;
        }*/
        return tipo switch
        {
            "int" => value is IntValue,
            "float64" => value is FloatValue,
            "bool" => value is BoolValue,
            "string" => value is StringValue,
            "rune" => value is CharValue, // Validamos `rune`
            _ => false
        };
    }
    
}
