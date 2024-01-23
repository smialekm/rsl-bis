using System.Collections.Generic;
using CodeModel;

public class IntermediaryRepresentation {
    public List<ViewFunction> ViewFunctions {get; set;}
    public List<ControllerFunction> ControllerFunctions {get; set;}
    public List<UseCaseClass> UseCaseClasses {get; set;} = new List<UseCaseClass>();
    public List<PresenterClass> PresenterClasses {get; set;}
    public List<ServiceInterface> ServiceInterfaces {get; set;}
    public ViewModel ViewModel {get; set;}
}

public enum PredicateType {
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