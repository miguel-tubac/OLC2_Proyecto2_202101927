public static class SecuanciasEscape
{
    public static string UnescapeString(string text)
    {
        return text.Substring(1, text.Length - 2) // Quita las comillas inicial y final
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
    }

    public static char UnescapeRune(string text)
    {
        string contenido = text.Substring(1, text.Length - 2); // Quita comillas simples

        return contenido switch
        {
            "\\n" => '\n',
            "\\r" => '\r',
            "\\t" => '\t',
            "\\'" => '\'',
            "\\\\" => '\\',
            _ => contenido[0] // Si no es escape, devuelve el carácter tal cual
        };
    }

}