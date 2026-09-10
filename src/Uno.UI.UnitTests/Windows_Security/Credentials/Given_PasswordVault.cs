#nullable enable

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Foundation.Extensibility;
using Windows.Security.Credentials;

namespace Uno.UI.Tests.Windows_Security.Credentials;

/// <summary>
/// The expectations below were captured from the Windows implementation of
/// <c>Windows.Security.Credentials.PasswordVault</c> running in a desktop .NET app on Windows 11.
/// </summary>
[TestClass]
public class Given_PasswordVault
{
	// HRESULT_FROM_WIN32(ERROR_NOT_FOUND), the HRESULT the Windows vault reports for a missing item.
	private const int ElementNotFound = unchecked((int)0x80070490);

	private static readonly TestPasswordVaultExtension Extension = new();

	[ClassInitialize]
	public static void InitializeClass(TestContext _)
		=> ApiExtensibility.Register(
			typeof(IPasswordVaultExtension),
			_ => Extension);

	[TestInitialize]
	public void Initialize()
		=> Extension.Reset();

	[TestMethod]
	public void When_CredentialIsAdded_Then_ItPersistsAcrossVaultInstances()
	{
		var firstVault = new PasswordVault();
		firstVault.Add(new PasswordCredential("repository", "user", "secret"));

		var secondVault = new PasswordVault();
		var credential = secondVault.Retrieve("REPOSITORY", "USER");

		Assert.AreEqual("secret", credential.Password);
	}

	[TestMethod]
	public void When_StaleVaultWrites_Then_ExistingCredentialsArePreserved()
	{
		var firstVault = new PasswordVault();
		var staleVault = new PasswordVault();
		firstVault.Add(new PasswordCredential("first", "user", "one"));

		staleVault.Add(new PasswordCredential("second", "user", "two"));

		var credentials = new PasswordVault().RetrieveAll();
		Assert.AreEqual(2, credentials.Count);
		Assert.AreEqual("one", credentials.Single(item => item.Resource == "first").Password);
		Assert.AreEqual("two", credentials.Single(item => item.Resource == "second").Password);
	}

	[TestMethod]
	public void When_AnotherVaultWrites_Then_ReadsObserveIt()
	{
		var reader = new PasswordVault();
		new PasswordVault().Add(new PasswordCredential("resource", "user", "secret"));

		Assert.AreEqual("secret", reader.Retrieve("resource", "user").Password);
		Assert.AreEqual(1, reader.RetrieveAll().Count);
		Assert.AreEqual(1, reader.FindAllByResource("resource").Count);
	}

	[TestMethod]
	public void When_VaultIsEmpty_Then_RetrieveAllReturnsEmpty()
		=> Assert.AreEqual(0, new PasswordVault().RetrieveAll().Count);

	[TestMethod]
	public void When_LookupIsPerformed_Then_CasingFollowsWindows()
	{
		var vault = new PasswordVault();
		vault.Add(new PasswordCredential("Resource", "User", "secret"));

		// Retrieve is case insensitive on both arguments.
		Assert.AreEqual("secret", vault.Retrieve("RESOURCE", "user").Password);

		// FindAllBy* are case sensitive.
		Assert.AreEqual(1, vault.FindAllByResource("Resource").Count);
		Assert.AreEqual(1, vault.FindAllByUserName("User").Count);
		Assert.AreEqual(ElementNotFound, Assert.ThrowsExactly<Exception>(() => vault.FindAllByResource("resource")).HResult);
		Assert.AreEqual(ElementNotFound, Assert.ThrowsExactly<Exception>(() => vault.FindAllByUserName("user")).HResult);
	}

	[TestMethod]
	public void When_CredentialIsMissing_Then_LookupsReportElementNotFound()
	{
		var vault = new PasswordVault();

		Assert.AreEqual(ElementNotFound, Assert.ThrowsExactly<Exception>(() => vault.Retrieve("missing", "user")).HResult);
		Assert.AreEqual(ElementNotFound, Assert.ThrowsExactly<Exception>(() => vault.FindAllByResource("missing")).HResult);
		Assert.AreEqual(ElementNotFound, Assert.ThrowsExactly<Exception>(() => vault.FindAllByUserName("missing")).HResult);
	}

	[TestMethod]
	public void When_RemovingAnAbsentCredential_Then_ItThrows()
	{
		var vault = new PasswordVault();
		vault.Add(new PasswordCredential("resource", "user", "secret"));

		var error = Assert.ThrowsExactly<Exception>(
			() => vault.Remove(new PasswordCredential("other", "user", "secret")));

		Assert.AreEqual(ElementNotFound, error.HResult);
		Assert.AreEqual(1, vault.RetrieveAll().Count);
	}

	[TestMethod]
	public void When_RemovingWithDifferentCasingAndPassword_Then_ItSucceeds()
	{
		var vault = new PasswordVault();
		vault.Add(new PasswordCredential("resource", "user", "secret"));

		vault.Remove(new PasswordCredential("RESOURCE", "USER", "not-the-password"));

		Assert.AreEqual(0, new PasswordVault().RetrieveAll().Count);
	}

