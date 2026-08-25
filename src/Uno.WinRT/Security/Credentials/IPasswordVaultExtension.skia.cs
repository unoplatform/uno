#nullable enable

namespace Windows.Security.Credentials;

internal interface IPasswordVaultExtension
{
	byte[]? Read();

	void Write(byte[] data);
}
