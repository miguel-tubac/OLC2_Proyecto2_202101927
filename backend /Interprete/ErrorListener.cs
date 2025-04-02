using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public class SemanticError : Exception
{
    public string message;
    public Antlr4.Runtime.IToken token;

    public SemanticError(string message, Antlr4.Runtime.IToken token){
        this.message = message;
        this.token = token;
    }

    public override string Message
    {
        get 
        {
            if (token == null)
            {
                return message;
            }
            //return new ErrorEntry(message, token.Line, token.Column, "Error Semantico");
            return message + " en línea " + token.Line + ", columna " + token.Column;
        }
    }

}


//Todo: Aca se debe de manejar una tabla con los errores para posteriormente mostrarlo
//Esto es para el errores Lexicos
public class LexicalErrorListener : BaseErrorListener, IAntlrErrorListener<int>
{
    public List<ErrorEntry> Errors { get; } = new List<ErrorEntry>();
    public List<ErrorEntry> GetErrors()
    {
        return Errors;
    }

    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
    {
        Errors.Add(new ErrorEntry(msg, line, charPositionInLine, "Error léxico"));
        //System.Console.WriteLine(Errors.Count);
        throw new ParseCanceledException($"Error léxico en línea {line}:{charPositionInLine} - {msg}");
    }
}

// Clase para manejar errores sintácticos
public class SyntaxErrorListener : BaseErrorListener
{
    public List<ErrorEntry> Errors { get; } = new List<ErrorEntry>();

    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
    {
        Errors.Add(new ErrorEntry(msg, line, charPositionInLine, "Error sintáctico"));
        //System.Console.WriteLine(Errors.Count);
        throw new ParseCanceledException($"Error sintaxis en línea {line}:{charPositionInLine} - {msg}");
    }
}







//Respando si algo falla

/*public class LexicalErrorListener : BaseErrorListener, IAntlrErrorListener<int>
{
    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine,string msg, RecognitionException e)
    {
        Console.WriteLine($"Error léxico en linea {line}, columna {charPositionInLine}: {msg}");
        throw new ParseCanceledException($"Error léxico en línea {line}:{charPositionInLine} - {msg}");
    }
}

// Esto es para los errores sintacticos
public class SyntaxErrorListener : BaseErrorListener
{
    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
    {
        Console.WriteLine($"Error de sintaxis en linea {line}, columna {charPositionInLine}: {msg}");
        throw new ParseCanceledException($"Error sintaxis en línea {line}:{charPositionInLine} - {msg}");
    }
}*/
