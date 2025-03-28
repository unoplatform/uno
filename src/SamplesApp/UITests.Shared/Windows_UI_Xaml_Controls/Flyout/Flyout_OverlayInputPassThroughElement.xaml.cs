using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Uno.UI.Samples.Controls;
using System;

namespace UITests.Shared.Windows_UI_Xaml_Controls.Flyout
{
	[Sample("Flyouts")]
	public sealed partial class Flyout_OverlayInputPassThroughElement : Page
	{
		public Flyout_OverlayInputPassThroughElement()
		{
			this.InitializeComponent();
		}

		private void SubscribeToPointerPressed(object sender, RoutedEventArgs e)
		{
			((UIElement)sender).AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(OnPointerPressed), true);
			((UIElement)sender).AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(OnPointerPressedUnHandled), false);
		}

		private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
		{
			PressedOutput.Text += $"{DateTime.Now:mm:ss} - {((FrameworkElement)sender).Name} - {e.Handled} - {GetSourceName(e)}\r\n";
		}

		private void OnPointerPressedUnHandled(object sender, PointerRoutedEventArgs e)
		{
			PressedUnhandledOutput.Text += $"{DateTime.Now:mm:ss} - {((FrameworkElement)sender).Name} - {e.Handled} - {GetSourceName(e)}\r\n";
		}

		private void OnElementTapped(object sender, TappedRoutedEventArgs e)
		{
			TappedOutput.Text += $"{DateTime.Now:mm:ss} - {((FrameworkElement)sender).Name} tapped\r\n";
			e.Handled = true;
		}

		private void OnButtonClicked(object sender, RoutedEventArgs e)
		{
			TappedOutput.Text += $"{DateTime.Now:mm:ss} - {((FrameworkElement)sender).Name} clicked\r\n";
		}

		private void ClearLog(object sender, RoutedEventArgs e)
		{
			TappedOutput.Text = "";
			PressedOutput.Text = "";
			PressedUnhandledOutput.Text = "";
		}

		private static string GetSourceName(PointerRoutedEventArgs args)
			=> args.OriginalSource switch
			{
				FrameworkElement { Name: { Length: > 0 } name } => name,
				null => "-null-",
				{ } src => $"{src.GetType().Name}:{src.GetHashCode():X8}"
			};

		private void OnFlyoutOpened(object sender, object e)
		{
			IsFlyoutOpened.Text = "True";
		}

		private void OnFlyoutClosed(object sender, object e)
		{
			IsFlyoutOpened.Text = "False";
		}
	}
}
