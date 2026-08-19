#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.System;

namespace Uno.UI.RuntimeTests.Helpers;

public static class TextContextMenuHelper
{
	/// <summary>
	/// Waits for the command carrying <paramref name="accelerator"/> in an open text context menu (e.g.
	/// <see cref="VirtualKey.C"/> for Copy) and returns the point to click to invoke it.
	/// </summary>
	/// <remarks>
	/// A mouse-opened TextCommandBarFlyout routes Copy/Select All to the *secondary* commands, so no clickable
	/// item exists until CommandBarFlyout expands the overflow. That expansion is deferred to a
	/// <see cref="CompositionTarget.Rendering"/> tick, which <c>WaitForIdle</c> does not pump — hence the render
	/// waits here. Matching on the accelerator rather than the label keeps this independent of the UI language.
	/// </remarks>
	public static async Task<Point> WaitForCommandPoint(XamlRoot xamlRoot, VirtualKey accelerator, int attempts = 30)
	{
		for (var i = 0; i < attempts; i++)
		{
			if (FindCommand(xamlRoot, accelerator)?.GetAbsoluteBounds() is { Width: > 0, Height: > 0 } bounds)
			{
				return new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
			}

			await UITestHelper.WaitForRender();
		}

		throw new TimeoutException(
			$"The text context-menu command for accelerator '{accelerator}' was never realized. " +
			$"Open popups: {VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot).Count}.");
	}

	private static AppBarButton? FindCommand(XamlRoot xamlRoot, VirtualKey accelerator)
		=> VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot)
			.Select(popup => popup.Child)
			.OfType<DependencyObject>()
			.SelectMany(EnumerateSelfAndDescendants)
			.OfType<AppBarButton>()
			.FirstOrDefault(button => button.KeyboardAccelerators.Any(ka => ka.Key == accelerator));

	private static IEnumerable<DependencyObject> EnumerateSelfAndDescendants(DependencyObject root)
	{
		yield return root;

		var count = VisualTreeHelper.GetChildrenCount(root);
		for (var i = 0; i < count; i++)
		{
			foreach (var descendant in EnumerateSelfAndDescendants(VisualTreeHelper.GetChild(root, i)))
			{
				yield return descendant;
			}
		}
	}
}
