#nullable enable

using System;
using System.IO;
using Uno.Extensions.Security.Credentials;
using Uno.Foundation.Extensibility;

namespace Windows.Security.Credentials;

public partial class PasswordVault
{
	public PasswordVault()
		: this(CreateExtensionPersister())
	{
	}

	private static IPersister CreateExtensionPersister()
	{
		if (!ApiExtensibility.CreateInstance<IPasswordVaultExtension>(typeof(PasswordVault), out var extension) || extension is null)
		{
			throw new NotSupportedException(
				"PasswordVault is not supported on this platform. " +
				"Call Uno.Security.Credentials.PasswordVaultHelper.IsSupported() before creating a PasswordVault instance.");
		}

		return new ExtensionPersister(extension);
	}

	private sealed class ExtensionPersister : IPersister
	{
		private readonly IPasswordVaultExtension _extension;

		public ExtensionPersister(IPasswordVaultExtension extension)
		{
			_extension = extension;
		}

		public bool TryOpenRead(out Stream inputStream)
		{
			var data = _extension.TryRead();
			if (data is null || data.Length == 0)
			{
				inputStream = Stream.Null;
				return false;
			}

			inputStream = new MemoryStream(data, writable: false);
			return true;
		}

		public WriteTransaction OpenWrite(out Stream outputStream)
		{
			var buffer = new MemoryStream();
			outputStream = buffer;

			return new WriteTransaction(onCommit: () => _extension.Write(buffer.ToArray()));
		}
	}
}
