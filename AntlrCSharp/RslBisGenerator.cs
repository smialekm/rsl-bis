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
using System.ComponentModel.Design;

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
    bool FirstSentence {get; set;}
    bool ConditionPossible {get; set;}
    Dictionary<string,ViewFunction> LabelToVF {get; set;} = new Dictionary<string, ViewFunction>();
    Dictionary<string,Trigger> UcNameToTrigger {get; set;} = new Dictionary<string, Trigger>();
    Dictionary<string,List<ViewFunction>> UcNameToVF {get; set;} = new Dictionary<string, List<ViewFunction>>();
    Dictionary<(string,string),UCOperation> UcVFToUCOperation {get; set;} = new Dictionary<(string, string), UCOperation>();
    DataAggregate CurrentViewAggregate = null;
    CheckEnumeration ScreenIdEnum = null;

    // Configuration

    public bool Verbose {get; set;} = false;

    public RslBisGenerator(){
        
    }

    private static string ObtainName(ParserRuleContext context){
        ITerminalNode[] strings;
        if (context is RslBisParser.NameContext nameContext) strings = nameContext.STRING();
        else if (context is RslBisParser.NotionContext notionContext) strings = notionContext.STRING();
        else if (context is RslBisParser.ValueContext valueContext) strings = valueContext.STRING();
        else if (context is RslBisParser.UilabelContext uiContext) strings = uiContext.STRING();
        else if (context is RslBisParser.DatatypeContext dataContext) return dataContext.GetText();
        else if (context is RslBisParser.MultnotionContext multContext) return multContext.GetText();
        else if (context is RslBisParser.TriggertypeContext trgContext) return trgContext.GetText();
        else if (context is RslBisParser.ViewtypeContext vContext) return vContext.GetText();
        else return null;     
        return string.Join(" ", strings.ToList().Select(x => x.GetText()));
    }

    public override IntermediaryRepresentation VisitStart([NotNull] RslBisParser.StartContext context)
    {
        if (Verbose) Console.WriteLine("Starting processing RSL specification");
        ScreenIdEnum = new CheckEnumeration(){name = "screen id"};
        result.ViewModel.enums.Add(ScreenIdEnum);
        Value start = new Value(){name = "start", parent = ScreenIdEnum};
        ScreenIdEnum.values.Add(start);
        DataAggregate appState = new DataAggregate(){name = "app state"};
        DataItem screenId = new DataItem(){name = "screen", type = ScreenIdEnum.name, typeKind = TypeKind.Simple};
        appState.fields.Add(screenId);
        result.ViewModel.items.Add(appState);
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
        UseCaseClass ucC = new UseCaseClass(){name = name};
        result.UseCaseClasses.Add(ucC);
        CurrentUCC = ucC;
        CleanupUseCaseGenerator();
        return base.VisitUsecase(context);
    }

    private void CleanupUseCaseGenerator(){
        CurrentVF = null;
        CurrentUCO = null;
        CurrentCondition = null;
        ConditionCO = null;
        InheritedDAD = new List<DataAggregate>();
        CurrentDAP = new List<DataAggregate>();
        CurrentDAD = new List<DataAggregate>();
        CurrentLabel = null;
        LastPredicateType = PredicateType.None;
        LastNonInvokePT = PredicateType.None;
        FirstSentence = true;
        ConditionPossible = false;
        LabelToVF = new Dictionary<string, ViewFunction>();
    }

    //*****************************************************************************************************
    // 2. PRECONDITION
    //*****************************************************************************************************  

    public override IntermediaryRepresentation VisitUcconditions([NotNull] RslBisParser.UcconditionsContext context)
    {
        try {
            if (Verbose) Console.WriteLine("Precondition sentence: " + context.GetText());
            ProcessPreconditions(context.conditions());
        } catch (Exception e) {
            WriteErrorMessage(e);
        }
        return result;
    }

    private void ProcessPreconditions(RslBisParser.ConditionsContext context)
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
        if (null != ccondition){
            // 2. if ‘condition’ is ‘contextcondition’ -> add ‘DataAggregate’ to ‘InheritedDAD’ and ‘CurrentUCC.attrs’
            InheritedDAD.Add(da); // TODO - replace by CurrentUCC.attrs
            CurrentUCC.attrs.Add(da);
        } else {
            // 3. If any ‘valuecondition’ exists -> create ‘COperation’; set it as ‘ConditionCO’;
            //    create ‘UCOperation’; add it to ‘CurrentUCC’; attach it to ‘COperation’;
            //    set ‘UCOperation.returnType’ as ‘boolean’
            if (null == ConditionCO) {
                UCOperation ucop = new UCOperation(){returnType = "boolean", uc = CurrentUCC, name = "precondition check!"};
                CurrentUCC.methods.Add(ucop);
                COperation cop = new COperation(){returnType = "boolean", name = "invoke check! " + CurrentUCC.name, invoked = ucop};
                ConditionCO = cop;
            }
            // 4. For each ‘valuecondition’ -> Algorithm for value condition
            ProcessValueCondition(vcondition, da);
        }
        ProcessPreconditions(context.conditions());
    }

    private void ProcessValueCondition(RslBisParser.ValueconditionContext context, DataAggregate da){
        string notionName = ObtainName(context.notion());
        // 1. Create ‘DataItem’ (‘parameter’; type as ‘notion’); add ‘DataItem’ to ‘ConditionCO.invoked’ ('UCOperation');
        // attach ‘DataAggregate’ to ‘ConditionCO’ (as ‘data’)
        CodeModel.Parameter di = new CodeModel.Parameter() {type = notionName};
        if (CurrentUCC.attrs.Contains(da)) di.isAttribute = true;
        ConditionCO.invoked.parameters.Add(di);
        ConditionCO.data.Add(da);
        // 2. Create ‘ServiceInterface’ (if does not exist) based on ‘notion’; attach it to ‘CurrentUCC’
        ServiceInterface si = result.ServiceInterfaces.Find(x => notionName == x.name);
        SOperation sop = si?.signatures.Find(x => "check! " + notionName == x.name);
        if (null == si) {
            si = new ServiceInterface(){name = notionName};
            result.ServiceInterfaces.Add(si);
        }
        if (!CurrentUCC.services.Contains(si)) CurrentUCC.services.Add(si);
        // 3. Create ‘Enumeration’ (if does not exist) based on ‘notion’; add it to ‘ViewModel’
        CheckEnumeration en = result.ViewModel.enums.Find(x => notionName + " !enum" == x.name);
        string valueName = ObtainName(context.value());
        Value value = en?.values.Find(x => valueName == x.name);
        if (null == en) {
            en = new CheckEnumeration(){name = notionName + " !enum"};
            result.ViewModel.enums.Add(en);
            da.enumer = en;
        }
        // 4. Create ‘Value’ based on ‘value’; add it to ‘Enumeration’
        if (null == value) {
            value = new Value(){name = valueName, parent = en};
            en.values.Add(value);
        }
        // 5. Create ‘SOperation’ (if does not exist); add ‘DataItem’ to ‘SOperation’; add ‘SOperation’ to 'ServiceInterface'
        // set ‘SOperation.returnType’ to ‘Enumeration.name’
        if (null == sop) {
            sop = new SOperation(){name = "check! " + notionName, returnType = en.name, type = PredicateType.Check, si = si};
            sop.parameters.Add(di);
            si.signatures.Add(sop);
        }
        // 6. Create ‘Call’; append it to ‘UCOperation’; attach ‘SOperation’ to ‘Call’
        Call call = new Call(){operation = sop, value = value};
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

    private void CreateCall(UCCallableOperation uop){
        // 1. Create ‘Call’; attach ‘SOperation’ to it; ; set ‘Call.label’ as CurrentLabel
        Call call = new Call(){label = CurrentLabel, operation = uop};
        // 2. If ‘CurrentCondition’ empty ->  append ‘Call’ to ‘CurrentUCO’ else append ‘Call’ to ‘CurrentCondition’ 
        if (null == CurrentCondition) CurrentUCO.instructions.Add(call);
        else CurrentCondition.instructions.Add(call);
    }

    private void SetLastPredicateTypes(PredicateType ptype){
        LastPredicateType = ptype;
        LastNonInvokePT = ptype;
    }

    //*****************************************************************************************************
    // 4. SYSTEM-TO-DATA (READ) SENTENCE
    //*****************************************************************************************************  

    public override IntermediaryRepresentation VisitReadpredicate([NotNull] RslBisParser.ReadpredicateContext context)
    {
        if (Verbose) Console.WriteLine(CurrentLabel + ": System-to-Data (read) predicate: " + context.GetText());
        ConditionPossible = true;
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
        }
        if (!CurrentUCC.services.Contains(si)) CurrentUCC.services.Add(si);
        // 3. Create ‘SOperation’ (if does not exist) based on ‘notion’; set ‘returnType’ based on ‘DataAggregate’; add ‘SOperation’ to ‘ServiceInterface’
        if (null == sop) { // TODO - handle overloaded methods
            sop = new SOperation(){name = "read! " + notionName, returnType = da.name, type = PredicateType.Read, si = si};
            // 4. For each ‘DataAggregate’ in ‘CurrentDAD’+’InheritedDAD’ create a ‘DataItem’ (‘parameter’; type as ‘DataAggregate’ name);
            //    add the ‘DataItems’ to the ‘SOperation’
            foreach (DataAggregate xd in CurrentDAD)
                if (!sop.parameters.Exists(di => xd.name == di.type)){
                    CodeModel.Parameter di = new CodeModel.Parameter() {type = xd.name};
                    if (CurrentUCC.attrs.Contains(xd)) di.isAttribute = true;
                    sop.parameters.Add(di);
                }
            foreach (DataAggregate xd in InheritedDAD)
                if (!sop.parameters.Exists(di => xd.name == di.type)){
                    CodeModel.Parameter di = new CodeModel.Parameter() {type = xd.name, isAttribute = true};
                    sop.parameters.Add(di);
                }
            si.signatures.Add(sop);
        }
        // 5. Create ‘Call’ etc.
        CreateCall(sop);

        LabelToVF.Add(CurrentLabel,null);
        SetLastPredicateTypes(PredicateType.Read);
        return result;
    }

    //*****************************************************************************************************
    // 5. SYSTEM-TO-VIEW SENTENCE
    //*****************************************************************************************************

    public override IntermediaryRepresentation VisitShowpredicate([NotNull] RslBisParser.ShowpredicateContext context)
    {
        if (Verbose) Console.WriteLine(CurrentLabel + ": System-to-View predicate: " + context.GetText());
        ConditionPossible = false;
        // 1. Create ‘ViewFunction’ (if does not exist) based on ‘notion’; set it as ‘CurrentVF’
        string notionName = ObtainName(context.notion());
        ViewFunction vf = result.ViewFunctions.Find(x => notionName == x.name);
        if (null == vf) {
            // 2. If (does not exist) -> create ‘ControllerFunction’; attach it to ‘ViewFunction’; attach ‘CurrentUCC’ to the ‘ControllerFunction’
            ControllerFunction cf = new ControllerFunction(){name = notionName};
            cf.useCase = CurrentUCC;
            // 3. If (does not exist) -> create ‘PresenterClass’; attach it to ‘ViewFunction’; attach it to the ‘CurrentUCC’
            PresenterClass pc = new PresenterClass(){name = notionName};
            CurrentUCC.presenters.Add(pc);
            vf = new ViewFunction(){name = notionName, controller = cf, presenter = pc};
            ScreenIdEnum.values.Add(new Value(){name = Utils.ToUpperCase(notionName), parent = ScreenIdEnum});
            // 4. If (does not exist) -> Attach all ‘DataAggregate’s in ‘CurrentDAP’ to the ‘ViewFunction’
            vf.data.AddRange(CurrentDAP);
            result.ViewFunctions.Add(vf);
            result.PresenterClasses.Add(pc);
            result.ControllerFunctions.Add(cf);
            CurrentViewAggregate = new DataAggregate(){name = notionName + " State"};
            result.ViewModel.items.Add(CurrentViewAggregate);
            CurrentViewAggregate.fields.AddRange(CurrentDAP.Select(da => new DataItem()
                {name = da.name, type = da.name, typeKind = TypeKind.Simple, baseType = da}));
        } else
            CurrentViewAggregate = result.ViewModel.items.Find(da => notionName + " State" == da.name);
        CurrentVF = vf;
        // 5. Create ‘POperation’ based on ‘notion’; add it to ‘PresenterClass’
        POperation pop = new POperation(){name = "show! " + notionName, pres = vf.presenter};
        // 6. For each ‘DataAggregate’ in ‘CurrentDAP’ add a ‘DataItem’ (‘parameter’; type as ‘DataAggregate’ name);
        // attach the ‘DataItems’ to the ‘POperation’
        foreach (DataAggregate da in CurrentDAP) {
            CodeModel.Parameter par = new CodeModel.Parameter(){type = da.name};
            if (CurrentUCC.attrs.Contains(da)) par.isAttribute = true;
            pop.parameters.Add(par);
        }
        POperation ppop = vf.presenter.methods.Find(x => pop.Equals(x)); // TODO - implement POperation.Equals
        if (null != ppop) pop = ppop;
        else vf.presenter.methods.Add(pop);
        // 7., 8. Create ‘Call’ etc.
        CreateCall(pop);
        // 9. Reset ‘CurrentDAD’ and ‘CurrentDAP’ (empty the lists); reset 'CurrentUCO'
        CurrentDAD.Clear();
        CurrentDAP.Clear();
        CurrentUCO = null; // TODO - asynchronous "execute" (reset only before the first Actor-to-... sentence)
        CurrentCondition = null;
        // 10. Append ‘label’-to-‘CurrentVF’ to ‘LabelToVF’
        LabelToVF.Add(CurrentLabel,vf);

        SetLastPredicateTypes(PredicateType.Show);
        return result;
    }

    //*****************************************************************************************************
    // 6. ACTOR-TO-DATA SENTENCE
    //*****************************************************************************************************

    public override IntermediaryRepresentation VisitEnterpredicate([NotNull] RslBisParser.EnterpredicateContext context)
    {
        if (Verbose) Console.WriteLine(CurrentLabel + ": Actor-to-Data predicate: " + context.GetText());
        ConditionPossible = false;
        if (null == CurrentVF)
            throw new Exception("Unexpected <enter> sentence");
        // 1. Create ‘DataAggregate’ (if does not exist) based on ‘notion’; add it to ‘CurrentDAD’; 
        // attach it to ‘ViewModel’; attach it to ‘CurrentVF’
        string notionName = ObtainName(context.notion());
        DataAggregate da = result.ViewModel.items.Find(x => notionName == x.name);
        if (null == da) {
            da = new DataAggregate(){name = notionName};
            result.ViewModel.items.Add(da);
        }
        CurrentDAD.Add(da);
        if (!CurrentUCC.attrs.Contains(da)) CurrentUCC.attrs.Add(da);
        if (!CurrentVF.data.Contains(da)) {
            CurrentVF.data.Add(da);
            CurrentViewAggregate.fields.Add(new DataItem()
                {name = da.name, type = da.name, typeKind = TypeKind.Simple, baseType = da});
        }
        CurrentVF.editable.Add(da);
        // 2. Append ‘label’-to-‘CurrentVF’ to ‘LabelToVF’
        LabelToVF.Add(CurrentLabel,CurrentVF);

        SetLastPredicateTypes(PredicateType.Enter);
        return result;
    }

    //*****************************************************************************************************
    // 7. ACTOR-INVOKE SENTENCE
    //*****************************************************************************************************

    public override IntermediaryRepresentation VisitInvoke([NotNull] RslBisParser.InvokeContext context)
    {
        if (Verbose) Console.WriteLine(CurrentLabel + ": Actor-Invoke sentence: " + context.GetText());
        ConditionPossible = true;
        EnumUnion eu = null;
        if (null != context.names().names()) {
            eu = new EnumUnion(){name = CurrentUCC.name +" @ " + CurrentLabel + " !union !enum"};
            result.ViewModel.unions.Add(eu);
        }
        ProcessUserInvoke(context.names(), eu);
        LabelToVF.Add(CurrentLabel,null);
        LastPredicateType = PredicateType.Invoke;
        return result;
    }

    private void ProcessUserInvoke(RslBisParser.NamesContext context, EnumUnion eu, UCOperation returnTo = null){
        if (null == context) return;
        string ucName = ObtainName(context.name());
        // 1. Create ‘Enumeration’ (if does not exist) based on ‘name’; add it to ‘ViewModel’
        CheckEnumeration en = result.ViewModel.enums.Find(x => ucName + " !result !enum" == x.name);
        if (null == en) {
            en = new CheckEnumeration(){name = ucName + " !result !enum"};
            result.ViewModel.enums.Add(en);
        }
        // 2. Create ‘UCOperation’ based on ‘name’ and ‘CurrentVF.name’ (‘invoked…’); add it to ‘CurrentUCC’; set it as ‘CurrentUCO’
        UCOperation ucop;
        if (null == returnTo){
            // ucop = new UCOperation(){name = "invoked " + ucName + " @ " + CurrentVF.name, uc = CurrentUCC};
            ucop = new UCOperation(){name = "invoked @ " + CurrentLabel, uc = CurrentUCC};
            CurrentUCC.methods.Add(ucop);
            CurrentUCO = ucop;
            string type;
            if (null == eu) type = en.name;
            else {
                eu.elements.Add(en);
                type = eu.name;
            }
            // 3. Create ‘DataItem’ (‘parameter’; type as ‘Enumeration’); add it to 'UCOperation’
            ucop.parameters.Add(new CodeModel.Parameter(){type = "any"});
            ucop.parameters.Add(new CodeModel.Parameter(){type = type});
        } else {
            ucop = returnTo;
            eu.elements.Add(en);
        }
        // 4. If ‘name’ is in ‘UcNameToTrigger’  -> 
        if (UcNameToTrigger.ContainsKey(ucName)){
            // add ‘Trigger.action’ (copy and attach if necessary) to ‘ControllerFuntion’ attached to ‘CurrentVF’;
            Trigger trg = UcNameToTrigger[ucName];
            COperation cop = trg.action;
            UCOperation baseUcop = cop.invoked;
            UCOperation proxyUcop = new UCOperation(){name = baseUcop.name, invoking = true, uc = CurrentUCC};
            foreach (DataAggregate par in cop.data)
                proxyUcop.parameters.Add(new CodeModel.Parameter(){type = par.name});
            CurrentUCC.methods.Add(proxyUcop);
            Call call = new Call(){operation = baseUcop};
            proxyUcop.instructions.Add(call);
            COperation newcop = new COperation(){invoked = proxyUcop, name = cop.name};
            newcop.data.AddRange(cop.data);
            CurrentVF.controller.functions.Add(newcop);
            // add ‘Trigger.condition’ (if not empty, copy and attach if necessary) to ‘ControllerFuntion’ attached to ‘CurrentVF’;
            COperation condcop = trg.condition;
            COperation newCondcop = null;
            if (null != condcop){
                UCOperation baseCUcop = condcop.invoked;
                UCOperation proxyCUcop = new UCOperation(){name = baseCUcop.name + " " + ucName, uc = CurrentUCC, returnType = "boolean"};
                foreach (DataAggregate par in cop.data)
                    proxyCUcop.parameters.Add(new CodeModel.Parameter(){type = par.name});
                CurrentUCC.methods.Add(proxyCUcop);
                Call condCall = new Call(){operation = baseCUcop};
                proxyCUcop.instructions.Add(condCall);
                newCondcop = new COperation(){invoked = proxyCUcop, name = condcop.name,
                                                  returnType = condcop.returnType};
                newCondcop.data.AddRange(condcop.data);
                CurrentVF.controller.functions.Add(newCondcop);
            }
            // add the ‘Trigger’ (copy if necessary) from the map entry to ‘CurrentVF’;
            Trigger newtrg = new Trigger(){name = trg.name, action = newcop, condition = newCondcop};
            CurrentVF.triggers.Add(newtrg);
            // attach ‘Trigger.action.invoked.uc’ to ‘ControllerFuntion’ attached to ‘CurrentVF’ (if necessary);
            if (!CurrentVF.controller.useCase.invoked.Contains(cop.invoked.uc))
                CurrentVF.controller.useCase.invoked.Add(cop.invoked.uc);
            // attach ‘UCOperation’ to ‘Trigger.action’ as ‘returnTo’
            newcop.returnTo = ucop;
        // 5. else -> add use case ‘name’-to-‘CurrentVF’ to ‘UcNameToViewFunction’;
        } else {
            if (!UcNameToVF.ContainsKey(ucName)) UcNameToVF.Add(ucName, new List<ViewFunction>());
            if (!UcNameToVF[ucName].Contains(CurrentVF)) {
                UcNameToVF[ucName].Add(CurrentVF);
                // add use case ‘name’&’CurrentVF.name’-to-‘UCOperation’ to ‘UcVFToUCOperation’
                UcVFToUCOperation.Add((ucName, CurrentVF.name), ucop);
            }
            else throw new Exception("Duplicate use case invocation");
        }
        ProcessUserInvoke(context.names(), eu, ucop);
    }

    //*****************************************************************************************************
    // 8. ACTOR-TO-TRIGGER SENTENCE
    //*****************************************************************************************************

    public override IntermediaryRepresentation VisitSelectpredicate([NotNull] RslBisParser.SelectpredicateContext context)
    {
        if (Verbose) Console.WriteLine(CurrentLabel + ": Actor-to-Trigger predicate: " + context.GetText());
        ConditionPossible = false;
        // 1. Create a ‘Trigger’ based on ‘notion’; If ‘CurrentVF’ exists -> add it to ‘CurrentVF’
        Trigger trg = new Trigger(){name = ObtainName(context.notion())};
        if (null != CurrentVF) CurrentVF.triggers.Add(trg);
        // 2. Create a ‘COperation’ based on ‘notion’ ; attach it to the ‘Trigger’ as ‘action’
        COperation cop = new COperation(){name = "select! " + ObtainName(context.notion())};
        trg.action = cop;
        // 3. Attach all ‘DataAggregate’s in ‘CurrentDAD’ to the ‘COperation’
        cop.data.AddRange(CurrentDAD);
        // 4. Create ‘UCOperation’ based on ‘COperation’
        int counter = 0;
        string ucopName = cop.name;
        while (CurrentUCC.methods.Exists(x => ucopName == x.name)){
            ucopName = cop.name + (0 == counter ? "" : " "+counter);
            counter++;
        }
        UCOperation ucop = new UCOperation(){name = ucopName, uc = CurrentUCC};
        // 5. For each ‘DataAggregate’ in ‘CurrentDAD’ create a ‘DataItem’ (‘parameters’; type as ‘DataAggregate’ name);
        //    add the ‘DataItems’ to the ‘UCOperation’
        foreach (DataAggregate da in CurrentDAD) {
            CodeModel.Parameter par = new CodeModel.Parameter(){type = da.name};
            if (CurrentUCC.attrs.Contains(da)) par.isAttribute = true;
            ucop.parameters.Add(par);
        }
        // 6. Add ‘UCOperation’ to the ‘CurrentUCC’; attach it to ‘COperation’ (as ‘invoked’)
        CurrentUCC.methods.Add(ucop); cop.invoked = ucop;
        // 7. Set ‘UCOperation’ as ‘CurrentUCO’
        CurrentUCO = ucop;
        // 8. If ‘CurrentUCO’ empty -> Algorithm for initial trigger sentence
        if (FirstSentence) {
            ProcessInitialSentence(context, trg);
            FirstSentence = false;
        // 9. else -> Add the ‘COperation’ to ‘ControllerFuntion’ attached to ‘CurrentVF’; attach ‘CurrentUCC’ to ‘ControllerFunction’
        } else {
            CurrentVF.controller.functions.Add(cop);
            // if (!CurrentVF.controller.useCase.invoked.Contains(CurrentUCC)) CurrentVF.controller.useCase.invoked.Add(CurrentUCC);
        }

        SetLastPredicateTypes(PredicateType.Select);
        return result;
    }

    private void ProcessInitialSentence(RslBisParser.SelectpredicateContext context, Trigger trg){
        CurrentUCO.initial = true;
        // 1. Attach all ‘DataAggregate’s in ‘InheritedDAD’ to ‘COperation’ and ‘UCOperartion’ 
        COperation cop = trg.action;
        cop.data.AddRange(InheritedDAD);
        foreach (DataAggregate da in InheritedDAD) CurrentUCO.parameters.Add(new CodeModel.Parameter(){type = da.name, isAttribute = true});
        // 2. If ‘ConditionCO’ not empty -> attach ‘ConditionCO’ to ‘Trigger’ as ‘condition’
        if (null != ConditionCO) trg.condition = ConditionCO;
        // 3. If ‘CurrentUCC.name’ is in ‘UcNameToViewFunction’ -> for each matching ‘ViewFunction’ in ‘UcNameToViewFunction’ ->
        if (UcNameToVF.ContainsKey(CurrentUCC.name)) foreach (ViewFunction vf in UcNameToVF[CurrentUCC.name]){
            // add ‘COperation’ (copy and attach if necessary) and ‘ConditionCO’ (if not empty, copy and attach if necessary)
            //    to ‘ControllerFuntion’ attached to ‘ViewFunction’;
            UseCaseClass invokingUC = vf.controller.useCase;
            UCOperation baseUcop = cop.invoked;
            UCOperation proxyUcop = new UCOperation(){name = baseUcop.name, invoking = true, uc = invokingUC};
            foreach (DataAggregate parDa in cop.data) {
                CodeModel.Parameter par = new CodeModel.Parameter(){type = parDa.name};
                if (invokingUC.attrs.Contains(parDa)) par.isAttribute = true;
                proxyUcop.parameters.Add(par);
            }
            invokingUC.methods.Add(proxyUcop);
            Call call = new Call(){operation = baseUcop};
            proxyUcop.instructions.Add(call);
            COperation newcop = new COperation(){invoked = proxyUcop, name = cop.name};
            newcop.data.AddRange(cop.data);
            vf.controller.functions.Add(newcop);
            COperation condcop = null;
            if (null != ConditionCO){
                UCOperation baseCUcop = ConditionCO.invoked;
                UCOperation proxyCUcop = new UCOperation(){name = baseCUcop.name + " " + CurrentUCC.name, uc = invokingUC, returnType = "boolean"};
                foreach (DataAggregate parDa in cop.data) {
                    CodeModel.Parameter par = new CodeModel.Parameter(){type = parDa.name};
                    if (invokingUC.attrs.Contains(parDa)) par.isAttribute = true;
                    proxyUcop.parameters.Add(par);
                }
                invokingUC.methods.Add(proxyCUcop);
                Call condCall = new Call(){operation = baseCUcop};
                proxyCUcop.instructions.Add(condCall);
                condcop = new COperation(){invoked = proxyCUcop, name = ConditionCO.name,
                                                  returnType = ConditionCO.returnType};
                condcop.data.AddRange(ConditionCO.data);
                vf.controller.functions.Add(condcop);
            }
            // add ‘Trigger’ (copy if necessary);
            Trigger newTrg = new Trigger(){name = trg.name, action = newcop, condition = condcop};
            vf.triggers.Add(newTrg);
            // attach ‘CurrentUCC’ to ‘ControllerFunction’ attached to ‘ViewFunction’ (if necessary);
            if (!invokingUC.invoked.Contains(CurrentUCC)) invokingUC.invoked.Add(CurrentUCC);
            // attach matching (‘CurrentUCC.name’ & ‘ViewFunction.name’) ‘UCOperation’ from ‘UcVFToUCOperation’ to ‘COperation’ as ‘return’;
            UcVFToUCOperation.TryGetValue((CurrentUCC.name, vf.name), out newcop.returnTo);
            // remove ‘UcNameToViewFunction’ and ‘UcVFToUCOperation’ entries
            UcNameToVF.Remove(CurrentUCC.name);
            UcVFToUCOperation.Remove((CurrentUCC.name, vf.name));
        }
        // 4. Add ‘CurrentUCC.name’ -to-‘Trigger’ to ‘UcNameToTrigger’
        UcNameToTrigger.Add(CurrentUCC.name,trg);
        // 5. Clear ‘ConditionCO’
        ConditionCO = null;
    }

    //*****************************************************************************************************
    // 9. SYSTEM-TO-DATA (CUD/CHECK) SENTENCE
    //*****************************************************************************************************

    public override IntermediaryRepresentation VisitUpdatepredicate([NotNull] RslBisParser.UpdatepredicateContext context)
    {
        if (Verbose) Console.WriteLine(CurrentLabel + ": System-to-Data (update) predicate: " + context.GetText());
        ConditionPossible = true;
        ProcessDataSentence("update", context.notion());
        LabelToVF.Add(CurrentLabel,null);
        SetLastPredicateTypes(PredicateType.Update);
        return result;
    }

    public override IntermediaryRepresentation VisitDeletepredicate([NotNull] RslBisParser.DeletepredicateContext context)
    {
        if (Verbose) Console.WriteLine(CurrentLabel + ": System-to-Data (delete) predicate: " + context.GetText());
        ConditionPossible = true;
        ProcessDataSentence("delete", context.notion());
        LabelToVF.Add(CurrentLabel,null);
        SetLastPredicateTypes(PredicateType.Delete);
        return result;
    }

    public override IntermediaryRepresentation VisitCheckpredicate([NotNull] RslBisParser.CheckpredicateContext context)
    {
        if (Verbose) Console.WriteLine(CurrentLabel + ": System-to-Data (check) predicate: " + context.GetText());
        ConditionPossible = true;
        ProcessDataSentence("check", context.notion());
        LabelToVF.Add(CurrentLabel,null);
        SetLastPredicateTypes(PredicateType.Check);
        return result;
    }

    private void ProcessDataSentence(string verb, RslBisParser.NotionContext notion){
        string notionName = ObtainName(notion);
        // 1. Create ‘ServiceInterface’ (if does not exist) based on ‘notion’; attach it to ‘CurrentUCC’
        ServiceInterface si = result.ServiceInterfaces.Find(x => notionName == x.name);
        SOperation sop = si?.signatures.Find(x => verb +"! " + notionName == x.name);
        if (null == si) {
            si = new ServiceInterface(){name = notionName};
            result.ServiceInterfaces.Add(si);
        }
        if (!CurrentUCC.services.Contains(si)) CurrentUCC.services.Add(si);
        // 2. Create ‘SOperation’ (if does not exist) based on ‘notion’; add ‘SOperation’ to ‘ServiceInterface’
        if (null == sop) { // TODO - handle overloaded methods
            sop = new SOperation(){name = verb + "! " + notionName, 
                    type = "check" == verb ? PredicateType.Check : PredicateType.Execute, si = si};
            // 3. Create ‘DataItem’ based on ‘notion’; add it to ‘SOperation’
            CodeModel.Parameter di;
            if ("execute" != verb) {
                di = new CodeModel.Parameter() {type = notionName};
                DataAggregate da = CurrentUCC.attrs.Find(da => da.name == notionName);
                if (null != da) di.isAttribute = true;
                sop.parameters.Add(di);
            }
            // 4. For each ‘DataAggregate’ in ‘InheritedDAD’ create a ‘DataItem’ (‘parameter’; type as ‘DataAggregate’ name);
            //    add the ‘DataItems’ to the ‘SOperation’
            if ("execute" != verb)
                foreach (DataAggregate xd in CurrentDAD)
                    if (!sop.parameters.Exists(di => xd.name == di.type)){
                        di = new CodeModel.Parameter() {type = xd.name};
                        if (CurrentUCC.attrs.Contains(xd)) di.isAttribute = true;
                        sop.parameters.Add(di);
                    }
            foreach (DataAggregate xd in InheritedDAD)
                if (!sop.parameters.Exists(di => xd.name == di.type)){
                    di = new CodeModel.Parameter() {type = xd.name, isAttribute = true};
                    sop.parameters.Add(di);
                }
            si.signatures.Add(sop);
        }
        // 5. If sentence has a ‘checkpredicate’ (“check” sentence) -> create ‘Enumeration’ based on ‘notion’; add it to ‘ViewModel’;
        //    set ‘SOperation.returnType’ to “short” or ‘Enumeration.name’
        if ("check" == verb) {
            CheckEnumeration en = result.ViewModel.enums.Find(x => notionName + " !enum" == x.name);
            if (null == en) {
                en = new CheckEnumeration(){name = notionName + " !enum"};
                result.ViewModel.enums.Add(en);
                DataAggregate da = result.ViewModel.items.Find(x => notionName == x.name);
                if (null == da) throw new Exception("Check for a non-existent notion");
                da.enumer = en;
            }
            sop.returnType = en.name;
        } else sop.returnType = "bigint";
        // 6. Create ‘Call’ etc.
        CreateCall(sop);
    }

    //*****************************************************************************************************
    // 10. SYSTEM-TO-DATA (EXECUTE) SENTENCE
    //*****************************************************************************************************

    public override IntermediaryRepresentation VisitExecutepredicate([NotNull] RslBisParser.ExecutepredicateContext context)
    {
        if (Verbose) Console.WriteLine(CurrentLabel + ": System-to-Data (execute) predicate: " + context.GetText());
        ConditionPossible = true;
        ProcessDataSentence("execute", context.notion());
        LabelToVF.Add(CurrentLabel,null);
        SetLastPredicateTypes(PredicateType.Execute);
        return result;
    }

    //*****************************************************************************************************
    // 11. REPETITION SENTENCE
    //*****************************************************************************************************

    public override IntermediaryRepresentation VisitRepsentence([NotNull] RslBisParser.RepsentenceContext context)
    {
        try {
            CurrentLabel = null != context.label() ?
                                   context.label().NUMBER().GetText() :
                                   context.altlabel().CHAR().GetText() + context.altlabel().NUMBER().GetText();
            if (Verbose) Console.WriteLine(CurrentLabel + ": repetition sentence");
            // 1. Set ‘ConditionPossible’ and reset 'CurrentUCO'
            ConditionPossible = CurrentUCC.methods.SelectMany(x => x.instructions).ToList().Exists(x => x is Decision && CurrentLabel == x.label);
            CurrentUCO = null;
            // 2. Clear ‘CurrentCondition’
            CurrentCondition = null;
            // 3. Search for ‘label’ in ‘LabelToVF’; if ‘label’ found -> set ‘CurrentVF’ to associated ‘ViewFunction’
            if (LabelToVF.ContainsKey(CurrentLabel)) CurrentVF = LabelToVF[CurrentLabel];
            else throw new Exception("Incorrect repetition sentence label");
            SetLastPredicateTypes(PredicateType.Repetition);
        } catch (Exception e) {
            WriteErrorMessage(e);
        }
        return result;
    }

    //*****************************************************************************************************
    // 12. CONDITION SENTENCE
    //*****************************************************************************************************

    public override IntermediaryRepresentation VisitCondsentence([NotNull] RslBisParser.CondsentenceContext context)
    {
        try {
            if (Verbose) Console.WriteLine("Condition sentence: " + context.GetText());
            if (ConditionPossible == false) throw new Exception("Unexpected condition"); // ERROR HANDLING
            // 1. Create ‘Decision’ (if does not exist; set ‘Decision.label’ as ‘CurrentLabel’);
            Decision dec = (Decision) CurrentUCC.methods.SelectMany(x => x.instructions).ToList().Find(x => x is Decision && CurrentLabel == x.label);
            if (null == dec) { // not start of alternative scenario
                dec = new Decision(){label = CurrentLabel};
                // 2. If ‘CurrentCondition’ empty ->  append ‘Decision’ to ‘CurrentUCO’ else append ‘Decision’ to ‘CurrentCondition’
                if (null == CurrentCondition) CurrentUCO.instructions.Add(dec);
                else CurrentCondition.instructions.Add(dec);
                ConditionPossible = false;
            }
            // 3. Create ‘Condition’; add it to ‘Decision’
            Condition cond = new Condition(){};
            dec.conditions.Add(cond);
            // 4. Set ‘Condition’ as ‘CurrentCondition’
            CurrentCondition = cond;
            ProcessConditions(context.conditions());
        } catch (Exception e) {
            WriteErrorMessage(e);
        }
        return result;
    }

    private void ProcessConditions(RslBisParser.ConditionsContext context){
        // 5. For each ‘condition’ -> create ‘Expression’; add it to ‘Condition’;
        //    attach it to ‘DataAggregate’ based on ‘notion’; if ‘condition’ is ‘valuecondition’ -> create ‘Value’ based on ‘value’;
        //    add it to ‘Enumeration’ based on ‘notion’; attach it to ‘Expression’
        if (null == context) return;
        Expression expr = new Expression();
        CurrentCondition.expressions.Add(expr);

        RslBisParser.ContextconditionContext ccondition = context.condition().contextcondition();
        RslBisParser.ValueconditionContext vcondition = context.condition().valuecondition();

        string notionName = null != ccondition ? ObtainName(ccondition.notion()) : ObtainName(vcondition.notion());
        
        if ("ended" != notionName || null != ccondition) {       
            DataAggregate da = result.ViewModel.items.Find(x => notionName == x.name);
            if (null != da) expr.data = da;
            // ERROR HANDLING
            else throw new Exception("Notion not found");
        } else expr.data = null;

        if (null != vcondition) {
            CheckEnumeration en = result.ViewModel.enums.Find(x => notionName + " !enum" == x.name);
            if (null == en)
                if ("ended" == notionName) {
                    en = new CheckEnumeration(){name = "invoke result !enum"};
                    result.ViewModel.enums.Add(en);
                }
                else throw new Exception("Notion not checked");
            string valueName = ObtainName(vcondition.value());
            Value val = en.values.Find(x => valueName == x.name);
            if (null == val){
                val = new Value(){name = valueName, parent = en};
                en.values.Add(val);
            }
            expr.value = val;
        }
        ProcessConditions(context.conditions());
    }

    //*****************************************************************************************************
    // 13. END SENTENCE
    //*****************************************************************************************************

    public override IntermediaryRepresentation VisitResultsentence([NotNull] RslBisParser.ResultsentenceContext context)
    {
        if (Verbose) Console.WriteLine("End sentence ");
        if (null == CurrentUCO && null == CurrentCondition) throw new Exception("Unexpected end of scenario");
        // 1. Create ‘End’; append it to ‘CurrentUCO.instructions’ or ‘CurrentCondition.instructions’
        End end = new End();
        if (null == CurrentCondition) CurrentUCO.instructions.Add(end);
        else CurrentCondition.instructions.Add(end);
        // 2. Create ‘Enumeration’ (if does not exist) based on ‘CUCC.name’; add it to ‘ViewModel’
        CheckEnumeration en = result.ViewModel.enums.Find(x => CurrentUCC.name + " !result !enum" == x.name);
        string valueName = ObtainName(context.value());
        Value value = en?.values.Find(x => valueName == x.name);
        if (null == en) {
            en = new CheckEnumeration(){name = CurrentUCC.name + " !result !enum"};
            result.ViewModel.enums.Add(en);
        }
        // 3. Create ‘Value’ based on ‘value’; add it to ‘Enumeration’
        if (null == value) {
            value = new Value(){name = valueName, parent = en};
            en.values.Add(value);
        }
        end.value = value;
        return result;
    }

    //*****************************************************************************************************
    // 14. REJOIN SENTENCE
    //*****************************************************************************************************

    public override IntermediaryRepresentation VisitRejoinsentence([NotNull] RslBisParser.RejoinsentenceContext context)
    {
        // TODO - consider other than current (incompatible) Auxiliary variables state
        // (rejoin to <read> vs. rejoin to <show> vs. rejoin to <execute>)
        if (Verbose) Console.WriteLine("Rejoin sentence ");
        // Error In 1: ‘LastPredicateType’ empty or ‘System-to-Screen’ or ‘Actor-to-Data’(TODO) or ‘Repetition sentence’
        if (new List<PredicateType>{PredicateType.Show,PredicateType.Enter,PredicateType.Repetition}.Contains(LastPredicateType))
            throw new Exception("Unexpected rejoin sentence");
        // 1. Find ‘Call’ with ‘Call.label’ == ‘label’; if not found -> finish
        string rejoinLabel = context.labelref().GetText();
        List<Instruction> rejoinList = null;
        foreach (UCOperation ucop in CurrentUCC.methods){
            rejoinList = GetRejoinedInstructionList(ucop.instructions,rejoinLabel);
            if (null != rejoinList) break;
        }
        if (null == rejoinList){
            if (LabelToVF.ContainsKey(rejoinLabel)) return result;
            else throw new Exception("Incorrect rejoin label");
        }
        Call call = (Call) rejoinList.Find(x => x is Call && rejoinLabel == x.label);
        // 2. For each further ‘Instruction’ in ‘Call.parent.instructions’ following and including the current ‘Call’ ->
        //    { If ‘CurrentCondition’ empty ->  append further ‘Instruction’ to ‘CurrentUCO’ 
        //      else append further ‘Instruction’ to ‘CurrentCondition’ }
        List<Instruction> instructions = null == CurrentCondition ? CurrentUCO.instructions : CurrentCondition.instructions;
        for (int i = rejoinList.IndexOf(call); i < rejoinList.Count(); i++)
            instructions.Add(rejoinList[i]);
        return result;
    }
    
    private List<Instruction> GetRejoinedInstructionList(List<Instruction> instructions, string rejoinLabel){
        foreach (Instruction instr in instructions){
            if (instr is Call && rejoinLabel == instr.label) return instructions;
            else if (instr is Decision dec) 
                foreach (Condition cond in dec.conditions){
                    List<Instruction> decList = GetRejoinedInstructionList(cond.instructions, rejoinLabel);
                    if (null != decList) return decList;
                }
        }
        return null;
    }

    //*****************************************************************************************************
    // 15. DATA NOTION
    //*****************************************************************************************************

    public override IntermediaryRepresentation VisitDatanotion([NotNull] RslBisParser.DatanotionContext context)
    {
        if (Verbose) Console.WriteLine("Data notion: " + context.name().GetText());
        string notionName = ObtainName(context.name());
        DataAggregate da = result.ViewModel.items.Find(da => notionName == da.name);
        if (null == da) {
            da = new DataAggregate(){name = notionName};
            result.ViewModel.items.Add(da);
        }
        AddDataItems(da,context.attributes());
        return result;
    }

    private void AddDataItems(DataAggregate da, RslBisParser.AttributesContext context){
        if (null == context) return;
        string itemName = ObtainName(context.attribute().name());
        string typeName = null;
        TypeKind tk;
        if (null != context.attribute().datatype()) {
            typeName = ObtainName(context.attribute().datatype()); tk = TypeKind.Primitive;
        } else if (null != context.attribute().notion()) {
            typeName = ObtainName(context.attribute().notion()); tk = TypeKind.Simple;
        } else if (null != context.attribute().multnotion()) {
            typeName = ObtainName(context.attribute().multnotion().notion()); tk = TypeKind.Multiple;
        } else throw new Exception("Critical error");
        DataAggregate baseType = null;
        if (TypeKind.Primitive != tk) {
            baseType = result.ViewModel.items.Find(t => typeName == t.name);
            if (null == baseType) throw new Exception("Unknown domain notion: " + typeName);
        }
        DataItem di = new DataItem(){ name = itemName, type = typeName, typeKind = tk, baseType = baseType};
        da.fields.Add(di);
        AddDataItems(da,context.attributes());
    }

    //*****************************************************************************************************
    // 15. TRIGGER NOTION
    //*****************************************************************************************************

    public override IntermediaryRepresentation VisitTriggernotion([NotNull] RslBisParser.TriggernotionContext context)
    {
        string trgType = ObtainName(context.triggertype());
        SetTriggerDetails(trgType, context.namesandlabels());
        return result;
    }

    private void SetTriggerDetails(string trgType, RslBisParser.NamesandlabelsContext context){
        if (null == context) return;
        string trgName = ObtainName(context.name());
        Trigger trg = result.ViewFunctions.SelectMany(vf => vf.triggers).ToList().Find(t => trgName == t.name);
        if (null == trg) throw new Exception("Unexpected trigger definition");
        if (null != trgType) trg.type = trgType;
        if (null != context.uilabel()) trg.label = ObtainName(context.uilabel());
        SetTriggerDetails(trgType, context.namesandlabels());
    }

    //*****************************************************************************************************
    // 15. VIEW NOTION
    //*****************************************************************************************************

    public override IntermediaryRepresentation VisitViewnotion([NotNull] RslBisParser.ViewnotionContext context)
    {
        string viewType = ObtainName(context.viewtype());
        SetViewnotionDetails(viewType, context.namesandlabels());
        return result;   
    }

    private void SetViewnotionDetails(string viewType, RslBisParser.NamesandlabelsContext context){
        if (null == context) return;
        string viewName = ObtainName(context.name());
        ViewFunction view = result.ViewFunctions.Find(vf => viewName == vf.name);
        if (null == view) throw new Exception("Unexpected view notion definition");
        if (null != viewType) view.type = viewType;
        if (null != context.uilabel()) view.label = ObtainName(context.uilabel());
        SetViewnotionDetails(viewType, context.namesandlabels());
    }

    //*****************************************************************************************************
    // ERROR HANDLING
    //*****************************************************************************************************

    public override IntermediaryRepresentation VisitSentence([NotNull] RslBisParser.SentenceContext context)
    {
        try {
            return base.VisitSentence(context);
        } catch (Exception e) {
            WriteErrorMessage(e);
            return result;
        }
    }

    public override IntermediaryRepresentation VisitAltsentence([NotNull] RslBisParser.AltsentenceContext context)
    {
        try {
            return base.VisitAltsentence(context);
        } catch (Exception e) {
            WriteErrorMessage(e);
            return result;
        }
    }

    public override IntermediaryRepresentation VisitEndsentence([NotNull] RslBisParser.EndsentenceContext context)
    {
        try {
            return base.VisitEndsentence(context);
        } catch (Exception e) {
            WriteErrorMessage(e, "after " + CurrentLabel);
            return result;
        }
    }

    public override IntermediaryRepresentation VisitSystemstep([NotNull] RslBisParser.SystemstepContext context)
    {
        if (null == CurrentUCO && null == CurrentCondition)
            throw new Exception(null != CurrentVF ? "\'Actor-to\' sentence expected" : "Unexpected \'System-to\' sentence");
        return base.VisitSystemstep(context);
    }

    public override IntermediaryRepresentation VisitUserstep([NotNull] RslBisParser.UserstepContext context)
    {
        if (!FirstSentence && null == CurrentVF)
            throw new Exception(null == CurrentUCO || null == CurrentCondition ? "Unexpected \'Actor-to\' sentence" : "\'System-to\' sentence expected");
        return base.VisitUserstep(context);
    }

    private void WriteErrorMessage(Exception e, string altlabel = null){
        string label = altlabel ?? CurrentLabel ?? "precondition";
        Console.WriteLine(">>>>> Error: " + e.Message + " in use case \"" + CurrentUCC.name + "\", sentence - " + label);
        Console.WriteLine(e.ToString());
    }
}