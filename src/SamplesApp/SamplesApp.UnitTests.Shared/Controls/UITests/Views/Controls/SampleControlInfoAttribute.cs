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
		readonly bool _ignoreInSnapshotTests;
		readonly string _description;

		public SampleControlInfoAttribute(
			string category = null,
			string controlName = null,
			Type viewModelType = null,
			bool ignoreInSnapshotTests = false,
			string description = null)
		{
			this._controlName = controlName;
			this._category = category;
			this._viewModelType = viewModelType;
			this._ignoreInSnapshotTests = ignoreInSnapshotTests;
			this._description = description;
        }
		
		public string ControlName => _controlName;

		public string Category => _category;

		public Type ViewModelType => _viewModelType;

		public bool IgnoreInSnapshotTests => _ignoreInSnapshotTests;

		public string Description => _description;
	}
}
