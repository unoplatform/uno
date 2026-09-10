#nullable enable

using System;

namespace Microsoft.Windows.AppNotifications.Builder;

internal static partial class AppNotificationBuilderUtility
{
	// Uno: System.Uri permits relative values, unlike the projected WinRT Uri accepted by the native builder.
	public static string GetAbsoluteUri(Uri value, string parameterName)
	{
		ArgumentNullException.ThrowIfNull(value, parameterName);
		if (!value.IsAbsoluteUri)
		{
			throw new ArgumentException("An absolute URI is required.", parameterName);
		}

		return value.AbsoluteUri;
	}

	// WindowsVersion::IsWindows10_20H1OrGreater is the native capability gate.
	public static bool IsWindows10_20H1OrGreater() => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041);
}
