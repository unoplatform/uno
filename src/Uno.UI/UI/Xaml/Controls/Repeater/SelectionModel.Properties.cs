using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class SelectionModel
	{
		public event TypedEventHandler<SelectionModel, SelectionModelChildrenRequestedEventArgs> ChildrenRequested;

		public event TypedEventHandler<SelectionModel, SelectionModelSelectionChangedEventArgs> SelectionChanged;
	}
}
