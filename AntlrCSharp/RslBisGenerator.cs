using System.Diagnostics.Contracts;
using Antlr4.Runtime.Misc;

public class RslBisGenerator : RslBisBaseVisitor<TranslationIntermediary> {
    public override TranslationIntermediary VisitStart([NotNull] RslBisParser.StartContext context)
    {
        object childResult = base.VisitStart(context);
        return null; // TODO - generate code!!
    }
}