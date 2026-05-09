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
	private readonly Task<List<(string fontName, string filePath, SKTypeface typeface)>> _fonts;

	public static AndroidSkiaFontFallbackService Instance { get; } = new AndroidSkiaFontFallbackService();
	private AndroidSkiaFontFallbackService()
	{
		_fonts = Task.Factory.StartNew(() =>
		{
			return Directory.EnumerateFiles("/system/fonts")
				.Select(f => (Path.GetFileName(f), f, SKTypeface.FromFile(f)))
				.ToList();
		}, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
	}

	public async Task<string?> GetFontFamilyForCodepoint(int codepoint)
	{
		foreach (var (fontName, _, typeface) in await _fonts)
		{
			if (typeface.ContainsGlyph(codepoint))
			{
				return fontName;
			}
		}
		return null;
	}

	public async Task<Stream?> GetFontStreamForFontFamily(string fontFamily, FontWeight weight, FontStretch stretch, FontStyle style)
	{
		var match = (await _fonts).FirstOrDefault(f => f.fontName.Equals(fontFamily));
		return match.filePath is null ? null : File.OpenRead(match.filePath);
	}
}
