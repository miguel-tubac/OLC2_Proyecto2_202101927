

// Estructura para almacenar errores
public class SimboloEntry
{
    public string ID { get; set; }
    public string Tipo_simbol { get; set; }
    public string Tipo_dato { get; set; }
    public string Ambito { get; set; }
    public int Linea { get; set; }
    public int Columna { get; set; }
    

    public SimboloEntry(string id, string tipo_simbolo, string tipo_dato, string ambito, int linea, int columna)
    {
        ID = id;
        Tipo_simbol = tipo_simbolo;
        Tipo_dato = tipo_dato;
        Ambito = ambito;
        Linea = linea;
        Columna = columna;
    }
}
