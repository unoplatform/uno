#pragma warning disable CS0628  // new protected member declared in sealed class ==> Class is not sealed on all platforms
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Windows.Storage;
using Uno.Extensions;
using Uno.Foundation.Logging;

namespace Windows.Security.Credentials
{
	public partial class PasswordVault
	{
		private readonly object _updateGate = new object();
		private readonly IPersister _persister;

		private ImmutableList<PasswordCredential> _credentials;

		protected PasswordVault(IPersister persister)
		{
			_persister = persister ?? throw new ArgumentNullException(nameof(persister));
			_credentials = Load();
		}

		public IReadOnlyList<PasswordCredential> RetrieveAll()
			=> _credentials;

		public IReadOnlyList<PasswordCredential> FindAllByResource(string resource)
		{
			// UWP: 'resource' is case sensitive
			var result = _credentials.Where(cred => cred.Resource == resource).ToImmutableList();
			if (result.IsEmpty)
			{
				throw new("No match"); // UWP: Throw 'Exception' if no match
			}

			return result;
		}

		public IReadOnlyList<PasswordCredential> FindAllByUserName(string userName)
		{
			// UWP: 'userName' is case sensitive
			var result = _credentials.Where(cred => cred.UserName == userName).ToImmutableList();
			if (result.IsEmpty)
			{
				throw new("No match"); // UWP: Throw 'Exception' if no match
			}

			return result;
		}

		public PasswordCredential Retrieve(string resource, string userName)
		{
			// UWP: Retrieve is case IN-sensitive for both 'resource' and 'userName'
			var result = _credentials.FirstOrDefault(cred => Comparer.Instance.Equals(cred, resource, userName));
			if (result == null)
			{
				throw new("No match"); // UWP: Throw 'Exception' if no match
			}

			return result;
		}

		public void Remove(PasswordCredential credential)
		{
			while (true)
			{
				var capture = _credentials;
				var updated = capture.Remove(credential, Comparer.Instance);

				if (capture == updated)
				{
					return;
				}

				lock (_updateGate)
				{
					if (capture == _credentials)
					{
						Persist(updated);
						_credentials = updated;

						return;
					}
				}
			}
		}

		public void Add(PasswordCredential credential)
		{
			while (true)
			{
				var capture = _credentials;
				var existing = capture.FirstOrDefault(c => Comparer.Instance.Equals(c, credential));

				ImmutableList<PasswordCredential> updated;
				if (existing == null)
				{
					updated = capture.Add(credential);
				}
				else
				{
					existing.RetrievePassword();
					credential.RetrievePassword();

					if (existing.Password == credential.Password)
					{
						// no change, abort update!
						return;
					}

					updated = capture.Replace(existing, credential);
				}

				lock (_updateGate)
				{
					if (capture == _credentials)
					{
						Persist(updated);
						_credentials = updated;

						return;
					}
				}
			}
		}

		public ImmutableList<PasswordCredential> Load()
		{
			try
			{
				if (_persister.TryOpenRead(out var src))
				{
					using (src)
					using (var reader = new BinaryReader(src))
					{
						var count = reader.ReadInt32();
						var credentials = ImmutableList.CreateBuilder<PasswordCredential>();

						for (var i = 0; i < count; i++)
						{
							var res = reader.ReadString();
							var use = reader.ReadString();
							var pwd = reader.ReadString();

							credentials.Add(new PasswordCredential(res, use, pwd));
						}

						return credentials.ToImmutable();
					}
				}
			}
			catch (Exception e)
			{
				this.Log().Warn("Failed to load values from persister, assume empty.", e);
			}

			return ImmutableList<PasswordCredential>.Empty;
		}

		public void Persist(ImmutableList<PasswordCredential> credentials)
		{
			using (var transaction = _persister.OpenWrite(out var dst))
			using (dst)
			using (var writer = new BinaryWriter(dst))
			{
				writer.Write(credentials.Count);

				foreach (var credential in credentials)
				{
					credential.RetrievePassword();

					writer.Write(credential.Resource);
					writer.Write(credential.UserName);
					writer.Write(credential.Password);
				}

				writer.Flush();
				transaction.Commit();
			}
		}

