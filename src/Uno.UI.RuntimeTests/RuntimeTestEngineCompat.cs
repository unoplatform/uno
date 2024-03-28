global using Colors = Microsoft.UI.Colors;
global using FontWeights = Microsoft.UI.Text.FontWeights;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.UI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using Uno.Foundation.Logging;
using Uno.UI.RuntimeTests.Helpers;

#if HAS_UNO
namespace Uno.Extensions
{
	// Reroutes the LogExtensionPoint this is referenced and initialized by the application
	internal static class LogExtensionPoint
	{

		public static ILogger Log(this Type type)
			=> Uno.Foundation.Logging.LoggerFactory.ExternalLoggerFactory?.CreateLogger(type.FullName ?? type.Name) as ILogger ?? NullLogger.Instance;

		public static ILoggerFactory AmbientLoggerFactory
			=> Uno.Foundation.Logging.LoggerFactory.ExternalLoggerFactory as ILoggerFactory ?? NullLoggerFactory.Instance;
	}
}
#endif

namespace Uno.UI.RuntimeTests
{
	partial class ImageAssert
	{
		public static void HasColorAtChild(RawBitmap screenshot, UIElement renderedElement, UIElement child, double x, double y, string expectedColorCode, byte tolerance = 0, [CallerLineNumber] int line = 0)
			=> HasColorAtChild(screenshot, renderedElement, child, (int)x, (int)y, (Color)XamlBindingHelper.ConvertValue(typeof(Color), expectedColorCode), tolerance, line);

		public static void HasColorAtChild(RawBitmap screenshot, UIElement renderedElement, UIElement child, double x, double y, Color expectedColor, byte tolerance = 0, [CallerLineNumber] int line = 0)
		{
			var point = child.TransformToVisual(renderedElement).TransformPoint(new Windows.Foundation.Point(x, y));
			HasColorAtImpl(screenshot, (int)point.X, (int)point.Y, expectedColor, tolerance, line);
		}
	}

	partial class UnitTestsControl
	{
		partial void ConstructPartial()
		{
			Loaded += Configure;
		}

		private static void Configure(object sender, RoutedEventArgs e)
		{
			var xamlRoot = ((UnitTestsControl)sender).XamlRoot;

			Private.Infrastructure.TestServices.WindowHelper.XamlRoot = xamlRoot;
			Private.Infrastructure.TestServices.WindowHelper.IsXamlIsland =
#if HAS_UNO
				xamlRoot?.HostWindow is null;
#else
				false;
#endif
		}
	}
}
