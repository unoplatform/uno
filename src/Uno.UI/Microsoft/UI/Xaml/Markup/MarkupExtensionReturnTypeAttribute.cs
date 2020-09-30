using System;
using Windows.Foundation.Metadata;

namespace Windows.UI.Xaml.Markup
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	[WebHostHidden]
	public sealed partial class MarkupExtensionReturnTypeAttribute : Attribute
	{
		public MarkupExtensionReturnTypeAttribute()
		{
		}

		public Type ReturnType;
	}
}
