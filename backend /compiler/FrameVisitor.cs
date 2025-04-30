using analyzer;

public class FrameElement
{
    public string Name {get; set;}
    public int Offset {get; set;}

    public FrameElement(string name, int offset)
    {
        Name = name;
        Offset = offset;
    }
}

public class FrameVisitor : LanguageBaseVisitor<Object>
{
    public List<FrameElement> Frame;
    public int LocalOffset;
    public int BaseOffset;

    public FrameVisitor(int baseOffset)
    {
        Frame = new List<FrameElement>();
        LocalOffset = 0;
        BaseOffset = baseOffset;
    }

    public override Object VisitPrimeraDecl(LanguageParser.PrimeraDeclContext context)
    {
        string name = context.ID().GetText();

        Frame.Add(new FrameElement(name, BaseOffset + LocalOffset));
        LocalOffset +=1;

        return null;
    }

    public override Object VisitSegundaDecl(LanguageParser.SegundaDeclContext context)
    {
        string name = context.ID().GetText();

        Frame.Add(new FrameElement(name, BaseOffset + LocalOffset));
        LocalOffset +=1;

        return null;
    }

    public override Object VisitBloqueSente(LanguageParser.BloqueSenteContext context)
    {
        foreach (var dcl in context.declaraciones())
        {
            Visit(dcl);
        }
        return null;
    }

    public override Object VisitIfstat(LanguageParser.IfstatContext context)
    {
        Visit(context.stmt(0));
        if (context.stmt().Length > 1) Visit(context.stmt(1));
        return null;
    }

    public override Object VisitWhileStmt(LanguageParser.WhileStmtContext context)
    {
        Visit(context.stmt());
        return null;
    }

    public override Object VisitForStmt(LanguageParser.ForStmtContext context)
    {
        if (context.forInit().declararvar() != null){
            Visit(context.forInit().declararvar());
        }
        Visit(context.stmt());
        return null;
    }

    public override Object VisitPrinStmt(LanguageParser.PrinStmtContext context)
    {
        foreach(var val in context.expr())
        {
            Visit(val);
        }
        return null;
    }
}