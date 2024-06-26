///////////////////////////////////////////////////////////
//  Operation.cs
//  Implementation of the Class Operation
//  Generated by Enterprise Architect
//  Created on:      23-sty-2024 15:01:55
//  Original author: smial
///////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CodeModel{
	public abstract class Operation : Generator {
		public string returnType = null;
		public List<Parameter> parameters = new List<Parameter>();

		public Operation(){}

		protected string GetParametersCode(bool var = false, bool bare = false){
			IEnumerable<string> pars = var ? parameters.Select(p => p.ToVarCode()) : parameters.Select(p => p.ToCode());
			return (bare ? "" : "(") + string.Join(", ", pars) + (bare ? "" : ")");
		}

		public string GetVarParametersCode(){
			return GetParametersCode(true);
		}

		public string GetBareParametersCode(){
			return GetParametersCode(true,true);
		}

		public string GetReturnTypeElemName(){
			List<string> types = new List<string>(){"bigint", "boolean"};
			if (types.Contains(returnType)) return returnType;
			return Utils.ToPascalCase(returnType.Replace("!", ""));
		}

		public string GetReturnTypeVarName(){
			return Utils.ToCamelCase(returnType.Replace("!", ""));
		}
    }
}