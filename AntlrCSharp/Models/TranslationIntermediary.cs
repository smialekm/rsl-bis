using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeModel;

public class IntermediaryRepresentation {
    public List<ViewFunction> ViewFunctions {get; set;} = new List<ViewFunction>();
    public List<ControllerFunction> ControllerFunctions {get; set;} = new List<ControllerFunction>();
    public List<UseCaseClass> UseCaseClasses {get; set;} = new List<UseCaseClass>();
    public List<PresenterClass> PresenterClasses {get; set;} = new List<PresenterClass>();
    public List<ServiceInterface> ServiceInterfaces {get; set;} = new List<ServiceInterface>();
    public ViewModel ViewModel {get; set;} = new ViewModel();

    public void ToMainFile(string path){
        string code = "";
        code += string.Join("", PresenterClasses.Select(pc => "conts " + pc.GetVarName() + ": " + pc.GetElemName() + " = new " + pc.GetElemName() + "();\n"));
        code += string.Join("", ServiceInterfaces.Select(si => "conts " + si.GetVarName() + ": " + si.GetElemName() + " = new " + si.GetElemName() + "();\n"));
        foreach (UseCaseClass ucc in UseCaseClasses){
            code += "";
        }
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
			File.WriteAllText(path + "\\" + "App.tsx", code);
    }
}

public enum PredicateType {
    None,
    Show,
    Read,
    Update,
    Delete,
    Check,
    Execute,
    Select,
    Enter,
    Invoke,
    Repetition
}