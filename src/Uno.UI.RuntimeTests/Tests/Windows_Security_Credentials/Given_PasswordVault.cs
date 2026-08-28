#nullable enable

using System;
using System.Linq;
using Windows.Security.Credentials;

namespace Uno.UI.RuntimeTests.Tests.Windows_Security_Credentials;

/// <summary>
/// Exercises <see cref="PasswordVault"/> against the real platform credential store
/// (Windows DPAPI, macOS Keychain, Linux Secret Service). Every test uses a dedicated
/// resource name and removes only its own entries.
/// </summary>
[TestClass]
[PlatformCondition(ConditionMode.Include,
	RuntimeTestPlatforms.SkiaWin32
	| RuntimeTestPlatforms.SkiaX11
	| RuntimeTestPlatforms.SkiaMacOS
	| RuntimeTestPlatforms.SkiaFrameBuffer)]
public class Given_PasswordVault
{
	private const string ResourcePrefix = "uno-runtime-tests/PasswordVault/";

	[TestMethod]
	public void When_CredentialIsAdded_Then_AnotherVaultReadsItBack()
	{
		var vault = CreateVault();
		var resource = ResourcePrefix + Guid.NewGuid().ToString("N");

		try
		{
			vault.Add(new PasswordCredential(resource, "runtime-user", "runtime-secret"));

			var credential = new PasswordVault().Retrieve(resource, "runtime-user");
			credential.RetrievePassword();

			Assert.AreEqual("runtime-secret", credential.Password);
		}
		finally
		{
			Cleanup(resource, "runtime-user");
		}
	}

	[TestMethod]
	public void When_ResourceCaseDiffers_Then_RetrieveStillMatches()
	{
		var vault = CreateVault();
		var resource = ResourcePrefix + Guid.NewGuid().ToString("N");

		try
		{
			vault.Add(new PasswordCredential(resource, "Runtime-User", "runtime-secret"));

			// UWP resolves Retrieve case-insensitively but FindAllByResource case-sensitively.
			var credential = vault.Retrieve(resource.ToUpperInvariant(), "RUNTIME-USER");
			credential.RetrievePassword();

			Assert.AreEqual("runtime-secret", credential.Password);
			Assert.ThrowsExactly<Exception>(() => vault.FindAllByResource(resource.ToUpperInvariant()));
		}
		finally
		{
			Cleanup(resource, "Runtime-User");
		}
	}

	[TestMethod]
	public void When_CredentialIsRemoved_Then_ItIsGoneFromTheNativeStore()
	{
		var vault = CreateVault();
		var resource = ResourcePrefix + Guid.NewGuid().ToString("N");

		try
		{
			vault.Add(new PasswordCredential(resource, "runtime-user", "runtime-secret"));
			vault.Remove(new PasswordCredential(resource, "runtime-user", "runtime-secret"));

			var reopened = new PasswordVault();
			Assert.ThrowsExactly<Exception>(() => reopened.Retrieve(resource, "runtime-user"));
			Assert.IsFalse(reopened.RetrieveAll().Any(item => item.Resource == resource));
		}
		finally
		{
			Cleanup(resource, "runtime-user");
		}
	}

	[TestMethod]
	public void When_UnicodeSecretIsStored_Then_ItRoundTrips()
	{
		var vault = CreateVault();
		var resource = ResourcePrefix + Guid.NewGuid().ToString("N") + "-\u00e9\u4e2d\u6587\ud83d\ude00";

		try
		{
			vault.Add(new PasswordCredential(resource, "\u00fcser\ud83d\ude00", "p\u00e4ss\u4e2d\ud83d\ude00"));

			var credential = new PasswordVault().Retrieve(resource, "\u00fcser\ud83d\ude00");
			credential.RetrievePassword();

			Assert.AreEqual("p\u00e4ss\u4e2d\ud83d\ude00", credential.Password);
		}
		finally
		{
			Cleanup(resource, "\u00fcser\ud83d\ude00");
		}
	}

	private static PasswordVault CreateVault()
	{
		try
		{
			return new PasswordVault();
		}
		catch (Exception error) when (error is InvalidOperationException
			or PlatformNotSupportedException
			or NotSupportedException
			or NotImplementedException)
		{
			Assert.Inconclusive($"No usable platform credential store: {error.GetType().FullName}: {error.Message}");
			throw;
		}
	}

	private static void Cleanup(string resource, string userName)
	{
		try
		{
			new PasswordVault().Remove(new PasswordCredential(resource, userName, string.Empty));
		}
		catch (Exception)
		{
			// The credential was already absent, or the store is unavailable; nothing else to undo.
		}
	}
}
