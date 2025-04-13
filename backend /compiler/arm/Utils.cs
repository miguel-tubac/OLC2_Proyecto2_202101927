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
}