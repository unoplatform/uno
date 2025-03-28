

using Windows.Foundation;

namespace Windows.UI.Xaml.Data
{
	public partial interface ISupportIncrementalLoading
	{
		bool HasMoreItems { get; }
		IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count);
	}
}
