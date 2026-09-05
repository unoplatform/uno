using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls_Primitives;

[TestClass]
[RunsOnUIThread]
public class Given_CornerRadiusFilterConverter
{
	// The 12 framework converters that must be reachable as system resources (see uno#23567).
	private static readonly string[] _converterKeys =
	{
		"TopCornerRadiusFilterConverter",
		"RightCornerRadiusFilterConverter",
		"BottomCornerRadiusFilterConverter",
		"LeftCornerRadiusFilterConverter",
		"TopLeftCornerRadiusDoubleValueConverter",
		"BottomRightCornerRadiusDoubleValueConverter",
		"TopThicknessFilterConverter",
		"BottomThicknessFilterConverter",
		"LeftThicknessFilterConverter",
		"RightThicknessFilterConverter",
		"TabViewLeftInsetCornerConverter",
		"TabViewRightInsetCornerConverter",
	};

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23567")]
	public async Task When_CornerRadius_Binding_Converter_Sets_Rectangle_Radius()
	{
		// End-to-end / WinUI-parity guard: ProgressBar binds Rectangle.RadiusX/RadiusY to its CornerRadius via
		// {StaticResource TopLeft/BottomRightCornerRadiusDoubleValueConverter}; verify the converters resolve and
		// produce the right radii. (When_CornerRadius_Converters_Are_System_Resources is the precise #23567
		// fails-before/passes-after guard; this is the rendering-level companion.)
		var progressBar = new ProgressBar
		{
			Width = 200,
			Maximum = 100,
			Value = 50,
			CornerRadius = new CornerRadius(8, 8, 4, 4),
		};

		try
		{
			await UITestHelper.Load(progressBar);

			var rectangles = new List<Rectangle>();
			CollectRectangles(progressBar, rectangles);

			// TopLeftValue -> RadiusX (8), BottomRightValue -> RadiusY (4).
			Assert.IsTrue(
				rectangles.Any(r => Math.Abs(r.RadiusX - 8) < 0.5 && Math.Abs(r.RadiusY - 4) < 0.5),
				$"Expected an indicator Rectangle with RadiusX≈8 / RadiusY≈4 from the CornerRadius converters. " +
				$"Found radii: {string.Join(", ", rectangles.Select(r => $"({r.RadiusX},{r.RadiusY})"))}.");
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

#if HAS_UNO
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23567")]
	public void When_CornerRadius_Converters_Are_System_Resources()
	{
		// The converters are consumed as Binding.Converter via {StaticResource} from Uno.UI control templates,
		// whose one-time static resolve can only reach the system MasterDictionary. All of them must be present
		// there (see CornerRadius_converters.xaml) — a dropped or typo'd key would otherwise ship unguarded.
		foreach (var key in _converterKeys)
		{
			var converter = ResourceResolver.GetSystemResource<IValueConverter>(key);
			Assert.IsNotNull(converter, $"{key} is not reachable as a system resource.");
		}
	}
#endif

	private static void CollectRectangles(DependencyObject root, List<Rectangle> result)
	{
		var count = VisualTreeHelper.GetChildrenCount(root);
		for (var i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			if (child is Rectangle rectangle)
			{
				result.Add(rectangle);
			}

			CollectRectangles(child, result);
		}
	}
}
