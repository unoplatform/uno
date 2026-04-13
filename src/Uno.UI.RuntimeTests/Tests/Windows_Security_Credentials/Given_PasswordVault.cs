using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Security.Credentials;
using Windows.Security.Credentials;

namespace Uno.UI.RuntimeTests.Tests.Windows_Security_Credentials;

[TestClass]
public class Given_PasswordVault
{
	private string _resource1 = null!;
	private string _resource2 = null!;

	[TestInitialize]
	public void Init()
	{
		// Isolate every run so we don't pollute the developer's real credential store.
		var suffix = Guid.NewGuid().ToString("N");
		_resource1 = $"uno.tests.vault.{suffix}.A";
		_resource2 = $"uno.tests.vault.{suffix}.B";
	}

	[TestCleanup]
	public void Cleanup()
	{
		if (!PasswordVaultHelper.IsSupported())
		{
			return;
		}

		try
		{
			var vault = new PasswordVault();
			foreach (var cred in vault.RetrieveAll().ToArray())
			{
				if (cred.Resource == _resource1 || cred.Resource == _resource2)
				{
					vault.Remove(cred);
				}
			}
		}
		catch
		{
			// best-effort cleanup
		}
	}

	[TestMethod]
	public void When_Add_Retrieve_Remove_RoundTrip()
	{
		if (!PasswordVaultHelper.IsSupported())
		{
			Assert.Inconclusive("PasswordVault is not supported on this platform.");
		}

		var vault = new PasswordVault();
		vault.Add(new PasswordCredential(_resource1, "alice", "hunter2"));

		var retrieved = vault.Retrieve(_resource1, "alice");
		Assert.AreEqual(_resource1, retrieved.Resource);
		Assert.AreEqual("alice", retrieved.UserName);
		Assert.AreEqual("hunter2", retrieved.Password);

		vault.Remove(retrieved);

		Assert.ThrowsExactly<Exception>(() => vault.Retrieve(_resource1, "alice"));
	}

	[TestMethod]
	public void When_Add_ReplacesExistingPassword()
	{
		if (!PasswordVaultHelper.IsSupported())
		{
			Assert.Inconclusive("PasswordVault is not supported on this platform.");
		}

		var vault = new PasswordVault();
		vault.Add(new PasswordCredential(_resource1, "alice", "old"));
		vault.Add(new PasswordCredential(_resource1, "alice", "new"));

		var credentials = vault.FindAllByResource(_resource1);
		Assert.AreEqual(1, credentials.Count);
		Assert.AreEqual("new", credentials[0].Password);
	}

	[TestMethod]
	public void When_FindAllByResource_Has_No_Match()
	{
		if (!PasswordVaultHelper.IsSupported())
		{
			Assert.Inconclusive("PasswordVault is not supported on this platform.");
		}

		var vault = new PasswordVault();
		Assert.ThrowsExactly<Exception>(() => vault.FindAllByResource(_resource1));
	}

	[TestMethod]
	public void When_Retrieve_Is_Case_Insensitive()
	{
		if (!PasswordVaultHelper.IsSupported())
		{
			Assert.Inconclusive("PasswordVault is not supported on this platform.");
		}

		var vault = new PasswordVault();
		vault.Add(new PasswordCredential(_resource1, "Alice", "secret"));

		var retrieved = vault.Retrieve(_resource1.ToUpperInvariant(), "alice");
		Assert.AreEqual("secret", retrieved.Password);
	}

	[TestMethod]
	public void When_RetrieveAll_Persists_Across_Instances()
	{
		if (!PasswordVaultHelper.IsSupported())
		{
			Assert.Inconclusive("PasswordVault is not supported on this platform.");
		}

		var writer = new PasswordVault();
		writer.Add(new PasswordCredential(_resource1, "alice", "pw1"));
		writer.Add(new PasswordCredential(_resource2, "bob", "pw2"));

		var reader = new PasswordVault();
		var alice = reader.Retrieve(_resource1, "alice");
		var bob = reader.Retrieve(_resource2, "bob");

		Assert.AreEqual("pw1", alice.Password);
		Assert.AreEqual("pw2", bob.Password);
	}
}
