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

	// See live regions documentation for more information
	// https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/ARIA_Live_Regions#live_regions
	void AnnouncePolite(string text);
	void AnnounceAssertive(string text);
}

internal static partial class AccessibilityAnnouncer
{
	internal static IUnoAccessibility AccessibilityImpl { get; set; }

	public static void AnnouncePolite(string text)
	{
		if (text is not null && AccessibilityImpl?.IsAccessibilityEnabled == true)
		{
			AccessibilityImpl.AnnouncePolite(text);
		}
	}

	// NOTE: Currently this is unused, but we have everything in place for when
	// we will support AutomationLiveSetting
	public static void AnnounceAssertive(string text)
	{
		if (text is not null && AccessibilityImpl?.IsAccessibilityEnabled == true)
		{
			AccessibilityImpl.AnnounceAssertive(text);
		}
	}
}
