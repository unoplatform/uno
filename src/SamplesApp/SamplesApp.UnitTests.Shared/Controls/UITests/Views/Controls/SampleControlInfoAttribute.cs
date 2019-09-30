using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.Samples.Controls
{

	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	public sealed class SampleControlInfoAttribute : Attribute
	{
		readonly string _controlName;
		readonly string _category;
		readonly Type _viewModelType;
		readonly bool _ignoreInAutomatedTests;
		readonly string _description;

		public SampleControlInfoAttribute(string category, string controlName = null, Type viewModelType = null, bool ignoreInAutomatedTests = false, string description = null)
		{
			this._controlName = controlName;
			this._category = category;
			this._viewModelType = viewModelType;
			this._ignoreInAutomatedTests = ignoreInAutomatedTests;
			this._description = description;
        }
		
		public string ControlName => _controlName;

		public string Category => _category;

		public Type ViewModelType => _viewModelType;

		public bool IgnoreInAutomatedTests => _ignoreInAutomatedTests;

		public string Description => _description;
	}
}
