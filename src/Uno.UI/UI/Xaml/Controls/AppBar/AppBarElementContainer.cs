using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents a container that allows an element that doesn't implement ICommandBarElement to be displayed in a command bar.
/// </summary>
public partial class AppBarElementContainer : global::Microsoft.UI.Xaml.Controls.ContentControl, ICommandBarElement, ICommandBarElement2, ICommandBarOverflowElement, ICommandBarElement3
{
	protected override void OnApplyTemplate()
	{
	}
}
