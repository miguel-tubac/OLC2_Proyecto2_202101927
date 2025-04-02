public class Embedidas
{
    public static void Generate(Environment env)
    {
        //Aca se declara la llamada 
        env.DeclararVariable("time", new FunctionValue(new TimeEmbeded(), "time"), null);
    }
}

//Aca van a estar las funciones embedidas
public class TimeEmbeded : Invocable
{
    public int Arity()
    {
        return 0;
    }

    public ValueWrapper Invoke(List<ValueWrapper> arg, InterpreterVisitor visitor){
        return new StringValue(DateTime.Now.ToString());
    }
}