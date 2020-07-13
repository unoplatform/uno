namespace Microsoft.UI.Xaml.Controls
{
	public sealed class TabViewTabCloseRequestedEventArgs
    {
		internal TabViewTabCloseRequestedEventArgs(object item, TabViewItem tab)
		{
			Item = item;
			Tab = tab;
		}

		public object Item { get; }

		public TabViewItem Tab { get; }
    }
}
