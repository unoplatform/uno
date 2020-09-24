#nullable enable

using Windows.ApplicationModel.DataTransfer;

namespace Windows.UI.Xaml
{
	public partial class DropCompletedEventArgs : RoutedEventArgs
	{
		internal DropCompletedEventArgs(UIElement originalSource, DataPackageOperation result)
			: base(originalSource)
		{
			DropResult = result;

			CanBubbleNatively = false;
		}

		public DataPackageOperation DropResult { get; }
	}
}
