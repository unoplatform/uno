using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// Uno-specific interface that allows controls to specify that particular properties should be ignored by the shared layouting, eg for
	/// compatibility when a native template is used.
	/// </summary>
	internal interface ILayoutOptOut
	{
		bool ShouldUseMinSize { get; }
	}
}
