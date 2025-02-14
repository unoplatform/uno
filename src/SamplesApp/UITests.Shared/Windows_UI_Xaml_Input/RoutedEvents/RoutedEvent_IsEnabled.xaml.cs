using System;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace UITests.Windows_UI_Xaml_Input.RoutedEvents
{
	[Sample("Routed Events", Name = "IsEnabled")]
	public sealed partial class RoutedEvent_IsEnabled : Page
	{
		public RoutedEvent_IsEnabled()
		{
			this.InitializeComponent();
			DisabledButton.RoutedEventTriggered += (s, e) => EventInfoTextBlock.Text = e;
			DisabledButton.Click += (s, e) => EventInfoTextBlock.Text = "Clicked";
		}
	}

	public partial class CustomButton : Button
	{
		public event EventHandler<string> RoutedEventTriggered;

		protected override void OnTapped(TappedRoutedEventArgs e) => RoutedEventTriggered?.Invoke(this, nameof(OnTapped));

		protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs e) => RoutedEventTriggered?.Invoke(this, nameof(OnDoubleTapped));

		protected override void OnGotFocus(RoutedEventArgs e) => RoutedEventTriggered?.Invoke(this, nameof(OnGotFocus));

		protected override void OnLostFocus(RoutedEventArgs e) => RoutedEventTriggered?.Invoke(this, nameof(OnLostFocus));

		protected override void OnPointerCanceled(PointerRoutedEventArgs e) => RoutedEventTriggered?.Invoke(this, nameof(OnPointerCanceled));

		protected override void OnPointerCaptureLost(PointerRoutedEventArgs e) => RoutedEventTriggered?.Invoke(this, nameof(OnPointerCaptureLost));

		protected override void OnPointerEntered(PointerRoutedEventArgs e) => RoutedEventTriggered?.Invoke(this, nameof(OnPointerEntered));

		protected override void OnPointerExited(PointerRoutedEventArgs e) => RoutedEventTriggered?.Invoke(this, nameof(OnPointerExited));

		protected override void OnPointerMoved(PointerRoutedEventArgs e) => RoutedEventTriggered?.Invoke(this, nameof(OnPointerMoved));

		protected override void OnPointerPressed(PointerRoutedEventArgs e) => RoutedEventTriggered?.Invoke(this, nameof(OnPointerPressed));

		protected override void OnPointerReleased(PointerRoutedEventArgs e) => RoutedEventTriggered?.Invoke(this, nameof(OnPointerReleased));

		protected override void OnPointerWheelChanged(PointerRoutedEventArgs e) => RoutedEventTriggered?.Invoke(this, nameof(OnPointerWheelChanged));
	}
}
