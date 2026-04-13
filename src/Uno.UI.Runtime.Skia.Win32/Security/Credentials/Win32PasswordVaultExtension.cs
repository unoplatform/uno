#nullable enable

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Uno.Extensions.Security.Credentials;

namespace Uno.UI.Runtime.Skia.Win32.Security.Credentials;

/// <summary>
/// Stores the PasswordVault blob in the Windows Credential Manager
/// (user-scoped, generic credential), keyed per application.
/// </summary>
internal sealed class Win32PasswordVaultExtension : IPasswordVaultExtension
{
	private const string TargetPrefix = "uno_passwordvault:";

	private const uint CRED_TYPE_GENERIC = 1;
	private const uint CRED_PERSIST_LOCAL_MACHINE = 2;
	private const int ERROR_NOT_FOUND = 1168;

	private static string TargetName => TargetPrefix + PasswordVaultAppIdentifier.AppId;

	public byte[]? TryRead()
	{
		if (!CredReadW(TargetName, CRED_TYPE_GENERIC, 0, out var credPtr))
		{
			var err = Marshal.GetLastWin32Error();
			if (err == ERROR_NOT_FOUND)
			{
				return null;
			}

			throw new Win32Exception(err, $"CredReadW failed with error {err}.");
		}

		try
		{
			var cred = Marshal.PtrToStructure<CREDENTIAL>(credPtr);
			if (cred.CredentialBlobSize == 0 || cred.CredentialBlob == IntPtr.Zero)
			{
				return Array.Empty<byte>();
			}

			var data = new byte[cred.CredentialBlobSize];
			Marshal.Copy(cred.CredentialBlob, data, 0, (int)cred.CredentialBlobSize);
			return data;
		}
		finally
		{
			CredFree(credPtr);
		}
	}

	public void Write(byte[] data)
	{
		var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
		try
		{
			var cred = new CREDENTIAL
			{
				Flags = 0,
				Type = CRED_TYPE_GENERIC,
				TargetName = TargetName,
				Comment = null,
				CredentialBlobSize = (uint)data.Length,
				CredentialBlob = handle.AddrOfPinnedObject(),
				Persist = CRED_PERSIST_LOCAL_MACHINE,
				AttributeCount = 0,
				Attributes = IntPtr.Zero,
				TargetAlias = null,
				UserName = Environment.UserName,
			};

			if (!CredWriteW(ref cred, 0))
			{
				var err = Marshal.GetLastWin32Error();
				throw new Win32Exception(err, $"CredWriteW failed with error {err}.");
			}
		}
		finally
		{
			handle.Free();
		}
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct CREDENTIAL
	{
		public uint Flags;
		public uint Type;
		public string TargetName;
		public string? Comment;
		public global::System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
		public uint CredentialBlobSize;
		public IntPtr CredentialBlob;
		public uint Persist;
		public uint AttributeCount;
		public IntPtr Attributes;
		public string? TargetAlias;
		public string? UserName;
	}

	[DllImport("advapi32.dll", EntryPoint = "CredReadW", SetLastError = true, CharSet = CharSet.Unicode)]
	private static extern bool CredReadW(string target, uint type, uint reservedFlag, out IntPtr credentialPtr);

	[DllImport("advapi32.dll", EntryPoint = "CredWriteW", SetLastError = true, CharSet = CharSet.Unicode)]
	private static extern bool CredWriteW(ref CREDENTIAL credential, uint flags);

	[DllImport("advapi32.dll", EntryPoint = "CredFree")]
	private static extern void CredFree(IntPtr cred);
}
