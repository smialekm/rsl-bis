using System.Collections.Generic;
using CodeModel;

public class IntermediaryRepresentation {
    public List<ViewFunction> ViewFunctions {get; set;} = new List<ViewFunction>();
    public List<ControllerFunction> ControllerFunctions {get; set;} = new List<ControllerFunction>();
    public List<UseCaseClass> UseCaseClasses {get; set;} = new List<UseCaseClass>();
    public List<PresenterClass> PresenterClasses {get; set;} = new List<PresenterClass>();
    public List<ServiceInterface> ServiceInterfaces {get; set;} = new List<ServiceInterface>();
    public ViewModel ViewModel {get; set;} = new ViewModel();
}

public enum PredicateType {
    None,
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