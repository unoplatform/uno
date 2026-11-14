#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Composition.Drawing;
using Windows.UI.Text;

namespace Uno.UI.Tests.CompositionTests;

// FontFallback.NotoFontsBaseUri: the browser's Noto fallback fonts served from an app-chosen origin
// (a CSP-locked or offline deployment) instead of raw.githubusercontent.com.
[UITestClass]
public class Given_NotoFontsBaseUri
{
	private static readonly string[] _allRawUrls = FallbackFontMaps.FontWeightsToRawUrls.Values
		.SelectMany(variants => variants.Values)
		.ToArray();

	[TestCleanup]
	public void Cleanup() => FontFallback.NotoFontsBaseUri = NotoFontFallbackService.DefaultBaseUri;

	[TestMethod]
	public void When_Default_Then_Every_Map_Url_Is_Unchanged()
	{
		Assert.AreEqual(new Uri("https://raw.githubusercontent.com/"), FontFallback.NotoFontsBaseUri);
		Assert.IsTrue(_allRawUrls.Length > 100);
		foreach (var raw in _allRawUrls)
		{
			Assert.AreEqual(new Uri(raw), NotoFontFallbackService.ResolveFontUri(raw, FontFallback.NotoFontsBaseUri));
		}
	}

	[TestMethod]
	public void When_Mirrored_Then_Every_Map_Url_Keeps_Its_Repository_Path()
	{
		var mirror = new Uri("https://cdn.example.com/static/noto/");
		foreach (var raw in _allRawUrls)
		{
			var resolved = NotoFontFallbackService.ResolveFontUri(raw, mirror);
			Assert.AreEqual(mirror.AbsoluteUri + raw.Substring("https://raw.githubusercontent.com/".Length), resolved.AbsoluteUri);
		}
	}

	[TestMethod]
	public void When_Base_Is_App_Package_Then_Cjk_And_Script_Fonts_Resolve_Under_It()
	{
		FontFallback.NotoFontsBaseUri = new Uri("ms-appx:///Assets/Noto");   // no trailing slash
		Assert.AreEqual(new Uri("ms-appx:///Assets/Noto/"), FontFallback.NotoFontsBaseUri);

		var cjk = FallbackFontMaps.FontWeightsToRawUrls["Japanese"]["Regular"];
		Assert.AreEqual(
			"ms-appx:///Assets/Noto/notofonts/noto-cjk/main/Sans/OTF/Japanese/NotoSansCJKjp-Regular.otf",
			NotoFontFallbackService.ResolveFontUri(cjk, FontFallback.NotoFontsBaseUri).AbsoluteUri);

		var arabic = FallbackFontMaps.FontWeightsToRawUrls["Noto Sans Arabic"]["Regular"];
		var resolved = NotoFontFallbackService.ResolveFontUri(arabic, FontFallback.NotoFontsBaseUri);
		Assert.AreEqual("ms-appx", resolved.Scheme);
		StringAssert.StartsWith(resolved.AbsoluteUri, "ms-appx:///Assets/Noto/notofonts/notofonts.github.io/");
		StringAssert.EndsWith(resolved.AbsoluteUri, "NotoSansArabic-Regular.ttf");
	}

	[TestMethod]
	public void When_Base_Is_Not_Absolute_Then_It_Is_Refused()
	{
		Assert.ThrowsExactly<ArgumentException>(() => FontFallback.NotoFontsBaseUri = new Uri("fonts/noto/", UriKind.Relative));
		Assert.ThrowsExactly<ArgumentNullException>(() => FontFallback.NotoFontsBaseUri = null!);
		Assert.AreEqual(NotoFontFallbackService.DefaultBaseUri, FontFallback.NotoFontsBaseUri);   // unchanged
	}

	[TestMethod]
	public void When_Url_Is_Not_Under_The_Default_Origin_Then_It_Is_Not_Rewritten()
	{
		var other = "https://fonts.example.org/NotoSans-Regular.ttf";
		Assert.AreEqual(new Uri(other), NotoFontFallbackService.ResolveFontUri(other, new Uri("https://cdn.example.com/")));
	}

	[TestMethod]
	public async Task When_Mirrored_On_Disk_Then_The_Font_Is_Read_From_The_Mirror()
	{
		// The fetch the browser runs, pointed at a local mirror laid out like the notofonts repositories.
		var root = Directory.CreateTempSubdirectory("noto-mirror-");
		try
		{
			var raw = FallbackFontMaps.FontWeightsToRawUrls["Noto Sans Arabic"]["Regular"];
			var relative = raw.Substring("https://raw.githubusercontent.com/".Length);
			var path = Path.Combine(root.FullName, relative.Replace('/', Path.DirectorySeparatorChar));
			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			byte[] bytes = [0, 1, 0, 0, 42];
			File.WriteAllBytes(path, bytes);

			FontFallback.NotoFontsBaseUri = new Uri(root.FullName + Path.DirectorySeparatorChar);
			using var stream = await NotoFontFallbackService.FetchNotoFontStream(
				"Noto Sans Arabic", new FontWeight(400), FontStretch.Normal, FontStyle.Normal, CancellationToken.None);

			Assert.IsNotNull(stream);
			using var read = new MemoryStream();
			await stream.CopyToAsync(read);
			CollectionAssert.AreEqual(bytes, read.ToArray());
		}
		finally
		{
			root.Delete(recursive: true);
		}
	}
}
