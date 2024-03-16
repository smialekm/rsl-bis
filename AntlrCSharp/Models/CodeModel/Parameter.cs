///////////////////////////////////////////////////////////
//  DataItem.cs
//  Implementation of the Class Parameter
//  Generated by Enterprise Architect
//  Created on:      23-sty-2024 15:01:54
//  Original author: smial
///////////////////////////////////////////////////////////

namespace CodeModel {
	public class Parameter {
		public string type;
		public bool isAttribute = false;

		public Parameter(){}

		public string ToCode(int i = 0, bool var = false){
			string code;
			if (type.Contains("@")) code = "result";
			else code = Utils.ToCamelCase(type.Replace("!", "")) + (i>0 ? i.ToString() : "");
			if (!var) code += ": " + ToTypeCode();
			return code;
		}

		public string ToVarCode(int i = 0){
			string code = (isAttribute) ? "this." : "";
			return code + ToCode(i, true);
		}
		
		public string ToTypeCode(){
			return Utils.ToPascalCase(type.Replace("@","at").Replace("!", ""));
		}
	}
}