using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml
{
	//
	// Summary:
	//     Specifies that a control can be in a certain state and that a VisualState is
	//     expected in the control's ControlTemplate.
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public sealed partial class TemplateVisualStateAttribute : Attribute
	{
		public string GroupName;
		public string Name;

		public TemplateVisualStateAttribute()
		{
		}
	}
}
