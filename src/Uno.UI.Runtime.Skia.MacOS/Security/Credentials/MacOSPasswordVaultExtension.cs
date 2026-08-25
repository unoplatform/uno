#nullable enable

using System;
using System.Runtime.InteropServices;
using Uno.Foundation.Extensibility;
using Windows.ApplicationModel;
using Windows.Security.Credentials;

namespace Uno.UI.Runtime.Skia.MacOS;

internal sealed class MacOSPasswordVaultExtension : IPasswordVaultExtension
{
	private const int Success = 0;
	private const int ItemNotFound = -25300;
	private const int InteractionNotAllowed = -25308;
	private const int AuthFailed = -25293;
	private const int MissingEntitlement = -34018;

	private readonly string _scope = Package.Current.Id.Name;

	public static void Register()
		=> ApiExtensibility.Register(
			typeof(IPasswordVaultExtension),
			_ => new MacOSPasswordVaultExtension());

	public byte[]? Read()
	{
		var status = NativeUno.uno_password_vault_read(_scope, out var data, out var length);
		try
		{
			if (status == ItemNotFound)
			{
				return null;
			}

			ThrowIfFailed(status, "read");
			if (length < 0 || (length > 0 && data == 0))
			{
				throw new InvalidOperationException("The macOS Keychain returned invalid credential data.");
			}

			var result = new byte[length];
			if (length > 0)
			{
				Marshal.Copy(data, result, 0, length);
			}

			return result;
		}
		finally
		{
			if (data != 0)
			{
				NativeUno.uno_password_vault_free(data, length);
			}
		}
	}

	public unsafe void Write(byte[] data)
	{
		fixed (byte* dataPointer = data)
		{
			var status = NativeUno.uno_password_vault_write(_scope, dataPointer, data.Length);
			ThrowIfFailed(status, "write");
		}
	}

	private static void ThrowIfFailed(int status, string operation)
	{
		if (status == Success)
		{
			return;
		}

		var reason = status switch
		{
			MissingEntitlement => "The app is not entitled to access the macOS Keychain.",
			InteractionNotAllowed => "The macOS Keychain is locked or cannot display an authentication prompt.",
			AuthFailed => "The macOS Keychain rejected authentication.",
			_ => $"The macOS Keychain returned OSStatus {status}."
		};

		throw new InvalidOperationException($"PasswordVault could not {operation} credentials. {reason}");
	}
}