		/// <summary>
		/// A persister responsible to securely persist the credentials managed by a PasswordVault
		/// </summary>
		protected interface IPersister
		{
			/// <summary>
			/// Tries to open the source stream from which credentials can be read.
			/// </summary>
			/// <param name="inputStream">The source stream which should be parsed to reload credentials</param>
			/// <returns>A bool which indicates if the <paramref name="inputStream"/> is valid or not.</returns>
			bool TryOpenRead(out Stream inputStream);

			/// <summary>
			/// Open the target stream which where credentials should be stored.
			/// </summary>
			/// <param name="outputStream">The target stream where credentials can be stored</param>
			/// <returns>A <see cref="WriteTransaction"/> which ensure to atomatically update the credentials</returns>
			WriteTransaction OpenWrite(out Stream outputStream);
		}

		/// <summary>
		/// A transaction used to persist credentials to ensure ACID
		/// </summary>
		protected sealed class WriteTransaction : IDisposable
		{
			private readonly Action _onCommit;
			private readonly Action<bool> _onComplete;

			private int _state = State.New;

			private static class State
			{
				public const int New = 0;
				public const int Commited = 1;
				public const int Disposed = int.MaxValue;
			}

			/// <summary>
			/// Creates a new transaction
			/// </summary>
			/// <param name="onCommit">Callback invoked when this transaction is committed (cf. <see cref="Commit"/>.</param>
			/// <param name="onComplete">Callback invoked when this transaction completes (i.e. Disposed).</param>
			public WriteTransaction(Action onCommit = null, Action<bool> onComplete = null)
			{
				_onCommit = onCommit;
				_onComplete = onComplete;
			}

			/// <summary>
			/// A boolean which indicates if this transaction was committed or not (cf. <see cref="Commit"/>).
			/// </summary>
			public bool IsCommited
			{
				get
				{
					var state = _state;
					if (state == State.Disposed)
					{
						throw new ObjectDisposedException(nameof(WriteTransaction));
					}

					return state == State.Commited;
				}
			}

			/// <summary>
			/// Makes the changes persistent
			/// </summary>
			public void Commit()
			{
				switch (Interlocked.CompareExchange(ref _state, State.Commited, State.New))
				{
					case State.New:
						_onCommit?.Invoke();
						break;

					case State.Disposed:
						throw new ObjectDisposedException(nameof(WriteTransaction));
				}
			}

			/// <inheritdoc />
			public void Dispose()
			{
				var previousState = Interlocked.Exchange(ref _state, State.Disposed);
				if (previousState != State.Disposed)
				{
					_onComplete?.Invoke(previousState == State.Commited);
				}
			}
		}

		/// <summary>
		/// A base class to persist a PasswordVault in a file on the disk
		/// </summary>
		protected abstract class FilePersister : IPersister
		{
			private readonly string _tmp;
			private readonly string _dst;

			/// <summary>
			/// Creates a new instance
			/// </summary>
			/// <param name="filePath">The path where the vault should be persisted</param>
			protected FilePersister(string filePath = null)
			{
				_dst = filePath ?? Path.Combine(ApplicationData.Current.LocalFolder.Path, ".vault");
				_tmp = _dst + ".tmp";
			}

			/// <summary>
			/// Wraps a given encrypted stream into a stream which ensure decryption
			/// </summary>
			/// <param name="outputStream">The encrypted stream</param>
			/// <returns>The decrypted stream</returns>
			protected abstract Stream Encrypt(Stream outputStream);

			/// <summary>
			/// Wraps a given raw stream into a stream which ensure encryption
			/// </summary>
			/// <param name="inputStream">The raw stream</param>
			/// <returns>The encrypted stream</returns>
			protected abstract Stream Decrypt(Stream inputStream);

			/// <inheritdoc />
			public bool TryOpenRead(out Stream inputStream)
			{
				var dst = new FileInfo(_dst);

				var exists = dst.Exists;

				if (exists)
				{
					var length = dst.Length;

					inputStream = Decrypt(File.Open(_dst, FileMode.Open, FileAccess.Read, FileShare.Read));
					return true;
				}
				else
				{
					inputStream = Stream.Null;
					return false;
				}
			}

