using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using XamlWindow = Windows.UI.Xaml.Window;

namespace UITests.Windows_UI_Xaml.ThemeResources
{
	[SampleControlInfo("XAML", nameof(ReloadedControlTheme), Description = SampleDescription, IsManualTest = true, IgnoreInSnapshotTests = true)]
	public sealed partial class ReloadedControlTheme : UserControl
	{
		private const string SampleDescription =
			"[ManualTest]: Use 'Un/Load Control' to load the control and then unload it." +
			"While the control is unloaded, change the theme with 'Dark Mode', and then reload the control." +
			"The theme changes while the control is unloaded should still be effective.";

		public ReloadedControlTheme()
		{
			this.InitializeComponent();

			this.Loaded += (s, e) =>
			{
				var root = XamlRoot?.Content as FrameworkElement;
				var isDark = GetCurrentOsTheme() == ApplicationTheme.Dark;

				DarkModeToggle.IsOn = isDark;
				DarkModeToggle.Toggled += (s2, e2) =>
				{
					var willBeDark = DarkModeToggle.IsOn;
					root.RequestedTheme = willBeDark ? ElementTheme.Dark : ElementTheme.Light;
				};

				LoadControlToggle.IsOn = false;
				LoadControlToggle.Toggled += (s2, e2) =>
				{
					var toLoad = LoadControlToggle.IsOn;
					if (toLoad)
					{
						TestControlContainer.Child = new Rectangle
						{
							Width = 50,
							Height = 50,
							Fill = Resources["ReloadedControlTheme_Brush"] as Brush,
						};
					}
					else
					{
						TestControlContainer.Child = null;
					}
				};
			};
		}

		private static ApplicationTheme GetCurrentOsTheme()
		{
			var settings = new UISettings();
			var systemBackground = settings.GetColorValue(UIColorType.Background);
			var black = Color.FromArgb(255, 0, 0, 0);

			return systemBackground == black ? ApplicationTheme.Dark : ApplicationTheme.Light;
		}
	}
}
