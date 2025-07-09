using System;

namespace Uno.UI.Xaml.Controls;

internal static class WebViewUtils
{
	/// <summary>
	/// Determines if a navigation is an anchor navigation within the same page.
	/// </summary>
	/// <param name="currentUrl">The URL of the current page</param>
	/// <param name="newUrl">The URL being navigated to</param>
	/// <returns>True if this is an anchor navigation, false otherwise</returns>
	internal static bool IsAnchorNavigation(string currentUrl, string newUrl)
	{
		if (string.IsNullOrEmpty(currentUrl) || string.IsNullOrEmpty(newUrl))
		{
			return false;
		}

		var currentUrlParts = currentUrl.Split('#');
		var newUrlParts = newUrl.Split('#');

		// Check if this is anchor navigation:
		// 1. Both URLs should have the same base (before #)
		// 2. New URL should have an anchor part (after #)
		// 3. The anchor parts should be different (or one missing)
		return currentUrlParts.Length >= 1
			&& newUrlParts.Length >= 2
			&& currentUrlParts[0].Equals(newUrlParts[0], StringComparison.OrdinalIgnoreCase)
			&& !string.IsNullOrEmpty(newUrlParts[1])
			&& (currentUrlParts.Length == 1 || !currentUrlParts[1].Equals(newUrlParts[1], StringComparison.OrdinalIgnoreCase));
	}
}