using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Markup
{
	public partial interface IProvideValueTarget
	{
		object TargetObject { get; }

		object TargetProperty { get; }
	}
}
