using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Android.OS;
using Android.Runtime;
using Android.Security;
using Android.Security.Keystore;
using Java.IO;
using Java.Nio.Charset;
using Java.Security;
using Javax.Crypto;
using Javax.Crypto.Spec;
using CipherMode = Javax.Crypto.CipherMode;

namespace Windows.Security.Credentials
{
	sealed partial class PasswordVault
	{
		public PasswordVault()
			: this(Build.VERSION.SdkInt > BuildVersionCodes.LollipopMr1 ? new KeyStorePersister() : (IPersister)new UnSecureKeyStorePersister())
		{
		}

		public PasswordVault(string filePath)
			: this(Build.VERSION.SdkInt > BuildVersionCodes.LollipopMr1 ? new KeyStorePersister() : (IPersister)new UnSecureKeyStorePersister())
		{
		}

		private sealed class KeyStorePersister : FilePersister
		{
			private const string _notSupported = @"There is no way to properly persist secured content on this device.
The 'AndroidKeyStore' is missing (or is innacessible), but it is a requirement for the 'PasswordVault' to store data securly.
This usually means that the device is using an API older than 18 (4.3). More details: https://developer.android.com/reference/java/security/KeyStore";

			private const string _algo = KeyProperties.KeyAlgorithmAes;
			private const string _block = KeyProperties.BlockModeCbc;
			private const string _padding = KeyProperties.EncryptionPaddingPkcs7;
			private const string _fullTransform = _algo + "/" + _block + "/" + _padding;
			private const string _provider = "AndroidKeyStore";
			private const string _alias = "uno_passwordvault";
			private static readonly byte[] _iv = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(_alias));

			private readonly IKey? _key;

			public KeyStorePersister(string? filePath = null)
				: base(filePath)
			{
				KeyStore? store;
				try
				{
					store = KeyStore.GetInstance(_provider);
				}
				catch (Exception e)
				{
					throw new NotSupportedException(_notSupported, e);
				}
				if (store == null)
				{
					throw new NotSupportedException(_notSupported);
				}

				store.Load(null);

				if (store.ContainsAlias(_alias))
				{
					var key = store.GetKey(_alias, null);

					_key = key;
				}
				else
				{
					var generator = KeyGenerator.GetInstance(_algo, _provider);
					generator!.Init(new KeyGenParameterSpec.Builder(_alias, KeyStorePurpose.Encrypt | KeyStorePurpose.Decrypt)
						.SetBlockModes(_block)
						.SetEncryptionPaddings(_padding)
						.SetRandomizedEncryptionRequired(false)
						.Build());
					_key = generator.GenerateKey();
				}
			}

			/// <inheritdoc />
			protected override Stream Encrypt(Stream outputStream)
			{
				var cipher = Cipher.GetInstance(_fullTransform);
				var iv = new IvParameterSpec(_iv, 0, cipher!.BlockSize);

				cipher.Init(CipherMode.EncryptMode, _key, iv);

				return new CipherStreamAdapter(new CipherOutputStream(outputStream, cipher));
			}

			/// <inheritdoc />
			protected override Stream Decrypt(Stream inputStream)
			{
				var cipher = Cipher.GetInstance(_fullTransform);
				var iv = new IvParameterSpec(_iv, 0, cipher!.BlockSize);

				cipher.Init(CipherMode.DecryptMode, _key, iv);

				return new InputStreamInvoker(new CipherInputStream(inputStream, cipher));
			}

			private class CipherStreamAdapter : Stream
			{
				private readonly CipherOutputStream _output;
				private readonly Stream _adapter;

				private bool _isDisposed;

				public CipherStreamAdapter(CipherOutputStream output)
				{
					_output = output;
					_adapter = new OutputStreamInvoker(output);
				}

				public override bool CanRead => _adapter.CanRead;

				public override bool CanSeek => _adapter.CanSeek;

				public override bool CanWrite => _adapter.CanWrite;

				public override long Length => _adapter.Length;

				public override long Position
				{
					get => _adapter.Position;
					set => _adapter.Position = value;
				}

				public override void Flush()
					=> _adapter.Flush();

				protected override void Dispose(bool disposing)
				{
					if (_isDisposed)
					{
						// We cannot .Close() the _output multiple times.
						return;
					}
					_isDisposed = true;

					if (disposing)
					{
						_output.Close();
					}

					_adapter.Dispose();
					_output.Dispose();
					base.Dispose(disposing);
				}

				public override int Read(byte[] buffer, int offset, int count)
					=> _adapter.Read(buffer, offset, count);

				public override long Seek(long offset, SeekOrigin origin)
					=> _adapter.Seek(offset, origin);

				public override void SetLength(long value)
					=> _adapter.SetLength(value);

				public override void Write(byte[] buffer, int offset, int count)
					=> _adapter.Write(buffer, offset, count);
			}
		}

		/// <summary>
		/// Persister for devices bellow Android level 23.
		/// RSA/ECB/PKCS1Padding only is supported and is not considered secure.
		/// </summary>
		private sealed class UnSecureKeyStorePersister : FilePersister
		{
			private const string _notSupported = @"RSA/ECB/PKCS1Padding with asymetric key is considered not secure and will not  be supported for device under API level 23";

			public UnSecureKeyStorePersister(string? filePath = null)
				: base(filePath)
			{
				throw new NotSupportedException(_notSupported);
			}

			protected override Stream Decrypt(Stream inputStream)
			{
				throw new NotSupportedException(_notSupported);
			}

			protected override Stream Encrypt(Stream outputStream)
			{
				throw new NotSupportedException(_notSupported);
			}
		}

	}
}
