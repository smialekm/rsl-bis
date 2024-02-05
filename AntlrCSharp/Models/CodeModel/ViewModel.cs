///////////////////////////////////////////////////////////
//  ViewModel.cs
//  Implementation of the Class ViewModel
//  Generated by Enterprise Architect
//  Created on:      23-sty-2024 15:01:55
//  Original author: smial
///////////////////////////////////////////////////////////

using System.Collections.Generic;
namespace CodeModel {
	public class ViewModel : FileGenerator {
		public List<DataAggregate> items = new List<DataAggregate>();
		public List<CheckEnumeration> enums = new List<CheckEnumeration>();

		public ViewModel(){}

        public override string GetElemName(){
            throw new System.NotImplementedException();
        }

        public override string ToCode(int tabs){
 			string ts = Utils.GetTabString(tabs);
            throw new System.NotImplementedException();
        }

        protected override string GetFileName(){
            return "Types";
        }
    }
}