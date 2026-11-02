using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Composition.Drawing;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Storage;
using Windows.UI.Text;
using static Private.Infrastructure.TestServices;

#nullable enable

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Documents
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_FontDetailsCache
	{
		private const string FontAsset = "ms-appx:///Uno.UI.RuntimeTests/Assets/Fonts/uno-fluentui-assets-runtimetest01.ttf";
		private const string UnknownFamily = "Uno Test Font That Does Not Exist";

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

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/20828")]
		public async Task When_Family_Unknown_Then_Default_Text_Font_Is_Used()
		{
			var (_, defaultTask) = FontDetailsCache.GetFont(null, 36, FontWeights.Bold, FontStretch.Normal, FontStyle.Normal);
			var (_, unknownTask) = FontDetailsCache.GetFont(UnknownFamily, 36, FontWeights.Bold, FontStretch.Normal, FontStyle.Normal);

			var defaultFont = (await defaultTask).FontHandle;
			var unknownFont = (await unknownTask).FontHandle;

			Assert.AreEqual(defaultFont.FamilyName, unknownFont.FamilyName);
			// The advance tells the bold face from the regular one, which the family name doesn't.
			Assert.AreEqual(GetAdvance(defaultFont, 'W'), GetAdvance(unknownFont, 'W'));
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/20828")]
		public async Task When_Family_Unknown_Then_TextBlock_Measures_Like_Default()
		{
			var defaultText = new TextBlock { Text = "5.0", FontSize = 36, FontWeight = FontWeights.Bold };
			var unknownText = new TextBlock { Text = "5.0", FontSize = 36, FontWeight = FontWeights.Bold, FontFamily = new FontFamily(UnknownFamily) };
			var panel = new StackPanel { Children = { defaultText, unknownText } };

			try
			{
				await UITestHelper.Load(panel);
				await WindowHelper.WaitFor(() => unknownText.ActualWidth > 0 && Math.Abs(unknownText.ActualWidth - defaultText.ActualWidth) < 0.5, 3000, "unknown family measured like the default font");

				Assert.AreEqual(defaultText.ActualWidth, unknownText.ActualWidth, 0.5);
				Assert.AreEqual(defaultText.ActualHeight, unknownText.ActualHeight, 0.5);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		private static float GetAdvance(IFont font, char character) => font.GetGlyphAdvance(font.GetGlyphIndex(character));

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
