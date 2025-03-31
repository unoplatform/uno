using Windows.Foundation;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public partial class SelectionModel
	{
		public event TypedEventHandler<SelectionModel, SelectionModelChildrenRequestedEventArgs> ChildrenRequested;

		public event TypedEventHandler<SelectionModel, SelectionModelSelectionChangedEventArgs> SelectionChanged;
	}
}
