namespace Microsoft.UI.Xaml.Controls
{
	public class TabViewTabDroppedOutsideEventArgs
    {
		internal TabViewTabDroppedOutsideEventArgs(object item, TabViewItem tab)
		{
			Item = item;
			Tab = tab;
		}

		public object Item { get; }

		public TabViewItem Tab { get; }
	}
}
