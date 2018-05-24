#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Data
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ISupportIncrementalLoading 
	{
		#if false || false || false || false
		bool HasMoreItems
		{
			get;
		}
		#endif
		#if false || false || false || false
		global::Windows.Foundation.IAsyncOperation<global::Windows.UI.Xaml.Data.LoadMoreItemsResult> LoadMoreItemsAsync( uint count);
		#endif
		// Forced skipping of method Windows.UI.Xaml.Data.ISupportIncrementalLoading.HasMoreItems.get
	}
}
