using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Markup
{
	/// <summary>
	/// Defines the property that will be used in Xaml when using implicit content.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
	public sealed partial class ContentPropertyAttribute : Attribute
	{
		private string _name;

		// This is a positional argument
		public ContentPropertyAttribute()
		{

		}

		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}
	}
}
