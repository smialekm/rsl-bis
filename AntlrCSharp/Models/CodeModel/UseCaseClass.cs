///////////////////////////////////////////////////////////
//  UseCaseClass.cs
//  Implementation of the Class UseCaseClass
//  Generated by Enterprise Architect
//  Created on:      23-sty-2024 15:01:55
//  Original author: smial
///////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Linq;
namespace CodeModel {
	public class UseCaseClass : ClassFileGenerator {
		public List<UCOperation> methods = new List<UCOperation>();
		public List<PresenterClass> presenters = new List<PresenterClass>();
		public List<ServiceInterface> services = new List<ServiceInterface>();

		public UseCaseClass(){}

        public override string GetElemName(){
            return "UC" + Utils.ToPascalCase(name);
        }

        public override string GetVarName(){
            return Utils.ToCamelCase(name);
        }

        public override string ToCode(int tabs){
 			string ts = Utils.GetTabString(tabs);
            // CODE: export class UCShowClientList {
            string code = ts + "export class " + GetElemName() + "{\n";
            //   CODE: pClientListWindow: PClientListWnd;
            code += "\t" + ts + string.Join(";\n\t" + ts, presenters.Select(p => p.GetVarName() + ": " + p.GetElemName())) + ";\n";
            //   CODE: iCl: IClients;
            code += "\t" + ts + string.Join(";\n\t" + ts, services.Select(s => s.GetVarName() + ": " + s.GetElemName())) + ";\n\n";
            //   CODE: constructor(clw: PClientListWnd, mm: PMainMenu, cl: IClients) {
            code += "\t" + ts + "constructor(";
            code += string.Join(", " + ts, presenters.Select(p => p.GetVarName() + ": " + p.GetElemName()));
            if (services.Count > 0)
                code += ", " + string.Join(", " + ts, services.Select(s => s.GetVarName() + ": " + s.GetElemName()));
            code += ") {\n";
            //   CODE: this.pCLW = clw;
            code += string.Join("", presenters.Select(p => ts + "\t\tthis." + p.GetVarName() + " = " + p.GetVarName() + ";\n"));
            code += string.Join("", services.Select(s => ts + "\t\tthis." + s.GetVarName() + " = " + s.GetVarName() + ";\n"));
            //   CODE: }
            code += "\t" + ts + "}\n\n";
            code += string.Join("\n", methods.Select(m => m.ToCode(tabs + 1)));
            // CODE: }
            code += ts + "}";
            return code;
        }

        protected override string GetFileName(){
            return "UC " + name;
        }
    }
}