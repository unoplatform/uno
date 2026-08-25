#nullable enable

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using Uno.Foundation.Extensibility;
using Windows.Storage;

namespace Windows.Security.Credentials;

partial class PasswordVault
{
	public PasswordVault()
		: this(new SkiaPersister(GetExtension()))
	{
	}

	private static IPasswordVaultExtension GetExtension()
	{
		if (ApiExtensibility.CreateInstance<IPasswordVaultExtension>(typeof(PasswordVault), out var extension))
		{
			return extension;
		}

		if (OperatingSystem.IsBrowser())
		{
			throw new NotSupportedException(
				"A browser cannot persist PasswordVault data in a secure enclave "
				+ "that is isolated from untrusted page code.");
		}

		throw new PlatformNotSupportedException(
			"PasswordVault requires a platform credential-store implementation.");
	}

	private sealed class SkiaPersister : IPersister, ISynchronizedPersister
	{
		private readonly IPasswordVaultExtension _extension;

		public SkiaPersister(IPasswordVaultExtension extension)
			=> _extension = extension;

		public bool TryOpenRead(out Stream inputStream)
		{
			byte[]? data;
			try
			{
				data = _extension.Read();
			}
			catch (Exception error) when (
				error is InvalidOperationException
					or PlatformNotSupportedException
					or DllNotFoundException
					or EntryPointNotFoundException)
			{
				throw new PasswordVaultUnavailableException(
					"The platform credential store could not be read.",
					error);
			}

			if (data is not null)
			{
				inputStream = new ZeroingMemoryStream(data);
				return true;
			}

			inputStream = Stream.Null;
			return false;
		}

		public WriteTransaction OpenWrite(out Stream outputStream)
		{
			var stream = new MemoryStream();
			outputStream = stream;
			return new WriteTransaction(onCommit: Commit);

			void Commit()
			{
				var data = stream.ToArray();
				try
				{
					_extension.Write(data);
				}
				catch (Exception error) when (
					error is InvalidOperationException
						or PlatformNotSupportedException
						or DllNotFoundException
						or EntryPointNotFoundException)
				{
					throw new PasswordVaultUnavailableException(
						"The platform credential store could not be written.",
						error);
				}
				finally
				{
					CryptographicOperations.ZeroMemory(data);
					CryptographicOperations.ZeroMemory(stream.GetBuffer());
				}
			}
		}

		public IDisposable AcquireLock()
		{
			var lockPath = Path.Combine(
				ApplicationData.Current.LocalFolder.Path,
				".password-vault.lock");
			for (var attempt = 0; ; attempt++)
			{
				try
				{
					return new FileStream(
						lockPath,
						FileMode.OpenOrCreate,
						FileAccess.ReadWrite,
						FileShare.None);
				}
				catch (IOException) when (attempt < 100)
				{
					Thread.Sleep(50);
				}
			}
		}
	}

	private sealed class ZeroingMemoryStream : MemoryStream
	{
		private readonly byte[] _buffer;

		internal ZeroingMemoryStream(byte[] buffer)
			: base(buffer, writable: false)
			=> _buffer = buffer;

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				CryptographicOperations.ZeroMemory(_buffer);
			}

			base.Dispose(disposing);
		}
	}
}
