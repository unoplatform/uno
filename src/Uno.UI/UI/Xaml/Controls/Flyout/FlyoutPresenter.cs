using Windows.UI.Xaml.Input;

namespace Windows.UI.Xaml.Controls
{
	public partial class FlyoutPresenter : ContentControl
	{
		public FlyoutPresenter()
		{
			DefaultStyleKey = typeof(FlyoutPresenter);
		}

		protected override void OnPointerPressed(PointerRoutedEventArgs args)
		{
			// All pointer-related should be "eaten" to prevent closing
			// the flyout when a tap is done in its content
			args.Handled = true;
		}

		protected override void OnPointerReleased(PointerRoutedEventArgs args)
		{
			// All pointer-related should be "eaten" to prevent closing
			// the flyout when a tap is done in its content
			args.Handled = true;
		}

		protected override void OnTapped(TappedRoutedEventArgs args)
		{
			// All pointer-related should be "eaten" to prevent closing
			// the flyout when a tap is done in its content
			args.Handled = true;
		}

		protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs args)
		{
			// All pointer-related should be "eaten" to prevent closing
			// the flyout when a tap is done in its content
			args.Handled = true;
		}

		protected override bool CanCreateTemplateWithoutParent { get; } = true;

		internal override bool IsViewHit() => true;
	}
}
