///////////////////////////////////////////////////////////
//  COperation.cs
//  Implementation of the Class COperation
//  Generated by Enterprise Architect
//  Created on:      23-sty-2024 15:01:54
//  Original author: smial
///////////////////////////////////////////////////////////

using System.Collections.Generic;
namespace CodeModel {
	public class COperation : Operation {

		public List<DataAggregate> data = new List<DataAggregate>();
		public UCOperation invoked;
		public UCOperation returnTo;

		public COperation(){}
	}
}