	[TestMethod]
	public void When_ArgumentsAreInvalid_Then_ArgumentExceptionIsThrown()
	{
		var vault = new PasswordVault();

		Assert.ThrowsExactly<ArgumentException>(() => vault.Add(null!));
		Assert.ThrowsExactly<ArgumentException>(() => vault.Remove(null!));
		Assert.ThrowsExactly<ArgumentException>(() => vault.Retrieve(null!, "user"));
		Assert.ThrowsExactly<ArgumentException>(() => vault.Retrieve("", "user"));
		Assert.ThrowsExactly<ArgumentException>(() => vault.Retrieve("resource", null!));
		Assert.ThrowsExactly<ArgumentException>(() => vault.Retrieve("resource", ""));
		Assert.ThrowsExactly<ArgumentException>(() => vault.FindAllByResource(null!));
		Assert.ThrowsExactly<ArgumentException>(() => vault.FindAllByResource(""));
		Assert.ThrowsExactly<ArgumentException>(() => vault.FindAllByUserName(null!));
		Assert.ThrowsExactly<ArgumentException>(() => vault.FindAllByUserName(""));
	}

	[TestMethod]
	public void When_UserNameCasingChanges_Then_TheEntryIsReplaced()
	{
		var vault = new PasswordVault();
		vault.Add(new PasswordCredential("resource", "user", "first"));

		vault.Add(new PasswordCredential("resource", "USER", "second"));

		var credentials = new PasswordVault().RetrieveAll();
		Assert.AreEqual(1, credentials.Count);
		Assert.AreEqual("USER", credentials[0].UserName);
		Assert.AreEqual("second", credentials[0].Password);
	}

	[TestMethod]
	public void When_PasswordIsUnchanged_Then_TheStoreIsNotRewritten()
	{
		var vault = new PasswordVault();
		vault.Add(new PasswordCredential("resource", "user", "secret"));
		var writes = Extension.WriteCount;

		vault.Add(new PasswordCredential("resource", "user", "secret"));

		Assert.AreEqual(writes, Extension.WriteCount);
	}

	[TestMethod]
	public void When_UnicodeSecretIsStored_Then_ItRoundTrips()
	{
		var vault = new PasswordVault();
		vault.Add(new PasswordCredential("r\u00e9source\ud83d\ude00", "\u00fcser", "p\u00e4ss\u4e2d\ud83d\ude00"));

		Assert.AreEqual(
			"p\u00e4ss\u4e2d\ud83d\ude00",
			new PasswordVault().Retrieve("r\u00e9source\ud83d\ude00", "\u00fcser").Password);
	}

	[TestMethod]
	public void When_PlatformStoreCannotBeRead_Then_ConstructionFailsClosed()
	{
		Extension.ReadError = new InvalidOperationException("credential store unavailable");

		var error = Assert.ThrowsExactly<PasswordVaultUnavailableException>(
			() => new PasswordVault());

		StringAssert.Contains(error.Message, "could not be read");
		Assert.IsNotNull(error.InnerException);
	}

	[TestMethod]
	public void When_PlatformStoreCannotBeWritten_Then_AddFailsClosed()
	{
		var vault = new PasswordVault();
		Extension.WriteError = new InvalidOperationException("credential store unavailable");

		var error = Assert.ThrowsExactly<PasswordVaultUnavailableException>(
			() => vault.Add(new PasswordCredential("resource", "user", "secret")));

		StringAssert.Contains(error.Message, "could not be written");
		Extension.WriteError = null;
		Assert.AreEqual(0, new PasswordVault().RetrieveAll().Count);
	}

	[TestMethod]
	public void When_SecretsCrossTheNativeBoundary_Then_TransientBuffersAreZeroed()
	{
		var vault = new PasswordVault();
		vault.Add(new PasswordCredential("resource", "user", "secret"));

		Assert.IsNotNull(Extension.LastWriteBuffer);
		CollectionAssert.AreEqual(new byte[Extension.LastWriteBuffer!.Length], Extension.LastWriteBuffer);

		_ = new PasswordVault();

		Assert.IsNotNull(Extension.LastReadBuffer);
		CollectionAssert.AreEqual(new byte[Extension.LastReadBuffer!.Length], Extension.LastReadBuffer);
	}

	private sealed class TestPasswordVaultExtension : IPasswordVaultExtension
	{
		private byte[]? _data;

		internal Exception? ReadError { get; set; }

		internal Exception? WriteError { get; set; }

		internal int WriteCount { get; private set; }

		internal byte[]? LastReadBuffer { get; private set; }

		internal byte[]? LastWriteBuffer { get; private set; }

		public byte[]? Read()
		{
			if (ReadError is not null)
			{
				throw ReadError;
			}

			return LastReadBuffer = _data?.ToArray();
		}

		public void Write(byte[] data)
		{
			LastWriteBuffer = data;

			if (WriteError is not null)
			{
				throw WriteError;
			}

			WriteCount++;
			_data = data.ToArray();
		}

		internal void Reset()
		{
			_data = null;
			ReadError = null;
			WriteError = null;
			WriteCount = 0;
			LastReadBuffer = null;
			LastWriteBuffer = null;
		}
	}
}
