using System;
using System.Collections.Generic;
using System.Data;
using CodeModel;

public class TranslationIntermediary {
    // Intermediary representation
    List<ViewFunction> ViewFunctions {get; set;}
    List<ControllerFunction> ControllerFunctions {get; set;}
    List<UseCaseClass> UseCaseClasses {get; set;}
    List<PresenterClass> PresenterClasses {get; set;}
    List<ServiceInterface> ServiceInterfaces {get; set;}
    ViewModel ViewModel {get; set;}
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
}

enum PredicateType {
    Show,
    Create, 
    Read,
    Update,
    Delete,
    Check,
    Execute,
    Select,
    Enter,
    Invoke
}