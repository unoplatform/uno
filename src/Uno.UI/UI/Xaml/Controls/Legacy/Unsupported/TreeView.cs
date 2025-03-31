using System;

namespace Microsoft.UI.Xaml.Controls;

[Obsolete(
		"The Microsoft.UI.Xaml.Controls version of this control is not supported. " +
		"Please use Microsoft" + /* UWP don't rename */ ".UI.Xaml.Controls.TreeView instead.")]
public partial class TreeView
{
	public TreeView()
	{
		throw new NotImplementedException(
			"The Microsoft.UI.Xaml.Controls version of this control is not supported. " +
			"Please use Microsoft" + /* UWP don't rename */ ".UI.Xaml.Controls.TreeView instead.");
	}
}
