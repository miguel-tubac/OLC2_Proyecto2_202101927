public interface Invocable
{
    int Arity();//Esto representa el numero de parametros que necesita la funcion

    ValueWrapper Invoke(List<ValueWrapper> args, InterpreterVisitor visitor);
}