using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Composition.Drawing;
using Uno.UI.Composition.Managed;
using Uno.UI.Composition.Skia;
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

		[TestMethod]
		public async Task When_Family_Not_Installed_Then_Resolved_Through_Provider()
		{
			var family = $"Uno Missing {Guid.NewGuid():N}";
			var original = FontProvider.Current;
			var provider = new FamilyProvider(original, family, await LoadAssetFont());
			FontProvider.Current = provider;
			try
			{
				var details = await FontDetailsCache.GetFont(family, 14f, FontWeights.Normal, FontStretch.Normal, FontStyle.Normal).loadedTask;

				Assert.AreEqual(1, provider.MatchFamilyAsyncCalls);
				Assert.AreEqual("SymbolsRuntimeTest01", details.FontHandle.FamilyName);
			}
			finally
			{
				FontProvider.Current = original;
			}
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task When_Fallback_Disabled_Then_Codepoint_Not_Resolved_From_Other_Fonts(bool managed)
		{
			const int Ideograph = 0x4E2D;
			var enabled = managed ? ManagedBackend.CreateFontProvider() : SkiaBackend.CreateFontProvider();
			var disabled = managed ? ManagedBackend.CreateFontProvider(enableFallback: false) : SkiaBackend.CreateFontProvider(enableFallback: false);

			if (await enabled.MatchCharacterAsync(Ideograph, FontWeights.Normal, FontStretch.Normal, FontStyle.Normal, 14f) is null)
			{
				Assert.Inconclusive("No installed font covers U+4E2D.");
			}

			Assert.IsNull(await disabled.MatchCharacterAsync(Ideograph, FontWeights.Normal, FontStretch.Normal, FontStyle.Normal, 14f));
		}

		private static async Task<IFont> LoadAssetFont()
		{
			var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(FontAsset));
			using var stream = await file.OpenStreamForReadAsync();
			using var fontFile = FontProvider.Current.LoadFontFile(stream);
			return fontFile.CreateFont(null, FontWeights.Normal, FontStretch.Normal, FontStyle.Normal, 14f)!;
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

		// Resolves the family only through MatchFamilyAsync, as a provider that fetches fonts on demand would.
		private sealed class FamilyProvider(IFontProvider inner, string family, IFont font) : IFontProvider
		{
			public int MatchFamilyAsyncCalls { get; private set; }

			public IFont? CreateFont(byte[] data, string? familyNameHint, FontWeight weight, FontStretch stretch, FontStyle style, float fontSize)
				=> inner.CreateFont(data, familyNameHint, weight, stretch, style, fontSize);

			public IFont? MatchFamily(string familyName, FontWeight weight, FontStretch stretch, FontStyle style, float fontSize)
				=> inner.MatchFamily(familyName, weight, stretch, style, fontSize);

			public ValueTask<IFont?> MatchFamilyAsync(string familyName, FontWeight weight, FontStretch stretch, FontStyle style, float fontSize)
			{
				if (familyName != family)
				{
					return inner.MatchFamilyAsync(familyName, weight, stretch, style, fontSize);
				}

				MatchFamilyAsyncCalls++;
				return new(font);
			}

			public ValueTask<IFont?> MatchCharacterAsync(int codepoint, FontWeight weight, FontStretch stretch, FontStyle style, float fontSize)
				=> inner.MatchCharacterAsync(codepoint, weight, stretch, style, fontSize);

			public IFont GetDefaultFont(FontWeight weight, FontStretch stretch, FontStyle style, float fontSize)
				=> inner.GetDefaultFont(weight, stretch, style, fontSize);
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
