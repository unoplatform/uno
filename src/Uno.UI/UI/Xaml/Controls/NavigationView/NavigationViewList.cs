namespace Windows.UI.Xaml.Controls
{
	public  partial class NavigationViewList : global::Windows.UI.Xaml.Controls.ListView
	{
		public NavigationViewList() : base()
		{
			Style = Style.DefaultStyleForType(typeof(ListView));
		}

		protected override bool IsItemItsOwnContainerOverride(object item)
		{
			return item is NavigationViewItem
				|| item is NavigationViewItemHeader
				|| item is NavigationViewItemSeparator;
		}

		protected override DependencyObject GetContainerForItemOverride()
		{
			return new NavigationViewItem() { IsGeneratedContainer = true };
		}
	}
}
