#if !WINAPPSDK
#nullable enable

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Storage;

namespace Uno.UI.RuntimeTests.Tests.Windows_Storage
{
	/// <summary>
	/// Tests for <see cref="StorageFile.RegisterApplicationUriAssetAsync"/>, which allows
	/// runtime code to inject asset content so that subsequent
	/// <see cref="StorageFile.GetFileFromApplicationUriAsync"/> calls resolve paths that are
	/// absent from the compile-time <c>uno-assets.txt</c> manifest.
	/// </summary>
	[TestClass]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Wasm)]
	public class Given_StorageFile_RegisterApplicationUriAsset
	{
		[TestMethod]
		public async Task When_Asset_Registered_Then_GetFileFromApplicationUri_Succeeds()
		{
			const string assetPath = "RuntimeTests/RegisteredAsset.txt";
			var expectedContent = Encoding.UTF8.GetBytes("hello from runtime registered asset");

			using var stream = new MemoryStream(expectedContent);
			await StorageFile.RegisterApplicationUriAssetAsync(assetPath, stream);

			var uri = new Uri($"ms-appx:///{assetPath}");
			var file = await StorageFile.GetFileFromApplicationUriAsync(uri);

			Assert.IsNotNull(file);

			var actualBytes = await FileIO.ReadBufferAsync(file);
			Assert.AreEqual((uint)expectedContent.Length, actualBytes.Length, "File size mismatch.");

			var actualContent = Encoding.UTF8.GetString(actualBytes.ToArray());
			Assert.AreEqual("hello from runtime registered asset", actualContent);
		}

		[TestMethod]
		public async Task When_Asset_Registered_With_LeadingSlash_Then_Resolves()
		{
			const string assetPath = "/RuntimeTests/RegisteredAssetWithSlash.txt";
			var content = Encoding.UTF8.GetBytes("leading slash is trimmed");

			using var stream = new MemoryStream(content);
			await StorageFile.RegisterApplicationUriAssetAsync(assetPath, stream);

			// Both with and without the leading slash should resolve.
			var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///RuntimeTests/RegisteredAssetWithSlash.txt"));

			Assert.IsNotNull(file);
		}

		[TestMethod]
		public async Task When_Asset_Registered_Twice_Then_Content_Is_Latest()
		{
			const string assetPath = "RuntimeTests/RegisteredAsset_Overwrite.txt";

			using var first = new MemoryStream(Encoding.UTF8.GetBytes("first content"));
			await StorageFile.RegisterApplicationUriAssetAsync(assetPath, first);

			using var second = new MemoryStream(Encoding.UTF8.GetBytes("second content"));
			await StorageFile.RegisterApplicationUriAssetAsync(assetPath, second);

			var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///{assetPath}"));
			var actualContent = await FileIO.ReadTextAsync(file);

			Assert.AreEqual("second content", actualContent);
		}
	}
}
#endif
