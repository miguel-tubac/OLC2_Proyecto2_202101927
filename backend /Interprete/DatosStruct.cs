public class DatosStruct
{
    public string tipo {get;set;}
    public string id {get;set;}

    public DatosStruct(string tipo, string id)
    {
        this.tipo = tipo;
        this.id = id;
    }
}