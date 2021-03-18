using NotImplementedException = System.NotImplementedException;

namespace Windows.UI.Xaml.Controls
{
	partial class ListPickerFlyout
	{
		protected override bool ShouldShowConfirmationButtons()
		{
			return SelectionMode == ListPickerFlyoutSelectionMode.Multiple;
		}
	}
}
