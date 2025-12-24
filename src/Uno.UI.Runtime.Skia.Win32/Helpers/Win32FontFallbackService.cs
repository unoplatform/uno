using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Text;
using SkiaSharp;

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

internal class Win32FontFallbackService : IFontFallbackService
{
	private const string FontsDir = @"C:\Users\RamezRagaa\Downloads\fonts";
	private readonly List<(string fontName, SKTypeface typeface)> _fonts;

	public static Win32FontFallbackService Instance { get; } = new Win32FontFallbackService();

	private Win32FontFallbackService()
	{
		_fonts = Directory.EnumerateFiles(FontsDir)
			.Select(f => (Path.GetFileName(f), SKTypeface.FromStream(new MemoryStream(File.ReadAllBytes(f)))))
			.ToList();
	}

	public async Task<string?> GetFontNameForCodePoint(int codepoint)
	{
		await Task.CompletedTask;
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
