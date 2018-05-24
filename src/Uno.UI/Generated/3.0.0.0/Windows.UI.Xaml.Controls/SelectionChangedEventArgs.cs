#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class SelectionChangedEventArgs : global::Windows.UI.Xaml.RoutedEventArgs
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IList<object> AddedItems
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<object> SelectionChangedEventArgs.AddedItems is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IList<object> RemovedItems
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<object> SelectionChangedEventArgs.RemovedItems is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public SelectionChangedEventArgs( global::System.Collections.Generic.IList<object> removedItems,  global::System.Collections.Generic.IList<object> addedItems) : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.SelectionChangedEventArgs", "SelectionChangedEventArgs.SelectionChangedEventArgs(IList<object> removedItems, IList<object> addedItems)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.SelectionChangedEventArgs.SelectionChangedEventArgs(System.Collections.Generic.IList<object>, System.Collections.Generic.IList<object>)
		// Forced skipping of method Windows.UI.Xaml.Controls.SelectionChangedEventArgs.AddedItems.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SelectionChangedEventArgs.RemovedItems.get
	}
}
