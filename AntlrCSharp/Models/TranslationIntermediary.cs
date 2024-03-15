using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.XPath;
using CodeModel;

public class IntermediaryRepresentation {
    public List<ViewFunction> ViewFunctions {get; set;} = new List<ViewFunction>();
    public List<ControllerFunction> ControllerFunctions {get; set;} = new List<ControllerFunction>();
    public List<UseCaseClass> UseCaseClasses {get; set;} = new List<UseCaseClass>();
    public List<PresenterClass> PresenterClasses {get; set;} = new List<PresenterClass>();
    public List<ServiceInterface> ServiceInterfaces {get; set;} = new List<ServiceInterface>();
    public ViewModel ViewModel {get; set;} = new ViewModel();

    private string GetImports(){
            string code = "import { useReducer } from \"react\";\n";
            code += "import { AppState, ScreenId } from \"../viewmodel/ViewModel\";\n";
            foreach (ViewFunction vf in ViewFunctions)
                code += "import { " + vf.GetElemName() + " } from \"./view/" + vf.GetElemName() + "\";\n";
            foreach (PresenterClass pc in PresenterClasses)
                code += "import { " + pc.GetElemName() + " } from \"./view/" + pc.GetElemName() + "\";\n";
            foreach (UseCaseClass ucc in UseCaseClasses)
                code += "import { " + ucc.GetElemName() + " } from \"../usecases/" + ucc.GetElemName() + "\";\n";
            return code + "\n";
        }

    public void ToMainFile(string path){
        string code = GetImports();
        code += string.Join("", PresenterClasses.Select(pc => "conts " + pc.GetVarName() + ": " + pc.GetElemName() + " = new " + pc.GetElemName() + "();\n")) + "\n";
        code += string.Join("", ServiceInterfaces.Select(si => "conts " + si.GetVarName() + ": " + si.GetElemName() + " = new " + si.GetSvcName() + "();\n")) + "\n";

        foreach (UseCaseClass ucc in UseCaseClasses){
            code += "const " + ucc.GetVarName() + ": " + ucc.GetElemName() + " = new " + ucc.GetElemName() + "(";
            code += ucc.GetParams() + ");\n";
        }
        code += "\nconst ucStart: UCStart = new UCStart();\n";

        code += "function switchView(state: AppState, action: ScreenId) {\n";
        code += "\tlet newState = { ...state };\n";
        code += "\tswitch (action) {\n";
        CheckEnumeration screenId = ViewModel.enums.Find(id => "screen id" == id.name);
        if (null == screenId) throw new System.Exception("Critical error - no ScreenId defined");
        foreach (Value id in screenId.values){
          code += "\t\tcase ScreenId." + id.GetElemName() + ":\n";
          code += "\t\t\tnewState.screen = ScreenId." + id.GetElemName() + ";\n";
          code += "\t\t\tbreak;\n";
        }
        code += "\t}\n\treturn newState;\n}\n";

        code += "export default function App() {\n";
        code += "\tconst [state, globalUpdateView] = useReducer(switchView, {\n";
        code += "\t\tscreen: ScreenId.START,\n\t});\n\n";

        code += string.Join("", PresenterClasses.Select(pc => "\t" + pc.GetElemName() + ".injectGlobalUpdateView(globalUpdateView);\n")) + "\n";

        code += "\treturn (\n";
        code += "\t\t<div className=\"App\">\n";
        code += "\t\t\t{ucStart.selectApplication()}\n";
        foreach (ViewFunction vf in ViewFunctions){
          code += "\t\t\t{" + vf.GetElemName() + "(state.screen === ScreenId." + vf.GetElemName().ToUpper().Substring(1) + ", ";
          code += vf.presenter.GetVarName();
          if (0 < vf.controller.useCases.Count) {
            code += ", ";
            code += string.Join(", ", vf.controller.useCases.Select(uc => uc.GetVarName()));
          }
          code += ")}\n";
        }
        
        code += "\t\t</div>\n\t);\n}\n";

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