using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;

namespace Uno.Helpers;

interface IUnoAccessibility
{
	bool IsAccessibilityEnabled { get; }

	// For now we only have "polite", but not "assertive"
	// See live regions documentation for more information
	// https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/ARIA_Live_Regions#live_regions
	void AnnouncePolite(string text);
}

internal static partial class AccessibilityAnnouncer
{
	internal static IUnoAccessibility AccessibilityImpl { get; set; }

	public static void Announce(string text)
	{
		if (AccessibilityImpl?.IsAccessibilityEnabled == true)
		{
			AccessibilityImpl.AnnouncePolite(text);
		}
	}
}
