#nullable enable

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Uno.Foundation.Extensibility;
using Windows.ApplicationModel;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security.Cryptography;

namespace Uno.UI.Runtime.Skia.Win32;

/// <summary>
/// Persists the serialized vault in a DPAPI-protected file owned by the current Windows user.
/// The protection key stays with the operating system and is never available to the app, which
/// mirrors how the Android implementation combines the platform key store with an app-local file.
/// </summary>
internal sealed unsafe class Win32PasswordVaultExtension : IPasswordVaultExtension
{
	private const string FileName = ".password-vault";
	private const string EntropyPrefix = "Uno.PasswordVault:";

	private readonly string _scope = Package.Current.Id.Name;

	public static void Register()
		=> ApiExtensibility.Register(
			typeof(IPasswordVaultExtension),
			_ => new Win32PasswordVaultExtension());

	public byte[]? Read()
	{
		byte[] protectedData;
		try
		{
			protectedData = File.ReadAllBytes(GetVaultPath());
		}
		catch (Exception error) when (error is FileNotFoundException or DirectoryNotFoundException)
		{
			return null;
		}
		catch (Exception error) when (error is IOException or UnauthorizedAccessException)
		{
			throw new InvalidOperationException("PasswordVault could not read the protected vault file.", error);
		}

		if (protectedData.Length == 0)
		{
			return null;
		}

		try
		{
			return Unprotect(protectedData);
		}
		finally
		{
			CryptographicOperations.ZeroMemory(protectedData);
		}
	}

	public void Write(byte[] data)
	{
		var path = GetVaultPath();
		var temporaryPath = path + ".tmp";
		var protectedData = Protect(data);
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			File.WriteAllBytes(temporaryPath, protectedData);

			if (File.Exists(path))
			{
				// No backup file is kept: it would let a removed credential be restored.
				File.Replace(temporaryPath, path, null, ignoreMetadataErrors: true);
			}
			else
			{
				File.Move(temporaryPath, path);
			}
		}
		catch (Exception error) when (error is IOException or UnauthorizedAccessException)
		{
			throw new InvalidOperationException("PasswordVault could not write the protected vault file.", error);
		}
		finally
		{
			CryptographicOperations.ZeroMemory(protectedData);
		}
	}

	private byte[] Protect(byte[] data)
	{
		var entropy = GetEntropy();
		fixed (byte* dataPointer = GetPinnable(data))
		fixed (byte* entropyPointer = entropy)
		{
			var input = new CRYPT_INTEGER_BLOB { cbData = (uint)data.Length, pbData = dataPointer };
			var entropyBlob = new CRYPT_INTEGER_BLOB { cbData = (uint)entropy.Length, pbData = entropyPointer };
			var output = default(CRYPT_INTEGER_BLOB);

			if (!PInvoke.CryptProtectData(&input, null, &entropyBlob, null, null, PInvoke.CRYPTPROTECT_UI_FORBIDDEN, &output))
			{
				throw new InvalidOperationException(
					$"PasswordVault could not protect the vault with DPAPI (error {Marshal.GetLastWin32Error()}).");
			}

			return CopyAndFree(ref output);
		}
	}

	private byte[] Unprotect(byte[] protectedData)
	{
		var entropy = GetEntropy();
		fixed (byte* dataPointer = GetPinnable(protectedData))
		fixed (byte* entropyPointer = entropy)
		{
			var input = new CRYPT_INTEGER_BLOB { cbData = (uint)protectedData.Length, pbData = dataPointer };
			var entropyBlob = new CRYPT_INTEGER_BLOB { cbData = (uint)entropy.Length, pbData = entropyPointer };
			var output = default(CRYPT_INTEGER_BLOB);

			if (!PInvoke.CryptUnprotectData(&input, null, &entropyBlob, null, null, PInvoke.CRYPTPROTECT_UI_FORBIDDEN, &output))
			{
				throw new InvalidOperationException(
					$"PasswordVault could not unprotect the vault with DPAPI (error {Marshal.GetLastWin32Error()}). "
					+ "The vault file belongs to another Windows user or application identity.");
			}

			return CopyAndFree(ref output);
		}
	}

	private static byte[] CopyAndFree(ref CRYPT_INTEGER_BLOB blob)
	{
		try
		{
			var result = new byte[blob.cbData];
			if (blob.cbData > 0)
			{
				new ReadOnlySpan<byte>(blob.pbData, (int)blob.cbData).CopyTo(result);
			}

			return result;
		}
		finally
		{
			if (blob.pbData is not null)
			{
				CryptographicOperations.ZeroMemory(new Span<byte>(blob.pbData, (int)blob.cbData));
				PInvoke.LocalFree(new HLOCAL(blob.pbData));
			}
		}
	}

	// 'fixed' on an empty array yields a null pointer, which DPAPI rejects.
	private static byte[] GetPinnable(byte[] data)
		=> data.Length == 0 ? new byte[1] : data;

	private byte[] GetEntropy()
		=> SHA256.HashData(Encoding.UTF8.GetBytes(EntropyPrefix + _scope));

	private static string GetVaultPath()
		=> Path.Combine(ApplicationData.Current.LocalFolder.Path, FileName);
}
