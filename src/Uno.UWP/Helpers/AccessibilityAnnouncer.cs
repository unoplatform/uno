#nullable enable

using System;

namespace Uno.Helpers;

internal static partial class AccessibilityAnnouncer
{
	private static IUnoAccessibility? _accessibilityImpl;

	internal static IUnoAccessibility? AccessibilityImpl
	{
		get => TestAccessibilityImpl ?? _accessibilityImpl;
		set
		{
			if (_accessibilityImpl is not null &&
				value is not null &&
				!ReferenceEquals(_accessibilityImpl, value))
			{
				throw new InvalidOperationException("AccessibilityImpl should only be set once.");
			}

			_accessibilityImpl = value;
		}
	}

	internal static IUnoAccessibility? TestAccessibilityImpl { get; set; }

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
