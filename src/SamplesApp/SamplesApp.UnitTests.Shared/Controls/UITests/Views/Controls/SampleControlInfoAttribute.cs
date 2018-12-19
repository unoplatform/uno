using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.Samples.Controls
{

	[System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	public sealed class SampleControlInfoAttribute : Attribute
	{
		readonly string _controlName;
		readonly string _category;
		readonly Type _viewModelType;
		readonly bool _ignoreInAutomatedTests;
		readonly string _description;

		public SampleControlInfoAttribute(string category, string controlName, Type viewModelType = null, bool ignoreInAutomatedTests = false, string description = null)
		{
			this._controlName = controlName;
			this._category = category;
			this._viewModelType = viewModelType;
			this._ignoreInAutomatedTests = ignoreInAutomatedTests;
			this._description = description;
        }
		
		public string ControlName
		{
			get { return _controlName; }
		}
		public string Category
		{
			get { return _category; }
		}

		public Type ViewModelType
		{
			get { return _viewModelType; }
		}

		public bool IgnoreInAutomatedTests
		{
			get { return _ignoreInAutomatedTests; }
		}

		public string Description
		{
			get { return _description; }
		}
	}
}
