using NotImplementedException = System.NotImplementedException;

namespace Microsoft.UI.Xaml.Controls
{
	partial class ListPickerFlyout
	{
		protected override bool ShouldShowConfirmationButtons()
		{
			return SelectionMode == ListPickerFlyoutSelectionMode.Multiple;
		}
	}
}
