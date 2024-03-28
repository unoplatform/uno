using System;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace UITests.Windows_UI_Xaml.FocusTests
{
	[Sample("Focus")]
	public sealed partial class Focus_FocusCycle : Page
	{
		public Focus_FocusCycle()
		{
			InitializeComponent();
		}

		private void FocusFirstClick(object sender, RoutedEventArgs args)
		{
			B1.Focus(FocusState.Keyboard);
		}

		private async void FocusNextClick(object sender, RoutedEventArgs args)
		{
			var button = (Button)sender;
			var content = button.Tag.ToString();
			var nextElement = FocusManager.FindNextElement(
				(FocusNavigationDirection)Enum.Parse(typeof(FocusNavigationDirection), content, true),
				new FindNextElementOptions()
				{
					SearchRoot = ContainerPrimary.XamlRoot.Content
				});

			if (nextElement != null)
			{
				var focusMoved = await FocusManager.TryFocusAsync(nextElement, FocusState.Keyboard);
				var controlName = (nextElement as FrameworkElement)?.Name;
				if (string.IsNullOrEmpty(controlName))
				{
					controlName = (nextElement as ContentControl)?.Content?.ToString() ?? "N/A";
				}
				MoveResultTextBlock.Text = $"Found focus target {nextElement.GetType().Name} with name {controlName}, focus {(focusMoved.Succeeded ? "moved" : "did not move")}";
			}
			else
			{
				MoveResultTextBlock.Text = $"Could not move focus, no target found.";
			}
		}
	}
}
