using Uno.UI.Samples.Controls;
using Uno.UI.Toolkit;
using Windows.UI.Xaml.Controls;

namespace UITests.Microsoft_UI_Xaml_Controls.ColorPickerTests
{
	[Sample("ColorPicker", "MUX", "ColorPickerInElevatedView")]
	public sealed partial class ColorPickerInElevatedView : Page
	{
		public ColorPickerInElevatedView()
		{
			Button openButton = new Button
			{
				Content = "open",
				Name = "open"
			};
			Button closeButton = new Button
			{
				Content = "close",
				Name = "close"
			};
			ElevatedView view = new ElevatedView
			{
				ElevatedContent = new StackPanel
				{
					Children =
					{
						closeButton,
						new Microsoft.UI.Xaml.Controls.ColorPicker
						{
							IsAlphaEnabled = true,
						}
					}
				}
			};

			Content = new StackPanel
			{
				Children =
				{
					new Button
					{
						Content = "focus",
						Name = "focus",
					},
					openButton,
				}
			};

			openButton.Click += (s, e) =>
			{
				var panel = Content as StackPanel;
				if (panel is StackPanel)
				{
					if (panel.Children.IndexOf(view) < 0)
						panel.Children.Add(view);
				}
			};

			closeButton.Click += (s, e) =>
			{
				var panel = Content as StackPanel;
				if (panel is StackPanel)
				{
					if (panel.Children.IndexOf(view) >= 0)
						panel.Children.Remove(view);
				}
			};
		}
	}
}
