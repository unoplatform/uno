#nullable enable

using System;
using System.Runtime.InteropServices;
using HarfBuzzSharp;
using SkiaSharp;
using Uno.Extensions;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

// The text layer talks to the neutral <see cref="IFont"/> handle (metrics/coverage/outlines/tables) and the
// HarfBuzz <see cref="Font"/> (shaping); it never touches a Skia font type. <see cref="Typeface"/> is the interim
// resolution handle (family/style → face) used only by the fallback path in Run — it stays SkiaSharp until the
// font-manager seam replaces it.
internal record FontDetails(IFont FontHandle, SKTypeface Typeface, float FontSize, float FontScaleX, Font Font)
{
	private (float textScaleX, float textScaleY)? _textScale;

	// Opt-in switch to render text through the SkiaSharp-free managed font backend (ManagedFont) instead of
	// SkiaFont. Set UNO_MANAGED_FONT_BACKEND=1 before launching to exercise the alternative drawing backend.
	private static readonly bool _useManagedFontBackend =
		Environment.GetEnvironmentVariable("UNO_MANAGED_FONT_BACKEND") is "1" or "true";

	// TODO: Investigate best value to use here. SKShaper uses a constant 512 scale, Avalonia uses default font scale. Not 100% sure how much difference it
	// makes here but it affects subpixel rendering accuracy. Performance does not seem to be affected by changing this value.
	private const int FontScale = 512;

	internal float LineHeight => FontHandle.Descent - FontHandle.Ascent;

	internal (float textScaleX, float textScaleY) TextScale
	{
		get
		{
			if (_textScale is null)
			{
				Font.GetScale(out var fontScaleX, out var fontScaleY);
				var textSizeY = FontSize / fontScaleY;
				var textSizeX = FontSize * FontScaleX / fontScaleX;
				_textScale = (textSizeX, textSizeY);
			}
			return _textScale.Value;
		}
	}

	internal Font Font { get; } = Font;

	// Serves an sfnt table to HarfBuzz from the neutral font handle (SkiaFont from its variable-instanced typeface,
	// ManagedFont from its own bytes) — so shaping no longer depends on a Skia typeface.
	internal static Blob? GetTable(Tag tag, IFont font)
	{
		var bytes = font.GetFontTable((uint)tag);
		if (bytes is not { Length: > 0 })
		{
			return null;
		}

		var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
		return new Blob(handle.AddrOfPinnedObject(), bytes.Length, MemoryMode.ReadOnly, handle.Free);
	}

	internal static FontDetails Create(SKTypeface skTypeFace, float fontSize) => _createMemorized(skTypeFace, fontSize);

	private static readonly Func<SKTypeface, float, FontDetails> _createMemorized = ((Func<SKTypeface, float, FontDetails>)CreateInternal).AsMemoized();
	private static FontDetails CreateInternal(SKTypeface skTypeFace, float fontSize)
	{
		var skFont = CreateSKFont(skTypeFace, fontSize);
		var fontHandle = CreateFontHandle(skFont);
		var hbFont = CreateHarfBuzzFont(fontHandle);

		return new(fontHandle, skTypeFace, skFont.Size, skFont.ScaleX, hbFont);
	}

	private static IFont CreateFontHandle(SKFont skFont) =>
		_useManagedFontBackend && TryCreateManagedFont(skFont) is { } managed ? managed : new SkiaFont(skFont);

	private static IFont? TryCreateManagedFont(SKFont skFont)
	{
		var typeface = skFont.Typeface;
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
		return stream.Read(bytes, bytes.Length) == bytes.Length && ManagedFont.TryCreate(bytes, ttcIndex, skFont.Size, out var managed)
			? managed
			: null;
	}

	private static SKFont CreateSKFont(SKTypeface skTypeFace, float fontSize)
	{
		var skFont = new SKFont(skTypeFace, fontSize);
		skFont.Edging = SKFontEdging.SubpixelAntialias;
		skFont.Subpixel = true;
		return skFont;
	}

	private static Font CreateHarfBuzzFont(IFont font)
	{
		var hbFace = new Face((_, tag) => GetTable(tag, font));
		hbFace.UnitsPerEm = font.UnitsPerEm;

		var hbFont = new Font(hbFace);
		hbFont.SetScale(FontScale, FontScale);
		hbFont.SetFunctionsOpenType();

		return hbFont;
	}
}
