using System.Diagnostics.Contracts;
using Antlr4.Runtime.Misc;
using System;
using System.Collections.Generic;
using CodeModel;
using System.Linq;

public class RslBisGenerator : RslBisBaseVisitor<IntermediaryRepresentation> {
    private IntermediaryRepresentation result = new IntermediaryRepresentation();
    // Auxiliary structures
    UseCaseClass CurrentUCC {get; set;}
    ViewFunction CurrentViewFunction {get; set;}
    UCOperation CurrentUCO {get; set;}
    Condition CurrentCondition {get; set;}
    COperation ConditionCO {get; set;}
    List<DataAggregate> InheritedDAD {get; set;}
    List<DataAggregate> CurrentDAP {get; set;}
    List<DataAggregate> CurrentDAD {get; set;}
    string CurrentLabel {get; set;}
    PredicateType LastPredicateType {get; set;} 
    PredicateType LastNonInvokePT {get; set;}
    Boolean StartOfAlternativeScenario {get; set;}
    Dictionary<string,ViewFunction> LabelToViewFunction {get; set;}
    Dictionary<string,Trigger> UcNameToTrigger {get; set;}
    Dictionary<string,ViewFunction> UcNameToViewFunction {get; set;}

    public RslBisGenerator(){
        
    }

    public override IntermediaryRepresentation VisitStart([NotNull] RslBisParser.StartContext context)
    {
        Console.WriteLine("Starting processing RSL specification");
        object childResult = base.VisitStart(context);
        return result;
    }

    public override IntermediaryRepresentation VisitUsecase([NotNull] RslBisParser.UsecaseContext context){
        string name = string.Join(" ", context.name().STRING().ToList().Select(x=>x.GetText()));
        Console.WriteLine("Parsing Use Case: {0}", name);
        UseCaseClass ucC = new UseCaseClass(){name = name};
        result.UseCaseClasses.Add(ucC);
        CurrentUCC = ucC;
        return result;
    }
    
}