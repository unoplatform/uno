#nullable enable
#if __SKIA__
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Text;
using SkiaSharp;

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

internal class FontFallbackService
{
	private const string FontsDir = @"C:\Users\RamezRagaa\Downloads\fonts";
	private readonly List<(string fontName, SKTypeface typeface)> _fonts;

	public static FontFallbackService Instance { get; } = new FontFallbackService();

	private FontFallbackService()
	{
		_fonts = Directory.EnumerateFiles(FontsDir)
			.Select(f => (Path.GetFileName(f), SKTypeface.FromStream(new MemoryStream(File.ReadAllBytes(f)))))
			.ToList();
	}

	public string? GetFontNameForCodePoint(int codepoint)
	{
		foreach (var (fontName, typeface) in _fonts)
		{
			if (typeface.ContainsGlyph(codepoint))
			{
				return fontName;
			}
		}
		return null;
	}

	public Task<SKTypeface?> GetTypefaceForFontName(string fontName, FontWeight weight, FontStretch stretch, FontStyle style)
	{
		var tcs = new TaskCompletionSource<SKTypeface?>();
		_ = Task.Run(async () =>
		{
			await Task.Delay(TimeSpan.FromSeconds(5));
			tcs.SetResult(_fonts.FirstOrDefault(f => f.fontName.Equals(fontName)).typeface);
		}).ConfigureAwait(false);
		return tcs.Task;
	}
}
#endif
