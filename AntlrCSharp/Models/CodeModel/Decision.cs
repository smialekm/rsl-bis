///////////////////////////////////////////////////////////
//  Decision.cs
//  Implementation of the Class Decision
//  Generated by Enterprise Architect
//  Created on:      23-sty-2024 15:01:54
//  Original author: smial
///////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Linq;
namespace CodeModel {
	public class Decision : Instruction {

		public List<Condition> conditions = new List<Condition>();

		public Decision(){}

        public override string ToCode(int tabs = 0){
			string ts = Utils.GetTabString(tabs);
            string code = ts + string.Join(" else ", conditions.Select(c => c.ToCode(tabs)));
			return code;
        }
    }
}