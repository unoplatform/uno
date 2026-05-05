using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Uno.UI.Tests.Helpers;

/// <summary>
/// Test helpers that replace the in-memory mock APIs that lived in
/// FrameworkElement.unittests.cs / Window.unittests.cs before #17399. They use
/// only InternalsVisibleTo from Uno.UI to drive the real Skia objects rather
/// than fake substitutes.
/// </summary>
internal static class TestExtensions
{
	/// <summary>
	/// Replicates the legacy mock `FrameworkElement.ForceLoaded()`: marks the
	/// element and all descendants as loaded by directly raising Loaded on
	/// each, since the unit-test process has no real window or render loop to
	/// drive that lifecycle.
	/// </summary>
	internal static void ForceLoaded(this FrameworkElement element)
	{
		if (element is null)
		{
			return;
		}

		ForceLoadedRecursive(element);
	}

	private static void ForceLoadedRecursive(UIElement element)
	{
		element.RaiseLoaded();

		var count = VisualTreeHelper.GetChildrenCount(element);
		for (var i = 0; i < count; i++)
		{
			if (VisualTreeHelper.GetChild(element, i) is UIElement child)
			{
				ForceLoadedRecursive(child);
			}
		}
	}

	/// <summary>
	/// Replicates the legacy mock `Window.SetWindowSize`: sets the visible
	/// bounds on the visual tree and invalidates layout, which is what the
	/// adaptive-trigger / size-change tests need.
	/// </summary>
	internal static void SetWindowSize(this Window window, Size size)
	{
		var xamlRoot = window.Content?.XamlRoot ?? Window.InitialWindow?.RootElement?.XamlRoot;
		if (xamlRoot?.VisualTree is { } visualTree)
		{
			visualTree.VisibleBoundsOverride = new Rect(0, 0, size.Width, size.Height);
		}

		if (window.Content is FrameworkElement root)
		{
			root.InvalidateMeasure();
			root.InvalidateArrange();
			root.UpdateLayout();
		}
	}
}
