namespace Windows.UI.Xaml.Controls
{
	public  partial class NavigationViewList : global::Windows.UI.Xaml.Controls.ListView
	{
		public NavigationViewList() : base()
		{
			Style = Style.DefaultStyleForType(typeof(ListView));
		}

		protected override bool IsItemItsOwnContainerOverride(object item)
			=> item is NavigationViewItemBase;

		protected override DependencyObject GetContainerForItemOverride()
			=> new NavigationViewItem() { IsGeneratedContainer = true };
	}
}
