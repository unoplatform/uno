using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class SelectionModel
    {
		public TypedEventHandler<SelectionModel, SelectionModelChildrenRequestedEventArgs> ChildrenRequested;

		public TypedEventHandler<SelectionModel, SelectionModelSelectionChangedEventArgs> SelectionChanged;
	}
}
