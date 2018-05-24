#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ItemClickEventArgs : global::Windows.UI.Xaml.RoutedEventArgs
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  object ClickedItem
		{
			get
			{
				throw new global::System.NotImplementedException("The member object ItemClickEventArgs.ClickedItem is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public ItemClickEventArgs() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ItemClickEventArgs", "ItemClickEventArgs.ItemClickEventArgs()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemClickEventArgs.ItemClickEventArgs()
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemClickEventArgs.ClickedItem.get
	}
}
