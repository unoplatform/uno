using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Storage;
using Windows.UI.Text;

#nullable enable

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Documents
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_FontDetailsCache
	{
		private const string FontAsset = "ms-appx:///Uno.UI.RuntimeTests/Assets/Fonts/uno-fluentui-assets-runtimetest01.ttf";

		[TestMethod]
		public async Task When_Uri_Font_Loaded_Then_Bytes_Not_Staged_In_Managed_Memory()
		{
			// The cache is keyed by uri and never evicted, so each load needs a file it has not seen yet.
			var warmUp = await CopyFontToTempFile();
			var measured = await CopyFontToTempFile();
			try
			{
				await LoadFont(warmUp);

				var fontLength = new FileInfo(measured).Length;
				var before = GC.GetTotalAllocatedBytes(precise: true);
				var details = await LoadFont(measured);
				var allocated = GC.GetTotalAllocatedBytes(precise: true) - before;

				Assert.AreEqual("SymbolsRuntimeTest01", details.FontHandle.FamilyName);

				// Staging through a MemoryStream costs several times the file size (doubling growth plus ToArray).
				Assert.IsTrue(
					allocated < fontLength / 2,
					$"Loading a {fontLength} byte font allocated {allocated} managed bytes");
			}
			finally
			{
				File.Delete(warmUp);
				File.Delete(measured);
			}
		}

		private static async Task<FontDetails> LoadFont(string path)
			=> await FontDetailsCache.GetFont(new Uri(path).AbsoluteUri, 14f, FontWeights.Normal, FontStretch.Normal, FontStyle.Normal).loadedTask;

		private static async Task<string> CopyFontToTempFile()
		{
			var path = Path.Combine(Path.GetTempPath(), $"uno-font-{Guid.NewGuid():N}.ttf");
			var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(FontAsset));
			using var source = await file.OpenStreamForReadAsync();
			using var target = File.Create(path);
			await source.CopyToAsync(target);
			return path;
		}
	}
}
