///////////////////////////////////////////////////////////
//  DataAggregate.cs
//  Implementation of the Class DataAggregate
//  Generated by Enterprise Architect
//  Created on:      23-sty-2024 15:01:54
//  Original author: smial
///////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeModel {
	public class DataAggregate : NamedElement {
		public List<DataItem> fields = new List<DataItem>();
		public CheckEnumeration enumer = null;

		public string GetElemName(){
			return Utils.ToPascalCase(name);
		}

		public string GetVarName(){
			return Utils.ToCamelCase(name);
		}

        public string ToHtml(bool editable, int tabs = 0, int hLevel = 3){
 			string ts = Utils.GetTabString(tabs);
			string varName = GetVarName();
            string code = "";
			if (3 >= hLevel)
				code += ts + "<h" + hLevel + ">" + name + "</h" + hLevel+ ">\n";
			foreach (DataItem item in fields)
				code += item.ToHtml(varName, editable, tabs + 1, hLevel + 1);
			return code;
        }

		public string ToCode(int tabs){
			string ts = Utils.GetTabString(tabs);
            string code = ts + "export type " + GetElemName() + " = {\n";
			code += string.Join("", fields.Select(di => di.ToCode(tabs+1) + "\n"));
			code += ts + "}\n";
			return code;
		}

        public DataAggregate(){}
	}
}