using System.Diagnostics.Contracts;
using Antlr4.Runtime.Misc;
using System;
using System.Collections.Generic;
using CodeModel;
using System.Linq;
using System.Reflection.Metadata;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using System.Runtime.Intrinsics.X86;

public class RslBisGenerator : RslBisBaseVisitor<IntermediaryRepresentation> {
    private IntermediaryRepresentation result = new IntermediaryRepresentation();
    // Auxiliary structures
    UseCaseClass CurrentUCC {get; set;} = null;
    ViewFunction CurrentVF {get; set;} = null;
    UCOperation CurrentUCO {get; set;} = null;
    Condition CurrentCondition {get; set;} = null;
    COperation ConditionCO {get; set;} = null;
    List<DataAggregate> InheritedDAD {get; set;} = new List<DataAggregate>();
    List<DataAggregate> CurrentDAP {get; set;} = new List<DataAggregate>();
    List<DataAggregate> CurrentDAD {get; set;} = new List<DataAggregate>();
    string CurrentLabel {get; set;} = null;
    PredicateType LastPredicateType {get; set;} = PredicateType.None;
    PredicateType LastNonInvokePT {get; set;} = PredicateType.None;
    Boolean StartOfAltScenario {get; set;} = false;
    Dictionary<string,ViewFunction> LabelToVF {get; set;} = new Dictionary<string, ViewFunction>();
    Dictionary<string,Trigger> UcNameToTrigger {get; set;} = new Dictionary<string, Trigger>();
    Dictionary<string,List<ViewFunction>> UcNameToVF {get; set;} = new Dictionary<string, List<ViewFunction>>();
    Dictionary<(string,string),UCOperation> UcVFToUCOperation {get; set;} = new Dictionary<(string, string), UCOperation>();

    // Configuration

