///////////////////////////////////////////////////////////
//  UCOperation.cs
//  Implementation of the Class UCOperation
//  Generated by Enterprise Architect
//  Created on:      23-sty-2024 15:01:55
//  Original author: smial
///////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
namespace CodeModel {
	public class UCOperation : Operation {
		public List<Instruction> instructions = new List<Instruction>();
		public UseCaseClass uc = null;
        public bool initial = false;

		public UCOperation(){}

        public override string GetElemName(){
            return Utils.ToCamelCase(name.Replace("@","at").Replace("!", ""));
        }

        public override string ToCode(int tabs){
			string ts = Utils.GetTabString(tabs);
            // CODE: showClientListSelected() {
            string code = ts + GetElemName() + "(" + GetParametersCode(false, true);
            if (initial)  code += (0 != parameters.Count ? ", " : "") + "returnTo?: Function";
            code += ")" + (!string.IsNullOrEmpty(returnType) ? ": " + returnType : "") + " {\n";
            if (initial){
            //   CODE: if (null != returnTo) this.returnTo = returnTo;
                code += ts + "\tif (undefined != this.returnTo) this.returnTo = returnTo;\n";
            //   CODE: this.clientType = clientType;
                code += string.Join("", parameters.Select( p => ts + "\tthis." + p.ToVarCode() + " = " + p.ToVarCode() + ";\n" ));
            }
            if (string.IsNullOrEmpty(returnType))
                foreach (Instruction instr in instructions)
                    code += instr.ToCode(tabs + 1) + "\n";
            else if ("boolean" == returnType) {
                try {
                    code += ts + "\treturn " + string.Join(" && ", instructions.Select(i => ((Call) i).ToVarCode())) + ";\n";
                } catch(InvalidCastException) {
                    throw new Exception("Critical compilation error");
                }
            } else throw new Exception("Critical compilation error");
            // CODE: }
            code += ts + "}\n";
            return code;
        }
	}
}