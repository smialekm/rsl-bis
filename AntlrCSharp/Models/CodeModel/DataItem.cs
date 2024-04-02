///////////////////////////////////////////////////////////
//  DataItem.cs
//  Implementation of the Class DataItem
//  Generated by Enterprise Architect
//  Created on:      23-sty-2024 15:01:54
//  Original author: smial
///////////////////////////////////////////////////////////

using System.ComponentModel;
using System.Xml.XPath;

namespace CodeModel {
	public class DataItem : NamedElement {
		public string type;
		public TypeKind? typeKind = null;
		public DataAggregate baseType = null;
		public bool editableStateItem = false;

		public DataItem(){}

		public string GetElemName(){
			return Utils.ToPascalCase(name);
		}

		public string GetVarName(){
			return Utils.ToCamelCase(name);
		}

		public string GetTypeVarName(){
			return Utils.ToCamelCase(type);
		}

		public string GetTypeName(){
			return Utils.ToPascalCase(type);
		}

		public string ToHtml(string parentPath, bool editable, int tabs = 0, int hLevel = 4){
			if (6 < hLevel) throw new System.Exception("To many levels of indentation in " + parentPath);
			string ts = Utils.GetTabString(tabs);
			string code = "";
			if (TypeKind.Primitive == typeKind) {
				string itemType = "text"; // TODO switch (other item types)
				bool isNotString = "text" != type;
				code = ts + "<label>" + name + "</label>\n";
				code += ts + "<input\n";
				code += ts + "\ttype=\"" + itemType + "\"\n";
				code += ts + "\tvalue={viewState." + parentPath + "." + GetVarName() + (isNotString ? ".toString()" : "") + "}\n";
				code += ts + "\tonChange={(e) => {\n";
				code += ts + "\t\tviewState." + parentPath + "." + GetVarName() + " = " + 
								(isNotString ? GetCodeType(true) + "(" : "") + "e.target.value" + (isNotString ? ")" : "") + ";\n";
				code += ts + "\t\tupdateView(\"" + parentPath.Replace(".", "_") + "_" + name + "\")\n";
				code += ts + "\t}}\n";
				code += ts + "/> <br />\n";
			} else if (null != baseType){
				if (TypeKind.Simple == typeKind) {
					code = ts + "<h" + hLevel + ">" + name + "</h" + hLevel + ">\n";
					code += baseType.ToHtml(editable, tabs, hLevel, parentPath + "." + GetVarName());
				} else if (TypeKind.Multiple == typeKind) {
					code = baseType.ToHtmlTable(editable, parentPath + "." + GetVarName(), tabs);
				} else throw new System.Exception("Critical error");
			}
			return code;
		}

		public string ToCode(int tabs = 0, bool forceSimple = false){
			string ts = Utils.GetTabString(tabs);
			string code = ts + (forceSimple ? GetTypeVarName() : GetVarName()) + ": ";
			if (TypeKind.Primitive != typeKind) {
				string typeString = Utils.ToPascalCase(type) + (TypeKind.Simple == typeKind || forceSimple ? "" : "[]");
				code += typeString + " = ";
				if ("ScreenId" == typeString) code += typeString + ".START";
				else code += (TypeKind.Simple == typeKind || forceSimple) ? "new " + typeString + "()" : "[]";
				if (editableStateItem && baseType.fields.Exists(di => TypeKind.Multiple == di.typeKind)) {
					code += ";\n" + code.Replace(Utils.ToCamelCase(name), Utils.ToCamelCase("base " + name));
					foreach (DataItem di in baseType.fields)
						if (TypeKind.Multiple == di.typeKind)
							code += ";\n" + di.ToCode(tabs,true).Replace(":", "?:").Replace(";","");
				}
			}
			else {
				code += GetCodeType() + " = ";
				switch (type) {
					case "integer": code += "BigInt(0)";
					break;
					case "float": code += "0";
					break;
					case "text": code += "\"\"";
					break;
					case "boolean": code += "false";
					break;
					case "time":
					case "date": code += "new Date()";
					break;
				}
			}
			return code + ";";
		}

		public string GetCodeType(bool forConstructor = false){
			switch (type) {
				case "integer": return forConstructor ? "BigInt" : "bigint";
				case "float": return forConstructor ? "Number" : "number";
				case "text": return forConstructor ? "String" : "string";
				case "boolean": return forConstructor ? "Boolean" : "boolean";
				case "time":
				case "date": return "Date";
			}
			return "";
		}
	}

	public enum TypeKind {
		Primitive,
		Simple,
		Multiple
	}
}