#nullable enable

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_AppleAppNotificationNativeContent
{
	[TestMethod]
	public void When_iOS_Attachment_Is_Resolved_Escaping_Is_Decoded_Once()
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

			Assert.AreEqual(
				spacedPath,
				AppleAppNotificationNativeContent.ResolveAttachmentPath(
					"ms-appx:///Assets/My%20Image.png",
					installedPath));
			Assert.AreEqual(
				escapedNamePath,
				AppleAppNotificationNativeContent.ResolveAttachmentPath(
					"ms-appx:///Assets/literal%2520.png",
					installedPath));
			Assert.AreEqual(
				spacedPath,
				AppleAppNotificationNativeContent.ResolveAttachmentPath(
					new Uri(spacedPath).AbsoluteUri,
					string.Empty));
		}
		finally
		{
			Directory.Delete(installedPath, recursive: true);
		}
	}

	[TestMethod]
	public void When_iOS_Attachment_Has_Unsafe_Authority_Or_Traversal_It_Is_Rejected()
	{
		var installedPath = CreateInstalledPath();
		try
		{
			Assert.IsNull(AppleAppNotificationNativeContent.ResolveAttachmentPath(
				"ms-appx://authority/Assets/image.png",
				installedPath));
			Assert.IsNull(AppleAppNotificationNativeContent.ResolveAttachmentPath(
				"ms-appx:///Assets/%2E%2E/secret.png",
				installedPath));
			Assert.IsNull(AppleAppNotificationNativeContent.ResolveAttachmentPath(
				"ms-appx:///Assets/missing.png",
				installedPath));
		}
		finally
		{
			Directory.Delete(installedPath, recursive: true);
		}
	}

	private static string CreateInstalledPath()
	{
		var path = Path.Combine(AppContext.BaseDirectory, "AppleNativeContentTests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(path);
		return path;
	}
}
