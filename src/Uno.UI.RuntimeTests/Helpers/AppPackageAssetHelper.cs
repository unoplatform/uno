using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Uno.UI.RuntimeTests.Helpers;

internal static class AppPackageAssetHelper
{
	/// <summary>Reads a file of the app package, such as a test font, by its <c>ms-appx:///</c> URI.</summary>
	public static async Task<byte[]> ReadAllBytesAsync(string uri)
	{
		var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(uri));
		using var stream = await file.OpenStreamForReadAsync();
		using MemoryStream buffer = new();
		await stream.CopyToAsync(buffer);
		return buffer.ToArray();
	}
}
