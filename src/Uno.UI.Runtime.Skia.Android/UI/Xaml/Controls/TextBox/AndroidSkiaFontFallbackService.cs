using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Windows.UI.Text;

namespace Uno.WinUI.Runtime.Skia.Android.UI.Xaml.Controls.TextBox;

internal class AndroidSkiaFontFallbackService : IFontFallbackService
{
	private readonly Task<List<(string fontName, SKTypeface typeface)>> _fonts;

	public static AndroidSkiaFontFallbackService Instance { get; } = new AndroidSkiaFontFallbackService();
	private AndroidSkiaFontFallbackService()
	{
		_fonts = Task.Factory.StartNew(() =>
		{
			return Directory.EnumerateFiles("/system/fonts")
				.Select(f => (Path.GetFileName(f), SKTypeface.FromStream(new MemoryStream(File.ReadAllBytes(f)))))
				.ToList();
		}, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
	}

	public async Task<string?> GetFontNameForCodepoint(int codepoint)
	{
		foreach (var (fontName, typeface) in await _fonts)
		{
			if (typeface.ContainsGlyph(codepoint))
			{
				return fontName;
			}
		}
		return null;
	}

	public async Task<SKTypeface?> GetTypefaceForFontName(string fontName, FontWeight weight, FontStretch stretch, FontStyle style)
		=> (await _fonts).FirstOrDefault(f => f.fontName.Equals(fontName)).typeface;
}
