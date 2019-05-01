using Windows.UI.Xaml.Input;

namespace Windows.UI.Xaml.Controls
{
	public partial class FlyoutPresenter : ContentControl
	{
		public FlyoutPresenter()
		{
		}

		protected override void OnPointerPressed(PointerRoutedEventArgs args)
		{
			// Stop the propagation of the pressed
			// to prevent dismissal of the flyout
			// when clicking inside it.
			args.Handled = true;
		}

		protected override bool CanCreateTemplateWithoutParent { get; } = true;
	}
}
