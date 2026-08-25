#nullable enable

using System;

namespace Windows.Security.Credentials;

internal sealed class PasswordVaultUnavailableException : InvalidOperationException
{
	internal PasswordVaultUnavailableException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
