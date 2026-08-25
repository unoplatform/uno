#nullable enable

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Foundation.Extensibility;
using Windows.Security.Credentials;

namespace Uno.UI.Tests.Windows_Security.Credentials;

[TestClass]
public class Given_PasswordVault
{
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
	public void When_PlatformStoreCannotBeRead_Then_ConstructionFailsClosed()
	{
		Extension.ReadError = new InvalidOperationException("credential store unavailable");

		var error = Assert.ThrowsExactly<PasswordVaultUnavailableException>(
			() => new PasswordVault());

		StringAssert.Contains(error.Message, "could not be read");
		Assert.IsNotNull(error.InnerException);
	}

	private sealed class TestPasswordVaultExtension : IPasswordVaultExtension
	{
		private byte[]? _data;

		internal Exception? ReadError { get; set; }

		public byte[]? Read()
		{
			if (ReadError is not null)
			{
				throw ReadError;
			}

			return _data?.ToArray();
		}

		public void Write(byte[] data)
			=> _data = data.ToArray();

		internal void Reset()
		{
			_data = null;
			ReadError = null;
		}
	}
}
