#nullable disable

using System;

namespace Windows.Foundation.Metadata
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = false)]
	public sealed partial class WebHostHiddenAttribute : Attribute
	{
		public WebHostHiddenAttribute()
		{

		}
	}
}
