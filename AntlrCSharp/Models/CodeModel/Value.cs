///////////////////////////////////////////////////////////
//  Value.cs
//  Implementation of the Class Value
//  Generated by Enterprise Architect
//  Created on:      23-sty-2024 15:01:55
//  Original author: smial
///////////////////////////////////////////////////////////

using System;

namespace CodeModel {
	public class Value : NamedElement {
		public CheckEnumeration parent = null;

		public Value(){
		}

		public string GetElemName(){
			return Utils.ToUpperCase(name);
		}
	}
}