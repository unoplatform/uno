namespace Windows.UI.Xaml.Controls
{
	public partial class TreeViewExpandingEventArgs
    {
		internal TreeViewExpandingEventArgs(TreeViewNode node)
		{
			Node = node;
		}

		public TreeViewNode Node { get; }

		public object Item => Node.Content;
	}
}
