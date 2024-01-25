///////////////////////////////////////////////////////////
//  ViewFunction.cs
//  Implementation of the Class ViewFunction
//  Generated by Enterprise Architect
//  Created on:      23-sty-2024 15:01:55
//  Original author: smial
///////////////////////////////////////////////////////////

using System.Collections.Generic;
namespace CodeModel {
	public class ViewFunction : NamedElement {
		public List<DataAggregate> data = new List<DataAggregate>();
		public ControllerFunction controller;
		public PresenterClass presenter;
		public List<Trigger> triggers = new List<Trigger>();

		public ViewFunction(){}
	}
}