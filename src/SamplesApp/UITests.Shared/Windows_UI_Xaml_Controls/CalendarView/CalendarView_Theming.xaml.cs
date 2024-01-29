using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI.Samples.Controls;
using Windows.UI;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
						if (SystemThemeHelper.GetSystemApplicationTheme() == ApplicationTheme.Dark)
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
	public static class SystemThemeHelper
	{
		/// <summary>
		/// Get the ApplicationTheme of the device/system
		/// </summary>
		public static ApplicationTheme GetSystemApplicationTheme()
		{
#if __ANDROID__
			if ((int)Android.OS.Build.VERSION.SdkInt >= 28)
			{
				var uiModeFlags = Android.App.Application.Context.Resources.Configuration.UiMode & Android.Content.Res.UiMode.NightMask;
				if (uiModeFlags == Android.Content.Res.UiMode.NightYes)
				{
					return ApplicationTheme.Dark;
				}
			}
			//OS has no preference or API not implemented, use light as default
			return ApplicationTheme.Light;
#elif __IOS__
			//Ensure the current device is running 12.0 or higher, because `TraitCollection.UserInterfaceStyle` was introduced in iOS 12.0
			if (UIKit.UIDevice.CurrentDevice.CheckSystemVersion(12, 0))
			{
				if (UIKit.UIScreen.MainScreen.TraitCollection.UserInterfaceStyle == UIKit.UIUserInterfaceStyle.Dark)
				{
					return ApplicationTheme.Dark;
				}
			}
			return ApplicationTheme.Light;
#elif __MACOS__
			var macosVersion = GetOperatingSystemVersion();
			if (macosVersion >= new Version(10, 14))
			{
				var app = AppKit.NSAppearance.CurrentAppearance?.FindBestMatch(new string[]
				{
					AppKit.NSAppearance.NameAqua,
					AppKit.NSAppearance.NameDarkAqua
				});

				if (app == AppKit.NSAppearance.NameDarkAqua)
				{
					return ApplicationTheme.Dark;
				}
			}
			return ApplicationTheme.Light;

			Version GetOperatingSystemVersion()
			{
				using (var info = new global::Foundation.NSProcessInfo())
				{
					var version = info.OperatingSystemVersion.ToString();
					if (Version.TryParse(version, out var systemVersion))
					{
						return systemVersion;
					}
					else if (int.TryParse(version, out var major))
					{
						return new Version(major, 0);
					}
					return new Version(0, 0);
				}
			}
#elif __WASM__
			var serializedTheme = Uno.Foundation.WebAssemblyRuntime.InvokeJS("Microsoft.UI.Xaml.Application.getDefaultSystemTheme()");

			if (serializedTheme != null)
			{
				if (Enum.TryParse(serializedTheme, out ApplicationTheme theme))
				{
					return theme;
				}
				else
				{
					throw new InvalidOperationException($"{serializedTheme} theme is not a supported OS theme");
				}
			}
			//OS has no preference or API not implemented, use light as default
			return ApplicationTheme.Light;
#elif WINAPPSDK
			var settings = new UISettings();
			var systemBackground = settings.GetColorValue(UIColorType.Background);
			var black = Color.FromArgb(255, 0, 0, 0);
			return systemBackground == black ? ApplicationTheme.Dark : ApplicationTheme.Light;
#else
			// Not implemented so default to light.
			return ApplicationTheme.Light;
#endif
		}
	}

}
