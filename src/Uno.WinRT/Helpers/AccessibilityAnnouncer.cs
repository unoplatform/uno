namespace Uno.Helpers;

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
