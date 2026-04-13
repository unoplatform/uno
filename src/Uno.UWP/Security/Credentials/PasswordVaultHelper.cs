#nullable enable

using System;

#if __SKIA__
using Uno.Extensions.Security.Credentials;
using Uno.Foundation.Extensibility;
#endif

namespace Uno.Security.Credentials;

/// <summary>
/// Uno-specific helpers for <see cref="Windows.Security.Credentials.PasswordVault"/>.
/// </summary>
/// <remarks>
/// This type is NOT part of the WinRT surface. It is provided by Uno Platform so apps
/// can probe for runtime availability (e.g. on Linux where support depends on
/// <c>libsecret</c> being installed).
/// </remarks>
public static class PasswordVaultHelper
{
#if __SKIA__
	private static readonly Lazy<bool> _isSupported = new Lazy<bool>(() =>
		ApiExtensibility.CreateInstance<IPasswordVaultExtension>(typeof(PasswordVaultHelper), out var extension) && extension is not null);
#endif

	/// <summary>
	/// Indicates whether <see cref="Windows.Security.Credentials.PasswordVault"/> is usable
	/// on the current platform and runtime environment.
	/// </summary>
	public static bool IsSupported()
	{
#if __ANDROID__ || __APPLE_UIKIT__
		return true;
#elif __WASM__
		return false;
#elif __SKIA__
		return _isSupported.Value;
#else
		return false;
#endif
	}
}
