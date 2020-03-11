using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Windows.UI.Xaml.Controls
{
	public partial class TreeViewNode
	{
		internal event TypedEventHandler<TreeViewNode, IVectorChangedEventArgs> ChildrenChanged;

		internal event TypedEventHandler<TreeViewNode, DependencyPropertyChangedEventArgs> ExpandedChanged;
	}
}
