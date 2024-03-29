///////////////////////////////////////////////////////////
//  COperation.cs
//  Implementation of the Class COperation
//  Generated by Enterprise Architect
//  Created on:      23-sty-2024 15:01:54
//  Original author: smial
///////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Linq;
namespace CodeModel {
	public class COperation : Operation {

		public List<DataAggregate> data = new List<DataAggregate>();
		public UCOperation invoked;
		public UCOperation returnTo;

		public COperation(){}

        public override string GetElemName(){
            return Utils.ToCamelCase(name.Replace("!",""));
        }

        public override string ToCode(int tabs){
			string ts = Utils.GetTabString(tabs);
			bool isCondition = null != returnType;
			// function invokeCheckFindClient(): boolean {
			string code = ts + "function " + GetElemName() + "()";
			if (isCondition) code += ": " + returnType;
			code += " {\n";
			// let role: Role = Object.create(state.role);
			foreach (DataAggregate da in data){
				code += "\t" + ts + "let " + da.GetVarName() + ": " + da.GetElemName() + 
						" = Object.create(state." + da.GetVarName() + ");\n";
			}
			// return findClient.PreconditionCheck(role);
			code += ts + "\t" + (isCondition ? "return " : "") + invoked.uc.GetVarName() + "." + invoked.GetElemName();
			code += "(" + string.Join(", ", data.Select(da => da.GetVarName()));
			if (null != returnTo)
				code += (0 == data.Count ? "" : ", ") + returnTo.uc.GetVarName() + "." + returnTo.GetElemName();
			code += ");\n";
			code += ts + "}\n";
            return code;
        }
	}
}