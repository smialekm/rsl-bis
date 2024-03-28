///////////////////////////////////////////////////////////
//  Call.cs
//  Implementation of the Class Call
//  Generated by Enterprise Architect
//  Created on:      23-sty-2024 15:01:54
//  Original author: smial
///////////////////////////////////////////////////////////

using System.Collections.Generic;

namespace CodeModel {
	public class Call : Instruction {

		public UCCallableOperation operation;
		public Value value = null;

		public Call(){}

        public string GetElemName(){
            return operation.GetElemName();
        }

        public override string ToCode(int tabs = 0){
            return ToFullCode(tabs, false);
        }

        protected string ToFullCode(int tabs = 0, bool var = false){
			string ts = Utils.GetTabString(tabs);
			string code = ts;
			// CODE: this.pCLW.showUpdatedClientListWnd(list);
			if (operation is SOperation op) {
				if (!var && (PredicateType.Read == op.type || PredicateType.Check == op.type))
					code += /* op.GetReturnTypeElemName() + */ "let " + op.GetReturnTypeVarName() + " = "; // TODO - repeated return variable names
				code += "this." + op.si.GetVarName();
			} else if (operation is POperation pop)
				code += "this." + pop.pres.GetVarName();
			else if (operation is UCOperation ucop)
				code += "this." + ucop.uc.GetVarName() + "?";
			else throw new System.Exception("Critical compilation failure");
			code += "." + operation.GetElemName();
			code += operation is UCOperation ? "(" + operation.GetBareParametersCode() + 
					(0 < operation.parameters.Count ? ", " : "") + "returnTo, this)" : operation.GetVarParametersCode();
			if (var) code += null == value ? "" : " == " + value.parent.GetElemName() + "." + value.GetElemName();
			else code += ";";
            return code;
        }

		public string ToVarCode(int tabs = 0){
			return ToFullCode(tabs, true);
		}
    }
}