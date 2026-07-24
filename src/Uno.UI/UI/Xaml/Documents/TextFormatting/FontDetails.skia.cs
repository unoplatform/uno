#nullable enable

using System;
using System.Runtime.InteropServices;
using HarfBuzzSharp;
using Uno.Extensions;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

// The text layer talks only to the neutral <see cref="IFont"/> handle (metrics/coverage/outlines/tables) and the
// HarfBuzz <see cref="Font"/> (shaping); font resolution (family/style → handle) is owned by the backend's
// <see cref="IFontManager"/>, so nothing here touches a Skia font type.
internal record FontDetails(IFont FontHandle, float FontSize, float FontScaleX, Font Font)
{
	private (float textScaleX, float textScaleY)? _textScale;

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

	internal static FontDetails Create(IFont fontHandle, float fontSize) => _createMemorized(fontHandle, fontSize);

	private static readonly Func<IFont, float, FontDetails> _createMemorized = ((Func<IFont, float, FontDetails>)CreateInternal).AsMemoized();
	private static FontDetails CreateInternal(IFont fontHandle, float fontSize)
	{
		var hbFont = CreateHarfBuzzFont(fontHandle);
		return new(fontHandle, fontSize, 1.0f, hbFont);
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
