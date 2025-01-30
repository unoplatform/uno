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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using XamlWindow = Microsoft.UI.Xaml.Window;

namespace UITests.Windows_UI_Xaml_Shapes
{
	[Sample("Shapes")]
	public sealed partial class Shape_StrokeTest : Page
	{
		private readonly List<SolidColorBrush> _brushes =
			new[] { Colors.Red, Colors.Green, Colors.Blue }
				.Select(x => new SolidColorBrush(x))
				.ToList();

		public Shape_StrokeTest()
		{
			this.InitializeComponent();
			TestTarget.Stroke = _brushes[0];

			// Apperently, these scenarios already work without the fix..?:
			// - 1 OK: assign Shape.Stroke directly
			// - 2 OK: assign Shape.Stroke.Color directly
			// - 3 OK: updating Shape.Stroke.Color(ThemeResource) with (dark/light) theme change
			// - 4 FAIL: assign Shape.Stroke.Color directly within a Grid?
			// This test is aimed at the #4 scenario, and tests for its fix.
		}

		private void ChangeTheme()
		{
			if (XamlRoot?.Content is FrameworkElement root)
			{
				var theme = root.ActualTheme switch
				{
					ElementTheme.Light => ApplicationTheme.Light,
					ElementTheme.Dark => ApplicationTheme.Dark,

					_ => GetCurrentOsTheme(),
				};
				root.RequestedTheme = theme == ApplicationTheme.Light ? ElementTheme.Dark : ElementTheme.Light;
			}

			ApplicationTheme GetCurrentOsTheme()
			{
				var settings = new UISettings();
				var systemBackground = settings.GetColorValue(UIColorType.Background);
				var black = Color.FromArgb(255, 0, 0, 0);

				return systemBackground == black ? ApplicationTheme.Dark : ApplicationTheme.Light;
			}
		}

		private void UpdateBrush()
		{
			var index = (_brushes.IndexOf(TestTarget.Stroke as SolidColorBrush) + 1) % _brushes.Count;
			var brush = _brushes[index];

			TestTarget.Stroke = brush;
		}

		private void UpdateBrushColor()
		{
			if (TestTarget.Stroke is SolidColorBrush stroke)
			{
				stroke.Color = Color.FromArgb(
					byte.MaxValue,
					(byte)(stroke.Color.R ^ 255),
					(byte)(stroke.Color.G ^ 255),
					(byte)(stroke.Color.B ^ 255)
				);
			}
		}
	}
}
