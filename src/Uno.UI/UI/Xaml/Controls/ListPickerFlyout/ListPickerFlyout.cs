using NotImplementedException = System.NotImplementedException;

namespace Windows.UI.Xaml.Controls
{
	partial class ListPickerFlyout
	{
		protected override Control CreatePresenter() => throw new NotImplementedException();

		protected override void OnConfirmed() => throw new NotImplementedException();

		protected override bool ShouldShowConfirmationButtons()
		{
			return SelectionMode == ListPickerFlyoutSelectionMode.Multiple;
		}
	}
}
