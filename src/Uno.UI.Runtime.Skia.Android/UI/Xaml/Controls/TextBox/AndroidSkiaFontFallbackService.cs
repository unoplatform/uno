using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SkiaSharp;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Windows.UI.Text;

namespace Uno.WinUI.Runtime.Skia.Android.UI.Xaml.Controls.TextBox;

internal class AndroidSkiaFontFallbackService : IFontFallbackService
{
	private readonly List<(string fontName, SKTypeface typeface)> _fonts;

	public AndroidSkiaFontFallbackService Instance { get; } = new AndroidSkiaFontFallbackService();
	private AndroidSkiaFontFallbackService()
	{
		_fonts = Directory.EnumerateFiles("/system/fonts")
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
		_ = Task.Run(() =>
		{
			tcs.SetResult(_fonts.FirstOrDefault(f => f.fontName.Equals(fontName)).typeface);
		}).ConfigureAwait(false);
		return tcs.Task;
	}
}
