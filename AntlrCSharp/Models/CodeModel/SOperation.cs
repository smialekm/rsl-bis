///////////////////////////////////////////////////////////
//  SOperation.cs
//  Implementation of the Class SOperation
//  Generated by Enterprise Architect
//  Created on:      23-sty-2024 15:01:55
//  Original author: smial
///////////////////////////////////////////////////////////

namespace CodeModel {
	public class SOperation : UCCallableOperation {
		public PredicateType type = PredicateType.None;
		public ServiceInterface si = null;

		public SOperation(){}

        public override string GetElemName(){
            return Utils.ToCamelCase(name.Replace("!", ""));
        }

        public override string ToCode(int tabs){
			string ts = Utils.GetTabString(tabs);
			string code = ts + GetElemName() + GetParametersCode() + (null == returnType ? "" : ": " + Utils.ToPascalCase(returnType));
			return code;
        }
	}
}