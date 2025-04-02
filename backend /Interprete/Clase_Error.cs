

// Estructura para almacenar errores
public class ErrorEntry
{
    public string Descripcion { get; set; }
    public int Linea { get; set; }
    public int Columna { get; set; }
    public string Tipo { get; set; }

    public ErrorEntry(string descripcion, int linea, int columna, string tipo)
    {
        Descripcion = descripcion;
        Linea = linea;
        Columna = columna;
        Tipo = tipo;
    }

    public override string ToString()
    {
        return $"{Tipo} en línea {Linea}, columna {Columna}: {Descripcion}";
    }
}
