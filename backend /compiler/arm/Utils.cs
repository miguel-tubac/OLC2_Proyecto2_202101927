//Esto descompone un string en bytes es decir en cada caracter y agrega el valor /0
public static class Utils
{
    public static List<byte> StringTo1ByteArray(string str)
    {
        var resultado = new List<byte>();
        int elementIndex = 0;

        while (elementIndex < str.Length)
        {
            resultado.Add((byte)str[elementIndex]);
            elementIndex++;
        }
        resultado.Add(0);//Agrega el caracter null de terminacion de la cadena

        return resultado;
    }

    public static string ObtenertDestino(string rd)
    {
        string destino = "";
        switch (rd){
            case "x0":
                destino = Register.D0;
                break;
            case "x1":
                destino = Register.D1;
                break;
            case "x2":
                destino = Register.D2;
                break;
            case "x3":
                destino = Register.D3;
                break;
            case "x4":
                destino = Register.D4;
                break;
            case "x5":
                destino = Register.D5;
                break;
            case "x6":
                destino = Register.D6;
                break;
            case "x7":
                destino = Register.D7;
                break;
            case "x8":
                destino = Register.D8;
                break;
            case "x9":
                destino = Register.D9;
                break;
            case "x10":
                destino = Register.D10;
                break;
            case "x11":
                destino = Register.D11;
                break;
            case "x12":
                destino = Register.D12;
                break;
            case "x13":
                destino = Register.D13;
                break;
            case "x14":
                destino = Register.D14;
                break;
            case "x15":
                destino = Register.D15;
                break;
            //TODO: solo llege hasta el 15, estan pendientes hasta el 31
        }

        return destino;
    }
}


public class CasoSwitch
{
    public string Label { get; set; }
    public analyzer.LanguageParser.ExprContext Condicion { get; set; }
    public analyzer.LanguageParser.DeclaracionesContext[] Declaraciones { get; set; }
}
