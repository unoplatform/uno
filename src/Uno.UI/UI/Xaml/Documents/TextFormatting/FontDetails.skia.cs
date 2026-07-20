#nullable enable

using System;
using System.Runtime.InteropServices;
using HarfBuzzSharp;
using SkiaSharp;
using Uno.Extensions;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

internal record FontDetails(SKFont SKFont, float SKFontSize, float SKFontScaleX, SKFontMetrics SKFontMetrics, Font Font)
{
	private (float textScaleX, float textScaleY)? _textScale;
	private IFont? _fontHandle;

	// Opt-in switch to render text through the SkiaSharp-free managed font backend (ManagedFont) instead of
	// SkiaFont. Set UNO_MANAGED_FONT_BACKEND=1 before launching to exercise the alternative drawing backend.
	private static readonly bool _useManagedFontBackend =
		Environment.GetEnvironmentVariable("UNO_MANAGED_FONT_BACKEND") is "1" or "true";

	/// <summary>The backend render-time font handle (outline glyphs -> geometry, color glyphs -> images).</summary>
	internal IFont FontHandle => _fontHandle ??= CreateFontHandle();

	private IFont CreateFontHandle() =>
		_useManagedFontBackend && TryCreateManagedFont() is { } managed ? managed : new SkiaFont(SKFont);

	private IFont? TryCreateManagedFont()
	{
		var typeface = SKFont.Typeface;
		if (typeface is null)
		{
			return null;
		}

		using var stream = typeface.OpenStream(out var ttcIndex);
		if (stream is null)
		{
			return null;
		}

		var bytes = new byte[stream.Length];
		return stream.Read(bytes, bytes.Length) == bytes.Length && ManagedFont.TryCreate(bytes, ttcIndex, SKFont.Size, out var managed)
			? managed
			: null;
	}
	// TODO: Investigate best value to use here. SKShaper uses a constant 512 scale, Avalonia uses default font scale. Not 100% sure how much difference it
	// makes here but it affects subpixel rendering accuracy. Performance does not seem to be affected by changing this value.
	private const int FontScale = 512;

	internal float LineHeight => SKFontMetrics.Descent - SKFontMetrics.Ascent;

	internal SKFont SKFont { get; } = SKFont;
	internal float SKFontScaleX { get; } = SKFontScaleX;
	internal SKFontMetrics SKFontMetrics { get; } = SKFontMetrics;

	internal (float textScaleX, float textScaleY) TextScale
	{
		get
		{
			if (_textScale is null)
			{
				Font.GetScale(out var fontScaleX, out var fontScaleY);
				var textSizeY = SKFontSize / fontScaleY;
				var textSizeX = SKFontSize * SKFontScaleX / fontScaleX;
				_textScale = (textSizeX, textSizeY);
			}
			return _textScale.Value;
		}
	}

	internal Font Font { get; } = Font;

	internal static Blob? GetTable(Tag tag, SKTypeface skTypeFace)
	{
		var size = skTypeFace.GetTableSize(tag);

		if (size == 0)
		{
			return null;
		}

		var data = Marshal.AllocHGlobal(size);

		var releaseDelegate = new ReleaseDelegate(() => Marshal.FreeHGlobal(data));

		var value = skTypeFace.TryGetTableData(tag, 0, size, data) ?
			new Blob(data, size, MemoryMode.Writeable, releaseDelegate) : null;

		return value;
	}

	internal static FontDetails Create(SKTypeface skTypeFace, float fontSize) => _createMemorized(skTypeFace, fontSize);

	private static readonly Func<SKTypeface, float, FontDetails> _createMemorized = ((Func<SKTypeface, float, FontDetails>)CreateInternal).AsMemoized();
	private static FontDetails CreateInternal(SKTypeface skTypeFace, float fontSize)
	{
		var skFont = CreateSKFont(skTypeFace, fontSize);
		var hbFont = CreateHarfBuzzFont(skTypeFace);

		return new(skFont, skFont.Size, skFont.ScaleX, skFont.Metrics, hbFont);
	}

	private static SKFont CreateSKFont(SKTypeface skTypeFace, float fontSize)
	{
		var skFont = new SKFont(skTypeFace, fontSize);
		skFont.Edging = SKFontEdging.SubpixelAntialias;
		skFont.Subpixel = true;
		return skFont;
	}

	private static Font CreateHarfBuzzFont(SKTypeface skTypeFace)
	{
		var hbFace = new Face((_, tag) => GetTable(tag, skTypeFace));
		hbFace.UnitsPerEm = skTypeFace.UnitsPerEm;

		var hbFont = new Font(hbFace);
		hbFont.SetScale(FontScale, FontScale);
		hbFont.SetFunctionsOpenType();

		return hbFont;
	}
}
