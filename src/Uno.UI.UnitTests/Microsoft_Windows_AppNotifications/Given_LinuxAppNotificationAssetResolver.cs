#nullable enable

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_LinuxAppNotificationAssetResolver
{
	[TestMethod]
	public void When_MsAppx_Uri_Has_Escaped_Characters_Only_The_Lookup_Path_Is_Decoded()
	{
		var packagePath = GetPackagePath();
		var expectedPath = Path.Combine(packagePath, "Assets", "My Icon#%✓.png");
		string? lookupPath = null;

		var resolved = LinuxAppNotificationAssetResolver.ResolveIcon(
			"ms-appx:///Assets/My%20Icon%23%25%E2%9C%93.png",
			packagePath,
			"Contoso.Package",
			path =>
			{
				lookupPath = path;
				return true;
			});

		Assert.AreEqual(expectedPath, lookupPath);
		Assert.AreEqual(GetFileUri(expectedPath), resolved);
	}

	[TestMethod]
	public void When_File_Uri_Has_Escaped_Characters_The_Original_Uri_Is_Returned()
	{
		var packagePath = GetPackagePath();
		var expectedPath = Path.Combine(packagePath, "My Icon#%✓.png");
		var source = GetFileUri(expectedPath);
		string? lookupPath = null;

		var resolved = LinuxAppNotificationAssetResolver.ResolveIcon(
			source,
			packagePath,
			"Contoso.Package",
			path =>
			{
				lookupPath = path;
				return true;
			});

		Assert.AreEqual(expectedPath, lookupPath);
		Assert.AreEqual(source, resolved);
	}

	[TestMethod]
	public void When_MsAppx_Authority_Matches_Current_Package_Asset_Is_Resolved()
	{
		var packagePath = GetPackagePath();
		var expectedPath = Path.Combine(packagePath, "Assets", "icon.png");

		var resolved = LinuxAppNotificationAssetResolver.ResolveIcon(
			"ms-appx://contoso.package/Assets/icon.png",
			packagePath,
			"Contoso.Package",
			path => path == expectedPath);

		Assert.AreEqual(GetFileUri(expectedPath), resolved);
	}

	[TestMethod]
	[DataRow("ms-appx:///Assets/../icon.png")]
	[DataRow("ms-appx:///Assets/%2E%2E/icon.png")]
	[DataRow("ms-appx:///..%2Foutside.png")]
	[DataRow("ms-appx:///Assets%2F..%2F..%2Foutside.png")]
	[DataRow("ms-appx:///Assets/%5C..%5Coutside.png")]
	[DataRow("ms-appx://Other.Package/Assets/icon.png")]
	public void When_MsAppx_Uri_Can_Escape_The_Current_Package_It_Is_Rejected(string source)
	{
		var lookupAttempted = false;

		var resolved = LinuxAppNotificationAssetResolver.ResolveIcon(
			source,
			GetPackagePath(),
			"Contoso.Package",
			_ =>
			{
				lookupAttempted = true;
				return true;
			});

		Assert.AreEqual(string.Empty, resolved);
		Assert.IsFalse(lookupAttempted);
	}

	[TestMethod]
	public void When_Local_Asset_Does_Not_Exist_It_Is_Rejected()
	{
		var packagePath = GetPackagePath();

		Assert.AreEqual(
			string.Empty,
			LinuxAppNotificationAssetResolver.ResolveIcon(
				"ms-appx:///Assets/missing.png",
				packagePath,
				"Contoso.Package",
				_ => false));
		Assert.AreEqual(
			string.Empty,
			LinuxAppNotificationAssetResolver.ResolveIcon(
				GetFileUri(Path.Combine(packagePath, "missing.png")),
				packagePath,
				"Contoso.Package",
				_ => false));
	}

	private static string GetPackagePath()
		=> Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "Package Root"));

	private static string GetFileUri(string path)
		=> new UriBuilder
		{
			Scheme = Uri.UriSchemeFile,
			Host = string.Empty,
			Path = path,
		}.Uri.AbsoluteUri;
}
