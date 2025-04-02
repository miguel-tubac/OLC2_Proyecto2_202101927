// Excepcion del break

public class BreakException : Exception
{
    public BreakException() : base("La sentencia del Break unicamante puede estar dentro de un bucle o switch")
    {
    }
}

//Execepcion del continue
public class ContinueException : Exception
{
    public ContinueException() : base("La sentencia del Continue unicamente puede estar dentro de un bucle")
    {
    }
}

//Excepcion del return
public class ReturnException : Exception
{
    public ValueWrapper Value {get;}

    public ReturnException(ValueWrapper value) : base("La sentencia Return unicamente puede colocarse dentro de funciones")
    {
        Value = value;
    }
}