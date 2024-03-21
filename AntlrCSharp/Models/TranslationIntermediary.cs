using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.XPath;
using Antlr4.Runtime.Atn;
using CodeModel;

public class IntermediaryRepresentation {
    public List<ViewFunction> ViewFunctions {get; set;} = new List<ViewFunction>();
    public List<ControllerFunction> ControllerFunctions {get; set;} = new List<ControllerFunction>();
    public List<UseCaseClass> UseCaseClasses {get; set;} = new List<UseCaseClass>();
    public List<PresenterClass> PresenterClasses {get; set;} = new List<PresenterClass>();
    public List<ServiceInterface> ServiceInterfaces {get; set;} = new List<ServiceInterface>();
    public ViewModel ViewModel {get; set;} = new ViewModel();

    private string GetImports(){
            string code = "import React from \"react\";\n";
            code += "import { useReducer } from \"react\";\n";
            code += "import { AppState, ScreenId } from \"./viewmodel/ViewModel\";\n";
            foreach (ViewFunction vf in ViewFunctions)
                code += "import " + vf.GetElemName() + " from \"./view/" + vf.GetElemName() + "\";\n";
            foreach (PresenterClass pc in PresenterClasses)
                code += "import { " + pc.GetElemName() + " } from \"./view/presenters/" + pc.GetElemName() + "\";\n";
            foreach (UseCaseClass ucc in UseCaseClasses)
                code += "import { " + ucc.GetElemName() + " } from \"./usecases/" + ucc.GetElemName() + "\";\n";
            foreach (ServiceInterface si in ServiceInterfaces)
                code += "import { " + si.GetElemName() + ", " + si.GetElemName().Substring(1) + "Proxy } from \"./services/" + si.GetElemName() + "\";\n";
            return code + "\n";
        }

    public void ToMainFile(string path){
        string code = GetImports();
        code += string.Join("", PresenterClasses.Select(pc => "const " + pc.GetVarName() + ": " + pc.GetElemName() + " = new " + pc.GetElemName() + "();\n")) + "\n";
        code += string.Join("", ServiceInterfaces.Select(si => "const " + si.GetVarName() + ": " + si.GetElemName() + " = new " + si.GetSvcName() + "();\n")) + "\n";

        UseCaseClass startClass = null;
        foreach (UseCaseClass ucc in UseCaseClasses){
            if ("Start" == ucc.name) startClass = ucc;
            code += "const " + ucc.GetVarName() + ": " + ucc.GetElemName() + " = new " + ucc.GetElemName() + "(";
            code += ucc.GetParams() + ");\n";
        }

        foreach (UseCaseClass ucc in UseCaseClasses)
            foreach (UseCaseClass inv in ucc.invoked)
                code += ucc.GetVarName() + "." + inv.GetVarName() + " = " + inv.GetVarName() + ";\n";

        code += "\nfunction switchView(state: AppState, action: ScreenId) {\n";
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

        code += string.Join("", PresenterClasses.Select(pc => "\t" + pc.GetVarName() + ".injectGlobalUpdateView(globalUpdateView);\n")) + "\n";

        code += "\n\tif (state.screen === ScreenId.START) start.selectApplication();\n\n";


        code += "\treturn (\n";
        code += "\t\t<div className=\"App\">\n";
        foreach (ViewFunction vf in ViewFunctions){
          code += "\t\t\t{" + vf.GetElemName() + "(state.screen === ScreenId." + vf.GetElemName().ToUpper().Substring(1) + ", ";
          code += vf.presenter.GetVarName();
          if (null != vf.controller.useCase)
            code += ", " + vf.controller.useCase.GetVarName();
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