using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
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
		private const string LatinFontAsset = "ms-appx:///Uno.UI.RuntimeTests/Assets/Fonts/Roboto-Regular.ttf";

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

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task When_Size_Changes_Then_Typeface_Tables_Not_Read_Again(bool fromFile)
		{
			const int Sizes = 50;
			var provider = SkiaBackend.CreateFontProvider();
			var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(LatinFontAsset));
			using var stream = await file.OpenStreamForReadAsync();
			using var fontFile = provider.LoadFontFile(stream);

			IFont Create(float size) => fromFile
				? fontFile.CreateFont(null, FontWeights.Normal, FontStretch.Normal, FontStyle.Normal, size)!
				: provider.GetDefaultFont(FontWeights.Normal, FontStretch.Normal, FontStyle.Normal, size);

			Create(10f).Shape("12", TextDirection.LeftToRight);

			var before = GC.GetAllocatedBytesForCurrentThread();
			for (var i = 1; i <= Sizes; i++)
			{
				Create(10f + i / 7f).Shape("12", TextDirection.LeftToRight);
			}
			var perSize = (GC.GetAllocatedBytesForCurrentThread() - before) / Sizes;

			// The GSUB/GPOS/cmap tables alone are tens of KB, read again for every size when the face isn't shared.
			Assert.IsTrue(perSize < 8 * 1024, $"A new font size allocated {perSize} bytes.");
		}

		[TestMethod]
		public async Task When_Face_Shared_Then_Advances_Follow_Size()
		{
			var provider = SkiaBackend.CreateFontProvider();
			var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(LatinFontAsset));
			using var stream = await file.OpenStreamForReadAsync();
			using var fontFile = provider.LoadFontFile(stream);

			float Advance(float size) => fontFile
				.CreateFont(null, FontWeights.Normal, FontStretch.Normal, FontStyle.Normal, size)!
				.Shape("Hamburgefonstiv", TextDirection.LeftToRight)
				.Advances
				.Sum();

			var at10 = Advance(10f);
			var at20 = Advance(20f);

			Assert.IsTrue(at10 > 0);
			Assert.AreEqual(at10 * 2, at20, 0.01f);
			Assert.AreEqual(at10, Advance(10f), 0.001f);
		}

		[TestMethod]
		public void When_Same_Font_Requested_Then_Same_Instance()
		{
			var first = FontDetailsCache.GetFont(null, 777.25f, FontWeights.Normal, FontStretch.Normal, FontStyle.Normal);
			var second = FontDetailsCache.GetFont(null, 777.25f, FontWeights.Normal, FontStretch.Normal, FontStyle.Normal);

			Assert.AreSame(first.details.FontHandle, second.details.FontHandle);
		}

		[TestMethod]
		public async Task When_Many_Sizes_Requested_Then_Old_Fonts_Collectable()
		{
			var oldest = RequestFontAndTrack(5000f);

			for (var i = 0; i < 3000; i++)
			{
				FontDetailsCache.GetFont(null, 5001f + i / 7f, FontWeights.Normal, FontStretch.Normal, FontStyle.Normal);
			}

			for (var i = 0; i < 5 && oldest.IsAlive; i++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				await Task.Yield();
			}

			Assert.IsFalse(oldest.IsAlive, "A font nobody asks for anymore should leave the cache.");
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static WeakReference RequestFontAndTrack(float size)
			=> new(FontDetailsCache.GetFont(null, size, FontWeights.Normal, FontStretch.Normal, FontStyle.Normal).details.FontHandle);

		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
		public async Task When_Local_Font_Not_Loaded_Then_First_Measure_Uses_It()
		{
			var path = await CopyFontToTempFile(LatinFontAsset);
			try
			{
				var source = new Uri(path).AbsoluteUri;
				var sut = new TextBlock { Text = "Measured before the first frame", FontSize = 17, FontFamily = new FontFamily(source) };

				sut.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
				var firstWidth = sut.DesiredSize.Width;

				var (details, loadedTask) = FontDetailsCache.GetFont(source, 17f, FontWeights.Normal, FontStretch.Normal, FontStyle.Normal);
				var loadedDuringRequest = loadedTask.IsCompletedSuccessfully;

				// A load still running keeps the file open, which would fail the delete below and hide these asserts.
				await loadedTask;

				Assert.IsTrue(loadedDuringRequest, "A local font should load during the first request.");
				Assert.AreEqual("Roboto", details.FontHandle.FamilyName);

				sut.InvalidateMeasure();
				sut.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
				Assert.AreEqual(firstWidth, sut.DesiredSize.Width);
			}
			finally
			{
				File.Delete(path);
			}
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

		private static async Task<string> CopyFontToTempFile(string asset = FontAsset)
		{
			var path = Path.Combine(Path.GetTempPath(), $"uno-font-{Guid.NewGuid():N}.ttf");
			var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(asset));
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