    public bool Verbose {get; set;} = false;

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
        if (Verbose) Console.WriteLine("Starting processing RSL specification");
        object childResult = base.VisitStart(context);
        return result;
    }

    //*****************************************************************************************************
    // 1. USE CASE
    //*****************************************************************************************************  

    public override IntermediaryRepresentation VisitUsecase([NotNull] RslBisParser.UsecaseContext context){
        // 1. Create ‘UseCaseClass’ based on ‘name’ and set as ‘CurrentUCC’
        string name = ObtainName(context.name());
        if (Verbose) Console.WriteLine("Parsing Use Case: {0}", name);
        /*TMP UCOperation ucO = new UCOperation(){name = "op " + name}; */
        UseCaseClass ucC = new UseCaseClass(){name = name};
        /*TMP ucC.methods.Add(ucO); CurrentUCO = ucO; */
        result.UseCaseClasses.Add(ucC);
        CurrentUCC = ucC;
        object childResult = base.VisitUsecase(context);
        return result;
    }

    //*****************************************************************************************************
    // 2. PRECONDITION
    //*****************************************************************************************************  

    public override IntermediaryRepresentation VisitUcconditions([NotNull] RslBisParser.UcconditionsContext context)
    {
        if (Verbose) Console.WriteLine("Precondition sentence: " + context.GetText());
        ProcessConditions(context.conditions());
        // TODO (remove) object childResult = base.VisitUcconditions(context);
        return result;
    }

    private void ProcessConditions(RslBisParser.ConditionsContext context)
    {
        if (null == context) return;
        RslBisParser.ContextconditionContext ccondition = context.condition().contextcondition();
        RslBisParser.ValueconditionContext vcondition = context.condition().valuecondition();
        // 1. For each ‘condition’ create ‘DataAggregate’ (if does not exist) based on ‘notion’
        //    add it to ‘ViewModel’
        string notionName = null != ccondition ? ObtainName(ccondition.notion()) : ObtainName(vcondition.notion());
        DataAggregate da = result.ViewModel.items.Find(x => notionName == x.name);
        if (null == da) {
            da = new DataAggregate(){name = notionName};
            result.ViewModel.items.Add(da);
        }
        if (null != ccondition)
            // 2. if ‘condition’ is ‘contextcondition’ -> add ‘DataAggregate’ to ‘InheritedDAD’
            InheritedDAD.Add(da);
        else {
            // 3. If any ‘valuecondition’ exists -> create ‘COperation’; set it as ‘ConditionCO’;
            //    create ‘UCOperation’; add it to ‘CurrentUCC’; attach it to ‘COperation’;
            //    set ‘UCOperation.returnType’ as ‘boolean’
            if (null == ConditionCO) {
                UCOperation ucop = new UCOperation(){returnType = "boolean"};
                CurrentUCC.methods.Add(ucop);
                COperation cop = new COperation(){name = "invoke! " + CurrentUCC.name, invoked = ucop};
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
        SOperation sop = si?.signatures.Find(x => "check! " + notionName == x.name);
        if (null == si) {
            si = new ServiceInterface(){name = notionName};
            result.ServiceInterfaces.Add(si);
            CurrentUCC.services.Add(si);
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
        // 6. Create ‘Call’; append it to ‘UCOperation’; attach ‘SOperation’ to ‘Call’
        Call call = new Call(){operation = sop};
        ConditionCO.invoked.instructions.Add(call);
    }

    //*****************************************************************************************************
    // 3. ANY SENTENCE
    //*****************************************************************************************************  

    public override IntermediaryRepresentation VisitSvosentence([NotNull] RslBisParser.SvosentenceContext context)
    {
        CurrentLabel = context.label().NUMBER().GetText();
        return base.VisitSvosentence(context);
    }

    public override IntermediaryRepresentation VisitAltsvosentence([NotNull] RslBisParser.AltsvosentenceContext context)
    {
        CurrentLabel = context.altlabel().CHAR().GetText() + context.altlabel().NUMBER().GetText();
        return base.VisitAltsvosentence(context);
    }

    public override IntermediaryRepresentation VisitRepsentence([NotNull] RslBisParser.RepsentenceContext context)
    {
        CurrentLabel = null != context.label() ?
                               context.label().NUMBER().GetText() : 
                               context.altlabel().CHAR().GetText() + context.altlabel().NUMBER().GetText();
        return base.VisitRepsentence(context);
    }

    //*****************************************************************************************************
    // 4. SYSTEM-TO-DATA (READ) SENTENCE
    //*****************************************************************************************************  

    public override IntermediaryRepresentation VisitReadpredicate([NotNull] RslBisParser.ReadpredicateContext context)
    {
        if (Verbose) Console.WriteLine("System-to-Data (read) predicate: " + context.GetText());
        // 1. Create ‘DataAggregate’ (if does not exist) based on ‘notion’; add it to ‘CurrentDAP’; attach it to ‘ViewModel’
        string notionName = ObtainName(context.notion());
        DataAggregate da = result.ViewModel.items.Find(x => notionName == x.name);
        if (null == da) {
            da = new DataAggregate(){name = notionName};
            result.ViewModel.items.Add(da);
        }
        CurrentDAP.Add(da); // TODO - error if already exists in CurrentDAP
        // 2. Create ‘ServiceInterface’ (if does not exist) based on ‘notion’; attach it to ‘CurrentUCC’
        ServiceInterface si = result.ServiceInterfaces.Find(x => notionName == x.name);
        SOperation sop = si?.signatures.Find(x => "read! " + notionName == x.name);
        if (null == si) {
            si = new ServiceInterface(){name = notionName};
            result.ServiceInterfaces.Add(si);
            CurrentUCC.services.Add(si);
        }
        // 3. Create ‘SOperation’ (if does not exist) based on ‘notion’; set ‘returnType’ based on ‘DataAggregate’; add ‘SOperation’ to ‘ServiceInterface’
        if (null == sop) {
            sop = new SOperation(){name = "read! " + notionName, returnType = da.name};
            // 4. For each ‘DataAggregate’ in ‘CurrentDAD’+’InheritedDAD’ create a ‘DataItem’ (‘parameter’; type as ‘DataAggregate’ name);
            //    add the ‘DataItems’ to the ‘SOperation’
            foreach (DataAggregate xd in Enumerable.Concat(CurrentDAD,InheritedDAD)){
                DataItem di = new DataItem() {type = notionName};
                sop.parameters.Add(di);
            }
            si.signatures.Add(sop);
        }
        // 5. Create ‘Call’; attach ‘SOperation’ to it; ; set ‘Call.label’ as CurrentLabel
        Call call = new Call(){label = CurrentLabel, operation = sop};
        // 6. If ‘CurrentCondition’ empty ->  append ‘Call’ to ‘CurrentUCO’ else append ‘Call’ to ‘CurrentCondition’ 
        if (null == CurrentCondition) CurrentUCO.instructions.Add(call);
        else CurrentCondition.instructions.Add(call);
        // TODO - error when CurrentUCO is null
        LastPredicateType = PredicateType.Read;
        LastNonInvokePT = PredicateType.Read;
        return result;
    }

    //*****************************************************************************************************
    // 5. SYSTEM-TO-SCREEN SENTENCE
    //*****************************************************************************************************

    public override IntermediaryRepresentation VisitShowpredicate([NotNull] RslBisParser.ShowpredicateContext context)
    {
        if (Verbose) Console.WriteLine("System-to-Screen predicate: " + context.GetText());
        // 1. Create ‘ViewFunction’ (if does not exist) based on ‘notion’; set it as ‘CurrentVF’
        string notionName = ObtainName(context.notion());
        ViewFunction vf = result.ViewFunctions.Find(x => notionName == x.name);
        if (null == vf) {
            // 2. If (does not exist) -> create ‘ControllerFunction’; attach it to ‘ViewFunction’; attach ‘CurrentUCC’ to the ‘ControllerFunction’
            ControllerFunction cf = new ControllerFunction(){name = notionName};
            cf.useCases.Add(CurrentUCC);
            // 3. If (does not exist) -> create ‘PresenterClass’; attach it to ‘ViewFunction’; attach it to the ‘CurrentUCC’
            PresenterClass pc = new PresenterClass(){name = notionName};
            CurrentUCC.presenters.Add(pc);
            vf = new ViewFunction(){name = notionName, controller = cf, presenter = pc};
            // 4. If (does not exist) -> Attach all ‘DataAggregate’s in ‘CurrentDAP’ to the ‘ViewFunction’
            vf.data.AddRange(CurrentDAP);
            result.ViewFunctions.Add(vf);
            result.PresenterClasses.Add(pc);
            result.ControllerFunctions.Add(cf);
        }
        CurrentVF = vf;
        // 5. Create ‘POperation’ based on ‘notion’; add it to ‘PresenterClass’
        POperation pop = new POperation(){name = notionName};
        // 6. For each ‘DataAggregate’ in ‘CurrentDAP’ add a ‘DataItem’ (‘parameter’; type as ‘DataAggregate’ name);
        // attach the ‘DataItems’ to the ‘POperation’
        foreach (DataAggregate da in CurrentDAP) pop.parameters.Add(new DataItem(){type = da.name});
        POperation ppop = vf.presenter.methods.Find(x => pop.Equals(x)); // TODO - implement POperation.Equals
        if (null != ppop) pop = ppop;
        else vf.presenter.methods.Add(pop);
        // 7. Create ‘Call’; attach ‘POperation’ to it; set ‘Call.label’ as CurrentLabel
        Call cl = new Call(){operation = pop, label = CurrentLabel};
        // 8. If ‘CurrentCondition’ empty -> append ‘Call’ to ‘CurrentUCO’ else append ‘Call’ to ‘CurrentCondition’
        if (null == CurrentCondition) CurrentUCO.instructions.Add(cl);
        else CurrentCondition.instructions.Add(cl);
        // 9. Reset ‘CurrentDAD’ and ‘CurrentDAP’ (empty the lists)
        CurrentDAD.Clear();
        CurrentDAP.Clear();
        // 10. Append ‘label’-to-‘CurrentVF’ to ‘LabelToVF’
        LabelToVF.Add(CurrentLabel,vf);

        LastPredicateType = PredicateType.Show;
        LastNonInvokePT = PredicateType.Show;
        return result;
    }

    //*****************************************************************************************************
    // 6. ACTOR-TO-DATA SENTENCE
    //*****************************************************************************************************

    public override IntermediaryRepresentation VisitEnterpredicate([NotNull] RslBisParser.EnterpredicateContext context)
    {
        if (Verbose) Console.WriteLine("Actor-to-Data predicate: " + context.GetText());
        // 1. Create ‘DataAggregate’ (if does not exist) based on ‘notion’; add it to ‘CurrentDAD’; 
        // attach it to ‘ViewModel’; attach it to ‘CurrentVF’
        string notionName = ObtainName(context.notion());
        DataAggregate da = result.ViewModel.items.Find(x => notionName == x.name);
        if (null == da) {
            da = new DataAggregate(){name = notionName};
            result.ViewModel.items.Add(da);
        }
        CurrentDAD.Add(da);
        // 2. Append ‘label’-to-‘CurrentVF’ to ‘LabelToVF’
        LabelToVF.Add(CurrentLabel,CurrentVF);

        LastPredicateType = PredicateType.Enter;
        LastNonInvokePT = PredicateType.Enter;
        return result;
    }

    //*****************************************************************************************************
    // 7. ACTOR-INVOKE SENTENCE
    //*****************************************************************************************************  

    //*****************************************************************************************************
    // 8. ACTOR-TO-TRIGGER SENTENCE
    //*****************************************************************************************************

    public override IntermediaryRepresentation VisitSelectpredicate([NotNull] RslBisParser.SelectpredicateContext context)
    {
        if (Verbose) Console.WriteLine("Actor-to-Trigger predicate: " + context.GetText());
        // 1. Create a ‘Trigger’ based on ‘notion’; If ‘CurrentVF’ exists -> add it to ‘CurrentVF’
        Trigger trg = new Trigger(){name = ObtainName(context.notion())};
        if (null != CurrentVF) CurrentVF.triggers.Add(trg);
        // 2. Create a ‘COperation’ based on ‘notion’ ; attach it to the ‘Trigger’ as ‘action’
        COperation cop = new COperation(){name = "select! " + ObtainName(context.notion())};
        trg.action = cop;
        // 3. Attach all ‘DataAggregate’s in ‘CurrentDAD’ to the ‘COperation’
        cop.data.AddRange(CurrentDAD);
        // 4. Create ‘UCOperation’ based on ‘COperation’
        UCOperation ucop = new UCOperation(){name = cop.name}; // TODO - set 'name' with ViewFunction name as prefix
        // 5. For each ‘DataAggregate’ in ‘CurrentDAD’ create a ‘DataItem’ (‘parameters’; type as ‘DataAggregate’ name);
        //    add the ‘DataItems’ to the ‘UCOperation’
        foreach (DataAggregate da in CurrentDAD) ucop.parameters.Add(new DataItem(){type = da.name});
        // 6. Add ‘UCOperation’ to the ‘CurrentUCC’; attach it to ‘COperation’ (as ‘invoked’)
        CurrentUCC.methods.Add(ucop); cop.invoked = ucop;
        // 7. If ‘CurrentUCO’ empty -> Algorithm for initial trigger sentence
        if (null == CurrentUCO) ProcessInitialSentence(context, trg);
        // 8. else -> Add the ‘COperation’ to ‘ControllerFuntion’ attached to ‘CurrentVF’; attach ‘CurrentUCC’ to ‘ControllerFunction’
        else {
            CurrentVF.controller.functions.Add(cop);
            CurrentVF.controller.useCases.Add(CurrentUCC);
        }
        // 9. Set ‘UCOperation’ as ‘CurrentUCO’
        CurrentUCO = ucop;

        LastPredicateType = PredicateType.Select;
        LastNonInvokePT = PredicateType.Select;
        return result;

    }

    private void ProcessInitialSentence(RslBisParser.SelectpredicateContext context, Trigger trg){
        // 1. If ‘ConditionCO’ not empty -> attach ‘ConditionCO’ to ‘Trigger’ as ‘condition’
        if (null != ConditionCO) trg.condition = ConditionCO;
        // 2. If ‘CurrentUCC.name’ is in ‘UcNameToViewFunction’ -> for each matching ‘ViewFunction’ in ‘UcNameToViewFunction’ ->
        if (UcNameToVF.ContainsKey(CurrentUCC.name)) foreach (ViewFunction vf in UcNameToVF[CurrentUCC.name]){
            // add ‘COperation’ (copy and attach if necessary) and ‘ConditionCO’ (if not empty, copy and attach if necessary)
            //    to ‘ControllerFuntion’ attached to ‘ViewFunction’;
            COperation cop = trg.action;
            COperation newcop = new COperation(){invoked = cop.invoked, name = cop.name};
            newcop.data.AddRange(cop.data);
            vf.controller.functions.Add(newcop);
            COperation condcop = null;
            if (null != ConditionCO){
                condcop = new COperation(){invoked = ConditionCO.invoked, name = ConditionCO.name,
                                                  returnType = ConditionCO.returnType};
                condcop.data.AddRange(ConditionCO.data);
                vf.controller.functions.Add(condcop);
            }
            // add ‘Trigger’ (copy if necessary);
            Trigger newtrg = new Trigger(){name = trg.name, action = newcop, condition = condcop};
            // attach ‘CurrentUCC’ to ‘ControllerFunction’ attached to ‘ViewFunction’;
            vf.controller.useCases.Add(CurrentUCC);
            // attach matching (‘CurrentUCC.name’ & ‘ViewFunction.name’) ‘UCOperation’ from ‘UcVFToUCOperation’ to ‘COperation’ as ‘return’;
            UcVFToUCOperation.TryGetValue((CurrentUCC.name, vf.name), out cop.returnTo);
            // remove ‘UcNameToViewFunction’ and ‘UcVFToUCOperation’ entries
            UcNameToVF.Remove(CurrentUCC.name);
            UcVFToUCOperation.Remove((CurrentUCC.name, vf.name));
        }
        // 3. Add ‘CurrentUCC.name’ -to-‘Trigger’ to ‘UcNameToTrigger’
        UcNameToTrigger.Add(CurrentUCC.name,trg);
        // 4. Clear ‘ConditionCO’
        ConditionCO = null;
    }

    //*****************************************************************************************************
    // 9. SYSTEM-TO-DATA (CUD/CHECK) SENTENCE
    //*****************************************************************************************************

    //*****************************************************************************************************
    // 10. SYSTEM-TO-DATA (EXECUTE) SENTENCE
    //*****************************************************************************************************

    //*****************************************************************************************************
    // 11. REPETITION SENTENCE
    //*****************************************************************************************************

    //*****************************************************************************************************
    // 12. CONDITION SENTENCE
    //*****************************************************************************************************

    //*****************************************************************************************************
    // 13. END SENTENCE
    //*****************************************************************************************************

    //*****************************************************************************************************
    // 14. REJOIN SENTENCE
    //*****************************************************************************************************

}