			/// <inheritdoc />
			public WriteTransaction OpenWrite(out Stream outputStream)
			{
				var fileStream = File.Open(_tmp, FileMode.Create, FileAccess.Write, FileShare.None);
				var encryptedStream = Encrypt(fileStream);

				outputStream = encryptedStream;

				return new WriteTransaction(onComplete: Complete);

				void Complete(bool isCommitted)
				{
					// The encryptedStream has been disposed by the "Persist" but make sure to dispose
					// the underlying file stream before accessing to the file itself.
					//fileStream.Flush();
					//fileStream.Close();
					fileStream.Dispose();

					if (!isCommitted)
					{
						return;
					}

					if (File.Exists(_dst))
					{
						// Note: We don't use the backup file. We don't want that removed credentials
						//		 can be restored by altering the current '_dst' file.
						File.Replace(_tmp, _dst, null, ignoreMetadataErrors: true);
					}
					else
					{
						File.Move(_tmp, _dst);
					}
				}
			}
		}

		/// <summary>
		/// A basically encrypted persister which does not provide an acceptable security level for sensitive data like a password.
		/// </summary>
		protected sealed class UnsecuredPersister : FilePersister
		{
			private readonly byte[] _key;
			private readonly byte[] _iv;

			/// <summary>
			/// Creates a new instance providing the secrets for encryption (TripleDES)
			/// </summary>
			/// <param name="key">The key, must be 24 bytes length</param>
			/// <param name="iv">The IV, must be 8 bytes length</param>
			/// <param name="filePath">The path where the vault should be persisted</param>
			public UnsecuredPersister(byte[] key, byte[] iv, string filePath = null)
				: base(filePath)
			{
				_key = key ?? throw new ArgumentNullException(nameof(key));
				_iv = iv ?? throw new ArgumentNullException(nameof(iv));

				if (_key.Length != 24)
				{
					throw new InvalidOperationException("The secret must have 24 bytes");
				}
				if (_iv.Length != 8)
				{
					throw new InvalidOperationException("The iv must have 8 bytes");
				}
			}

			/// <summary>
			/// Creates a new instance providing a simple password used to encrypt the file
			/// </summary>
			/// <param name="password">The password used to encrypt the file</param>
			/// <param name="filePath">The path where the vault should be persisted</param>
			public UnsecuredPersister(string password = null, string filePath = null)
				: base(filePath)
			{
				(_key, _iv) = GenerateSecrets(password ?? GetEntryPointIdentifier());
			}

			private static string GetEntryPointIdentifier()
			{
				var assembly = Assembly.GetEntryAssembly();
				var method = assembly.EntryPoint.DeclaringType;

				return assembly.GetName().Name + method.DeclaringType.FullName;
			}

			private static (byte[] key, byte[] iv) GenerateSecrets(string password)
			{
				if (string.IsNullOrWhiteSpace(password))
				{
					throw new ArgumentOutOfRangeException(nameof(password), "Password is empty");
				}

				var key = new byte[24];
				var iv = new byte[8];

				var src = SHA256.HashData(Encoding.UTF8.GetBytes(password));

				Array.Copy(src, 0, key, 0, 24);
				Array.Copy(src, 24, iv, 0, 8);

				return (key, iv);
			}

			protected override Stream Decrypt(Stream input)
				=> new CryptoStream(input, TripleDES.Create().CreateDecryptor(_key, _iv), CryptoStreamMode.Read);

			protected override Stream Encrypt(Stream output)
				=> new CryptoStream(output, TripleDES.Create().CreateEncryptor(_key, _iv), CryptoStreamMode.Write);
		}

		private class Comparer : EqualityComparer<PasswordCredential>
		{
			public static readonly Comparer Instance = new Comparer();

			public bool Equals(PasswordCredential obj, string resource, string userName)
				=> StringComparer.OrdinalIgnoreCase.Equals(obj.Resource, resource)
					&& StringComparer.OrdinalIgnoreCase.Equals(obj.UserName, userName);

			public override bool Equals(PasswordCredential left, PasswordCredential right)
				=> StringComparer.OrdinalIgnoreCase.Equals(left.Resource, right.Resource)
					&& StringComparer.OrdinalIgnoreCase.Equals(left.UserName, right.UserName);

			public override int GetHashCode(PasswordCredential obj)
				=> StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Resource)
					^ StringComparer.OrdinalIgnoreCase.GetHashCode(obj.UserName);
		}
	}
}
