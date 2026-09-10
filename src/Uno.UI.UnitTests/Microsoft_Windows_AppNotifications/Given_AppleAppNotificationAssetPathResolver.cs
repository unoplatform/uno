#nullable enable

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_AppleAppNotificationAssetPathResolver
{
	[TestMethod]
	public void When_MsAppx_Path_Contains_Escaped_Characters_It_Is_Decoded_Once()
	{
		var installedPath = CreateInstalledPath();
		try
		{
			var assetsPath = Path.Combine(installedPath, "Assets");
			Directory.CreateDirectory(assetsPath);
			var spacedPath = Path.Combine(assetsPath, "My Image.png");
			var escapedNamePath = Path.Combine(assetsPath, "literal%20.png");
			File.WriteAllText(spacedPath, "image");
			File.WriteAllText(escapedNamePath, "image");

			Assert.IsTrue(AppleAppNotificationAssetPathResolver.TryResolve(
				"ms-appx:///Assets/My%20Image.png",
				installedPath,
				out var resolvedSpacedPath));
			Assert.IsTrue(AppleAppNotificationAssetPathResolver.TryResolve(
				"ms-appx:///Assets/literal%2520.png",
				installedPath,
				out var resolvedEscapedNamePath));

			Assert.AreEqual(spacedPath, resolvedSpacedPath);
			Assert.AreEqual(escapedNamePath, resolvedEscapedNamePath);
		}
		finally
		{
			Directory.Delete(installedPath, recursive: true);
		}
	}

	[TestMethod]
	public void When_File_Paths_Contain_Escaped_Characters_They_Are_Decoded_Once()
	{
		var installedPath = CreateInstalledPath();
		try
		{
			var filePath = Path.Combine(installedPath, "My Image.png");
			var escapedNamePath = Path.Combine(installedPath, "literal%20.png");
			File.WriteAllText(filePath, "image");
			File.WriteAllText(escapedNamePath, "image");

			Assert.IsTrue(AppleAppNotificationAssetPathResolver.TryResolve(
				new Uri(filePath).AbsoluteUri,
				installedPath,
				out var resolvedPath));
			Assert.IsTrue(AppleAppNotificationAssetPathResolver.TryResolve(
				new Uri(escapedNamePath).AbsoluteUri,
				installedPath,
				out var resolvedEscapedNamePath));

			Assert.AreEqual(filePath, resolvedPath);
			Assert.AreEqual(escapedNamePath, resolvedEscapedNamePath);
		}
		finally
		{
			Directory.Delete(installedPath, recursive: true);
		}
	}

	[TestMethod]
	public void When_File_Path_Contains_Encoded_Traversal_It_Is_Rejected()
	{
		var installedPath = CreateInstalledPath();
		try
		{
			var root = new Uri(installedPath + Path.DirectorySeparatorChar).AbsoluteUri;

			Assert.IsFalse(AppleAppNotificationAssetPathResolver.TryResolve(
				root + "Assets/%2E%2E/secret.png",
				installedPath,
				out _));
		}
		finally
		{
			Directory.Delete(installedPath, recursive: true);
		}
	}

	[TestMethod]
	public void When_MsAppx_Path_Contains_Encoded_Traversal_It_Is_Rejected()
	{
		var installedPath = CreateInstalledPath();
		try
		{
			Assert.IsFalse(AppleAppNotificationAssetPathResolver.TryResolve(
				"ms-appx:///Assets/%2E%2E/secret.png",
				installedPath,
				out _));
			Assert.IsFalse(AppleAppNotificationAssetPathResolver.TryResolve(
				"ms-appx:///Assets/%2E%2E%5Csecret.png",
				installedPath,
				out _));
		}
		finally
		{
			Directory.Delete(installedPath, recursive: true);
		}
	}

	private static string CreateInstalledPath()
	{
		var path = Path.Combine(AppContext.BaseDirectory, "AppleAssetPathTests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(path);
		return path;
	}
}
