#nullable enable

namespace Uno.Extensions.Security.Credentials;

/// <summary>
/// Extension contract used by <see cref="Windows.Security.Credentials.PasswordVault"/> on Skia targets
/// to delegate storage of the opaque vault blob to an OS-native secure credential store
/// (Windows Credential Manager, macOS Keychain, Linux Secret Service).
/// </summary>
internal interface IPasswordVaultExtension
{
	/// <summary>
	/// Returns the previously persisted vault blob, or <c>null</c> if none exists.
	/// </summary>
	byte[]? TryRead();

	/// <summary>
	/// Persists the provided vault blob, replacing any previous value.
	/// </summary>
	void Write(byte[] data);
}
