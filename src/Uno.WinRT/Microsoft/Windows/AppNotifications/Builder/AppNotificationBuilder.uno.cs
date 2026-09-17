#nullable enable

using System;

namespace Microsoft.Windows.AppNotifications.Builder;

partial class AppNotificationBuilder
{
	private static void ThrowIfMaximumReached(int count, int maximum, string message)
	{
		if (count >= maximum)
		{
			throw new ArgumentException(message);
		}
	}

	// Uno: System.Uri permits relative values, so image validation also verifies native WinRT Uri semantics.
	private static string ValidateImageWithAlternateText(Uri imageUri, string alternateText)
	{
		var source = AppNotificationBuilderUtility.GetAbsoluteUri(imageUri, nameof(imageUri));
		if (string.IsNullOrEmpty(alternateText))
		{
			throw new ArgumentException("Alternate text is required.", nameof(alternateText));
		}

		return source;
	}
}
