using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#if !WINAPPSDK
using Uno.Helpers.Theming;
#endif
using Uno.UI.Samples.Controls;
#if WINAPPSDK
using Windows.UI;
using Windows.UI.ViewManagement;
#endif

namespace UITests.Windows_UI_Xaml_Controls.CalendarView
{
	[Sample("Pickers")]
	public sealed partial class CalendarView_Theming : Page
	{
		public CalendarView_Theming()
		{
			this.InitializeComponent();
		}

		private void ToggleButton_Click(object sender, RoutedEventArgs e)
		{
			// Set theme for window root.
			if (XamlRoot?.Content is FrameworkElement root)
			{
				switch (root.ActualTheme)
				{
					case ElementTheme.Default:
						if (SampleSystemThemeHelper.GetSystemApplicationTheme() == ApplicationTheme.Dark)
						{
							root.RequestedTheme = ElementTheme.Light;
						}
						else
						{
							root.RequestedTheme = ElementTheme.Dark;
						}
						break;
					case ElementTheme.Light:
						root.RequestedTheme = ElementTheme.Dark;
						break;
					case ElementTheme.Dark:
						root.RequestedTheme = ElementTheme.Light;
						break;
				}
			}
		}
	}

	/// <summary>
	/// Helper class for the theme (dark/light)
	/// </summary>
	public static class SampleSystemThemeHelper
	{
		/// <summary>
		/// Get the ApplicationTheme of the device/system
		/// </summary>
		public static ApplicationTheme GetSystemApplicationTheme()
		{
#if WINAPPSDK
			var settings = new UISettings();
			var systemBackground = settings.GetColorValue(UIColorType.Background);
			var black = Color.FromArgb(255, 0, 0, 0);
			return systemBackground == black ? ApplicationTheme.Dark : ApplicationTheme.Light;
#else
			return SystemThemeHelper.GetSystemTheme() switch
			{
				SystemTheme.Light => ApplicationTheme.Light,
				SystemTheme.Dark => ApplicationTheme.Dark,
				_ => throw new ArgumentOutOfRangeException(),
			};
#endif
		}
	}

}
