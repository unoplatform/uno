using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using HarfBuzzSharp;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Windows.UI.Text;


namespace Uno.WinUI.Runtime.Skia.Android.UI.Xaml.Controls.TextBox;

// Font fallback (codepoint -> system font) via HarfBuzz glyph-coverage lookup — no Skia. HarfBuzzSharp is the
// shaping library the neutral text stack already uses; only SkiaSharp is being removed from the hosts.
internal class AndroidSkiaFontFallbackService : IFontFallbackService
{
	private readonly Task<List<(string fontName, string filePath, Font? font)>> _fonts;

	public static AndroidSkiaFontFallbackService Instance { get; } = new AndroidSkiaFontFallbackService();
	private AndroidSkiaFontFallbackService()
	{
		_fonts = Task.Factory.StartNew(() =>
		{
			return Directory.EnumerateFiles("/system/fonts")
				.Select(f => (Path.GetFileName(f), f, TryLoadFont(f)))
				.ToList();
		}, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
	}

	private static Font? TryLoadFont(string filePath)
	{
		try
		{
			var bytes = File.ReadAllBytes(filePath);
			var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			var blob = new Blob(handle.AddrOfPinnedObject(), bytes.Length, MemoryMode.ReadOnly, handle.Free);
			var face = new Face(blob, 0);
			return new Font(face);
		}
		catch
		{
			return null; // non-font file or unreadable — skip in coverage lookups
		}
	}

	public async Task<string?> GetFontFamilyForCodepoint(int codepoint)
	{
		foreach (var (fontName, _, font) in await _fonts)
		{
			if (font is not null && font.TryGetGlyph(codepoint, out var glyph) && glyph != 0)
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
