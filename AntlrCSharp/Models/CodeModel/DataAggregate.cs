///////////////////////////////////////////////////////////
//  DataAggregate.cs
//  Implementation of the Class DataAggregate
//  Generated by Enterprise Architect
//  Created on:      23-sty-2024 15:01:54
//  Original author: smial
///////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;

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

        public string ToCode(int tabs){
 			string ts = Utils.GetTabString(tabs);
            string code = ts + "<h2>" + name + "</h2>\n";
			foreach (DataItem item in fields)
				code += ts + "\t<label>" + item.name + "</label>\n";
			return code;
        }

        public DataAggregate(){}
	}
}