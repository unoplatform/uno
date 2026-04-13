#nullable enable

using System;
using System.Reflection;

namespace Uno.Extensions.Security.Credentials;

/// <summary>
/// Provides the per-application identifier used by Skia Desktop
/// <see cref="IPasswordVaultExtension"/> implementations to scope stored credentials.
/// </summary>
/// <remarks>
/// On Skia Desktop targets there is no package identity to derive from, so the
/// entry assembly name is used. This ensures two Uno applications running on the
/// same machine cannot read each other's vault.
/// </remarks>
internal static class PasswordVaultAppIdentifier
{
	private static readonly Lazy<string> _appId = new Lazy<string>(Resolve);

	public static string AppId => _appId.Value;

	private static string Resolve()
	{
		var entry = Assembly.GetEntryAssembly();
		var name = entry?.GetName().Name;

		if (string.IsNullOrEmpty(name))
		{
			return "default";
		}

		return name;
	}
}
