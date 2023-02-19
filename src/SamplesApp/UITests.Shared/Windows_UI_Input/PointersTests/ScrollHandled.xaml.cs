using Uno.UI.Samples.Controls;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace UITests.Windows_UI_Input.PointersTests
{
	[Sample("Pointers", Description = "When scroll wheel is used while hovering above the buttons, it should not scroll. When scroll wheel is used above the page's blank area, it should scroll.")]
	public sealed partial class ScrollHandled : Page
	{
		public ScrollHandled()
		{
			InitializeComponent();
		}

		private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs args)
		{
			args.Handled = true;
		}
	}
}
