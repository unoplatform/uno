namespace Microsoft.UI.Xaml.Controls
{
	public partial class TreeViewCollapsedEventArgs
    {
		internal TreeViewCollapsedEventArgs(TreeViewNode node)
		{
			Node = node;
		}

		public TreeViewNode Node { get; }

		public object Item => Node.Content;
	}
}
