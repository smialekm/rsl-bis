///////////////////////////////////////////////////////////
//  ViewFunction.cs
//  Implementation of the Class ViewFunction
//  Generated by Enterprise Architect
//  Created on:      23-sty-2024 15:01:55
//  Original author: smial
///////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Linq;
namespace CodeModel {
	public class ViewFunction : FileGenerator {
		public List<DataAggregate> data = new List<DataAggregate>();
        public List<DataAggregate> editable = new List<DataAggregate>();
		public ControllerFunction controller;
		public PresenterClass presenter;
		public List<Trigger> triggers = new List<Trigger>();
        string type = "form";
        string label = null;

		public ViewFunction(){}

        public override string GetElemName(){
            return "V" + Utils.ToPascalCase(name);
        }

        public override string ToCode(int tabs){
 			string ts = Utils.GetTabString(tabs);
            // CODE: export default function VClientListWnd(
            string code = ts + "export default function " + GetElemName() + "(\n";
            //   CODE: isActive: boolean,
            code += ts + "\tisActive: boolean,\n";
            //   CODE:   pCLW: PClientListWnd,
            code += ts + "\t" + presenter.GetVarName() + ": " + presenter.GetElemName() + ",\n";
            //   ucSCL: UCShowClientList
            code += string.Join(",\n", controller.useCases.Select(u => ts + "\t" + u.GetVarName() + ": " + u.GetElemName())) + "\n";
            // CODE: ) {
            code += ts + ") {\n";
            //   CODE: const emptyState: ClientListWndState = new ClientListWndState();
            string windowName = GetElemName().Substring(1);
            code += ts + "\tconst emptyState: " + windowName + "State = new " + windowName + "State();\n";
            //   CODE: const [viewState, viewUpdate] = useReducer(updateClientListWnd, emptyState);
            code += ts + "\tconst [viewState, viewUpdate] = useReducer(update" + windowName + ", emptyState);\n\n";
            //   CODE: pCLW.injectDataHandles(clwData, clwUpdateView);
            code += ts + "\t" + presenter.GetVarName() + ".injectDataHandles(viewState, viewUpdate);\n\n"; 
            //   CODE: if (!isActive) return;
            code += ts + "\tif (!isActive) return;\n\n";
            //   CODE: const [selectAdd, selectBack] = CClientListWnd(viewState, ucSCL);
            code += ts + "\tconst [" + string.Join(", ", controller.functions.Select(f => f.GetElemName())) + "] = " +
                    controller.GetElemName() + "(viewState, " + string.Join(",", controller.useCases.Select(u => u.GetVarName())) + ");\n";
            //   CODE:   return (
            code += ts + "\treturn (\n";
            //     CODE: <div className="ClientListWnd">
            code += ts + "\t\t<div className=\"" + windowName + "\">\n";
            
            code += 0 == data.Count() ? "" : string.Join("", data.Select(da => da.ToCode(tabs + 3) + "\n"));

            code += 0 == triggers.Count() ? "" : string.Join("", triggers.Select(tr => tr.ToCode(tabs + 3) + "\n"));

            //     CODE: </div>
            code += ts + "\t\t</div>\n";
            code += ts + "\t);\n";
            code += ts + "}";
            return code;
        }

        protected override string GetFileName(){
            return "V " + name;
        }
    }
}