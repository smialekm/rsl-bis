///////////////////////////////////////////////////////////
//  ViewModel.cs
//  Implementation of the Class ViewModel
//  Generated by Enterprise Architect
//  Created on:      23-sty-2024 15:01:55
//  Original author: smial
///////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Linq;
namespace CodeModel {
	public class ViewModel : FileGenerator {
		public List<DataAggregate> items = new List<DataAggregate>();
		public List<CheckEnumeration> enums = new List<CheckEnumeration>();
        public List<EnumUnion> unions = new List<EnumUnion>();

		public ViewModel(){}

        public override string GetElemName(){
            return Utils.ToPascalCase(name);
        }

        public override string ToCode(int tabs){
 			string ts = Utils.GetTabString(tabs);
            string code = string.Join("", enums.Select(en => en.ToCode(tabs) + "\n"));
            code += string.Join("", items.Select(da => da.ToCode(tabs) + "\n"));
            code += string.Join("", unions.Select(eu => eu.ToCode(tabs) + "\n"));
            return code;
        }

        protected override string GetFileName(){
            return "View Model";
        }
    }
}