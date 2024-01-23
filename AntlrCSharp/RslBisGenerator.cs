using System.Diagnostics.Contracts;
using Antlr4.Runtime.Misc;

public class RslBisGenerator : RslBisBaseVisitor<object> {
    public override object VisitStart([NotNull] RslBisParser.StartContext context)
    {
        object childResult = base.VisitStart(context);
        return null; // TODO - generate code!!
    }
}