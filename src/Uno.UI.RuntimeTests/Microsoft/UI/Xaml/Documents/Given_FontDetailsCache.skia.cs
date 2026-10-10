using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Composition.Drawing;
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

		[TestMethod]
		public async Task When_Font_Stream_Not_Seekable_Then_Font_Loads()
		{
			var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(FontAsset));
			using var source = await file.OpenStreamForReadAsync();
			using var stream = new NonSeekableStream(source);

			using var fontFile = FontProvider.Current.LoadFontFile(stream);
			var font = fontFile.CreateFont(null, FontWeights.Normal, FontStretch.Normal, FontStyle.Normal, 14f);

			Assert.IsNotNull(font);
			Assert.AreEqual("SymbolsRuntimeTest01", font.FamilyName);
		}

		[TestMethod]
		public void When_Font_Stream_Empty_Then_No_Font()
		{
			using var fontFile = FontProvider.Current.LoadFontFile(new MemoryStream());

			Assert.IsNull(fontFile.CreateFont(null, FontWeights.Normal, FontStretch.Normal, FontStyle.Normal, 14f));
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

		private sealed class NonSeekableStream(Stream inner) : Stream
		{
			public override bool CanRead => true;
			public override bool CanSeek => false;
			public override bool CanWrite => false;
			public override long Length => throw new NotSupportedException();
			public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

			public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
			public override void Flush()
			{
			}
			public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
			public override void SetLength(long value) => throw new NotSupportedException();
			public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
		}
	}
}
