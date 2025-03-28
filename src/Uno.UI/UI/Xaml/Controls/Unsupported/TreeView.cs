using System;

namespace Windows.UI.Xaml.Controls;

[Obsolete(
		"The Windows.UI.Xaml.Controls version of this control is not supported. " +
		"Please use Microsoft" + /* UWP don't rename */ ".UI.Xaml.Controls.TreeView instead.")]
public partial class TreeView
{
	public TreeView()
	{
		throw new NotImplementedException(
			"The Windows.UI.Xaml.Controls version of this control is not supported. " +
			"Please use Microsoft" + /* UWP don't rename */ ".UI.Xaml.Controls.TreeView instead.");
	}
}
