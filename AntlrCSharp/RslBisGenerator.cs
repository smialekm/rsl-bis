using System.Diagnostics.Contracts;
using Antlr4.Runtime.Misc;
using System;
using System.Collections.Generic;
using CodeModel;
using System.Linq;
using System.Reflection.Metadata;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

public class RslBisGenerator : RslBisBaseVisitor<IntermediaryRepresentation> {
    private IntermediaryRepresentation result = new IntermediaryRepresentation();
    // Auxiliary structures
    UseCaseClass CurrentUCC {get; set;} = null;
    ViewFunction CurrentViewFunction {get; set;} = null;
    UCOperation CurrentUCO {get; set;} = null;
    Condition CurrentCondition {get; set;} = null;
    COperation ConditionCO {get; set;} = null;
    List<DataAggregate> InheritedDAD {get; set;} = new List<DataAggregate>();
    List<DataAggregate> CurrentDAP {get; set;} = new List<DataAggregate>();
    List<DataAggregate> CurrentDAD {get; set;} = new List<DataAggregate>();
    string CurrentLabel {get; set;} = null;
    PredicateType LastPredicateType {get; set;} = PredicateType.None;
    PredicateType LastNonInvokePT {get; set;} = PredicateType.None;
    Boolean StartOfAlternativeScenario {get; set;} = false;
    Dictionary<string,ViewFunction> LabelToViewFunction {get; set;} = new Dictionary<string, ViewFunction>();
    Dictionary<string,Trigger> UcNameToTrigger {get; set;} = new Dictionary<string, Trigger>();
    Dictionary<string,ViewFunction> UcNameToViewFunction {get; set;} = new Dictionary<string, ViewFunction>();

    public RslBisGenerator(){
        
    }

    private static string ObtainName(ParserRuleContext context){
        ITerminalNode[] strings;
        if (context is RslBisParser.NameContext nameContext) strings = nameContext.STRING();
        else if (context is RslBisParser.NotionContext notionContext) strings = notionContext.STRING();
        else if (context is RslBisParser.ValueContext valueContext) strings = valueContext.STRING();
        else return null;     
        return string.Join(" ", strings.ToList().Select(x => x.GetText()));
    }

    public override IntermediaryRepresentation VisitStart([NotNull] RslBisParser.StartContext context)
    {
        Console.WriteLine("Starting processing RSL specification");
        object childResult = base.VisitStart(context);
        return result;
    }

    public override IntermediaryRepresentation VisitUsecase([NotNull] RslBisParser.UsecaseContext context){
        // 1. Create ‘UseCaseClass’ based on ‘name’ and set as ‘CurrentUCC’
        string name = ObtainName(context.name());
        Console.WriteLine("Parsing Use Case: {0}", name);
        UseCaseClass ucC = new UseCaseClass(){name = name};
        result.UseCaseClasses.Add(ucC);
        CurrentUCC = ucC;
        object childResult = base.VisitUsecase(context);
        return result;
    }

    public override IntermediaryRepresentation VisitUcconditions([NotNull] RslBisParser.UcconditionsContext context)
    {
        Console.WriteLine("Precondition sentence");
        ProcessConditions(context.conditions());
        // TODO (remove) object childResult = base.VisitUcconditions(context);
        return result;
    }

    private void ProcessConditions(RslBisParser.ConditionsContext context)
    {
        if (null == context) return;
        RslBisParser.ContextconditionContext ccondition = context.condition().contextcondition();
        // 1. For each ‘condition’ create ‘DataAggregate’ (if does not exist) based on ‘notion’
        //    add it to ‘ViewModel’
        DataAggregate da = new DataAggregate();
        result.ViewModel.items.Add(da);
        if (null != ccondition) {
            // 2. if ‘condition’ is ‘contextcondition’ -> add ‘DataAggregate’ to ‘InheritedDAD’
            da.name = ObtainName(ccondition.notion());
            InheritedDAD.Add(da);
        } else {
            RslBisParser.ValueconditionContext vcondition = context.condition().valuecondition();
            da.name = ObtainName(vcondition.notion());
            // 3. If any ‘valuecondition’ exists -> create ‘COperation’; set it as ‘ConditionCO’;
            //    create ‘UCOperation’; add it to ‘CurrentUCC’; attach it to ‘COperation’;
            //    set ‘UCOperation.returnType’ as ‘boolean’
            if (null == ConditionCO) {
                UCOperation ucop = new UCOperation(){returnType = "boolean"};
                CurrentUCC.methods.Add(ucop);
                COperation cop = new COperation(){name = CurrentUCC.name, invoked = ucop};
                ConditionCO = cop;
            }
            // 4. For each ‘valuecondition’ -> Algorithm for value condition
            ProcessValueCondition(vcondition);
        }
        ProcessConditions(context.conditions());
    }

    private void ProcessValueCondition(RslBisParser.ValueconditionContext context){
        string notionName = ObtainName(context.notion());
        // 1. Create ‘DataItem’ (‘parameter’; type as ‘notion’); add ‘DataItem’ to ‘ConditionCO.invoked’ ('UCOperation')
        DataItem di = new DataItem() {type = notionName};
        ConditionCO.invoked.parameters.Add(di);
        // 2. Create ‘ServiceInterface’ (if does not exist) based on ‘notion’; attach it to ‘CurrentUCC’
        ServiceInterface si = result.ServiceInterfaces.Find(x => notionName == x.name);
        SOperation sop = si?.signatures.Find(x => "check " + notionName == x.name);
        if (null == si) {
            si = new ServiceInterface(){name = notionName};
            result.ServiceInterfaces.Add(si);
        }
        // 3. Create ‘Enumeration’ (it does not exist) based on ‘notion’; add it to ‘ViewModel’
        CheckEnumeration en = result.ViewModel.enums.Find(x => notionName == x.name);
        string valueName = ObtainName(context.value());
        Value value = en?.values.Find(x => valueName == x.name);
        if (null == en) {
            en = new CheckEnumeration(){name = notionName + " !enum"};
            result.ViewModel.enums.Add(en);
        }
        // 4. Create ‘Value’ based on ‘value’; add it to ‘Enumeration’
        if (null == value) {
            value = new Value(){name = valueName};
            en.values.Add(value);
        }
        // 5. Create ‘SOperation’ (if does not exist); add ‘DataItem’ to ‘SOperation’; add ‘SOperation’ to 'ServiceInterface'
        // set ‘SOperation.returnType’ to ‘Enumeration.name’
        if (null == sop) {
            sop = new SOperation(){name = "check! " + notionName, returnType = en.name};
            sop.parameters.Add(di);
            si.signatures.Add(sop);
        }
        CurrentUCC.services.Add(si);
        // 6. Create ‘Call’; append it to ‘UCOperation’; attach ‘SOperation’ to ‘Call’
        Call call = new Call(){operation = sop};
        ConditionCO.invoked.instructions.Add(call);
    }

    public override IntermediaryRepresentation VisitSvosentence([NotNull] RslBisParser.SvosentenceContext context)
    {
        CurrentLabel = context.label().ToString();
        return base.VisitSvosentence(context);
    }

    public override IntermediaryRepresentation VisitAltsvosentence([NotNull] RslBisParser.AltsvosentenceContext context)
    {
        CurrentLabel = context.altlabel().ToString();
        return base.VisitAltsvosentence(context);
    }

    public override IntermediaryRepresentation VisitRepsentence([NotNull] RslBisParser.RepsentenceContext context)
    {
        CurrentLabel = null != context.label() ? context.label().ToString() : context.altlabel().ToString();
        return base.VisitRepsentence(context);
    }
}