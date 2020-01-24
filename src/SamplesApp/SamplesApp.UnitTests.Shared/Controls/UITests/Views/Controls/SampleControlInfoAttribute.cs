using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.Samples.Controls
{

	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	public sealed class SampleControlInfoAttribute : SampleAttribute
	{
		public SampleControlInfoAttribute(
			string category = null,
			string controlName = null,
			Type viewModelType = null,
			bool ignoreInSnapshotTests = false,
			string description = null)
			: base(category)
		{
			Name = controlName;
			ViewModelType = viewModelType;
			IgnoreInSnapshotTests = ignoreInSnapshotTests;
			Description = description;
		}
	}

	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public class SampleAttribute : Attribute
	{
		public SampleAttribute(params string[] categories)
		{
			Categories = categories;
		}

		public string[] Categories { get; }

		public string Name { get; set; }

		public bool IgnoreInSnapshotTests { get; set; }

		public Type ViewModelType { get; set; }

		public string Description { get; set; }
	}
}
