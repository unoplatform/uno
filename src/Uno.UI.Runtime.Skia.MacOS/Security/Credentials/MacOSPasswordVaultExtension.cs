#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Text;
using Uno.Extensions.Security.Credentials;
using Uno.Foundation.Extensibility;

namespace Uno.UI.Runtime.Skia.MacOS.Security.Credentials;

/// <summary>
/// Stores the PasswordVault blob as a generic password item in the macOS Keychain.
/// </summary>
internal sealed class MacOSPasswordVaultExtension : IPasswordVaultExtension
{
	private const string ServicePrefix = "uno_passwordvault.";
	private const string AccountName = "uno";

	private static string ServiceName => ServicePrefix + PasswordVaultAppIdentifier.AppId;

	private const int ErrSecSuccess = 0;
	private const int ErrSecItemNotFound = -25300;

	private const string SecurityFramework = "/System/Library/Frameworks/Security.framework/Security";
	private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

	public static void Register() => ApiExtensibility.Register(typeof(IPasswordVaultExtension), _ => new MacOSPasswordVaultExtension());

	public byte[]? TryRead()
	{
		var service = Encoding.UTF8.GetBytes(ServiceName);
		var account = Encoding.UTF8.GetBytes(AccountName);

		var status = SecKeychainFindGenericPassword(
			IntPtr.Zero,
			(uint)service.Length, service,
			(uint)account.Length, account,
			out var length, out var dataPtr, out var itemRef);

		if (status == ErrSecItemNotFound)
		{
			return null;
		}

		if (status != ErrSecSuccess)
		{
			throw new InvalidOperationException($"SecKeychainFindGenericPassword failed with status {status}.");
		}

		try
		{
			if (length == 0 || dataPtr == IntPtr.Zero)
			{
				return Array.Empty<byte>();
			}

			var data = new byte[length];
			Marshal.Copy(dataPtr, data, 0, (int)length);
			return data;
		}
		finally
		{
			if (dataPtr != IntPtr.Zero)
			{
				SecKeychainItemFreeContent(IntPtr.Zero, dataPtr);
			}
			if (itemRef != IntPtr.Zero)
			{
				CFRelease(itemRef);
			}
		}
	}

	public void Write(byte[] data)
	{
		var service = Encoding.UTF8.GetBytes(ServiceName);
		var account = Encoding.UTF8.GetBytes(AccountName);

		var findStatus = SecKeychainFindGenericPassword(
			IntPtr.Zero,
			(uint)service.Length, service,
			(uint)account.Length, account,
			out _, out var existingDataPtr, out var existingItemRef);

		if (findStatus == ErrSecSuccess)
		{
			try
			{
				var modifyStatus = SecKeychainItemModifyAttributesAndData(existingItemRef, IntPtr.Zero, (uint)data.Length, data);
				if (modifyStatus != ErrSecSuccess)
				{
					throw new InvalidOperationException($"SecKeychainItemModifyAttributesAndData failed with status {modifyStatus}.");
				}
			}
			finally
			{
				if (existingDataPtr != IntPtr.Zero)
				{
					SecKeychainItemFreeContent(IntPtr.Zero, existingDataPtr);
				}
				if (existingItemRef != IntPtr.Zero)
				{
					CFRelease(existingItemRef);
				}
			}
			return;
		}

		if (findStatus != ErrSecItemNotFound)
		{
			throw new InvalidOperationException($"SecKeychainFindGenericPassword failed with status {findStatus}.");
		}

		var addStatus = SecKeychainAddGenericPassword(
			IntPtr.Zero,
			(uint)service.Length, service,
			(uint)account.Length, account,
			(uint)data.Length, data,
			IntPtr.Zero);

		if (addStatus != ErrSecSuccess)
		{
			throw new InvalidOperationException($"SecKeychainAddGenericPassword failed with status {addStatus}.");
		}
	}

	[DllImport(SecurityFramework)]
	private static extern int SecKeychainAddGenericPassword(
		IntPtr keychain,
		uint serviceNameLength, byte[] serviceName,
		uint accountNameLength, byte[] accountName,
		uint passwordLength, byte[] passwordData,
		IntPtr itemRef);

	[DllImport(SecurityFramework)]
	private static extern int SecKeychainFindGenericPassword(
		IntPtr keychainOrArray,
		uint serviceNameLength, byte[] serviceName,
		uint accountNameLength, byte[] accountName,
		out uint passwordLength, out IntPtr passwordData,
		out IntPtr itemRef);

	[DllImport(SecurityFramework)]
	private static extern int SecKeychainItemFreeContent(IntPtr attrList, IntPtr data);

	[DllImport(SecurityFramework)]
	private static extern int SecKeychainItemModifyAttributesAndData(
		IntPtr itemRef, IntPtr attrList, uint length, byte[] data);

	[DllImport(CoreFoundation)]
	private static extern void CFRelease(IntPtr cf);
}
