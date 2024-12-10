using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml
{
	[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
	public sealed partial class TemplatePartAttribute : Attribute
	{
		public string Name;

		public Type Type;
	}
}
