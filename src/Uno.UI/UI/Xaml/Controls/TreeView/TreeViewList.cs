#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;
using Windows.Foundation.Metadata;

namespace Windows.UI.Xaml.Controls
{
	[Uno.NotImplemented]
	public partial class TreeViewList : ListView
	{
		[Uno.NotImplemented]
		public TreeViewList() : base()
		{
			ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeViewList", "TreeViewList.TreeViewList()");
		}
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewList.TreeViewList()
	}
}
