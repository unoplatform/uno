#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HarfBuzzSharp;
using Windows.UI.Xaml.Controls;
using SkiaSharp;

namespace Windows.UI.Xaml.Documents.TextFormatting;

internal record FontDetails(SKFont SKFont, float SKFontSize, float SKFontScaleX, SKFontMetrics SKFontMetrics, Font Font, bool CanChange)
{
	// TODO: Investigate best value to use here. SKShaper uses a constant 512 scale, Avalonia uses default font scale. Not 100% sure how much difference it
	// makes here but it affects subpixel rendering accuracy. Performance does not seem to be affected by changing this value.
	private const int FontScale = 512;

	internal float LineHeight
	{
		get
		{
			var metrics = SKFontMetrics;
			return metrics.Descent - metrics.Ascent;
		}
	}

	internal SKFont SKFont { get; private set; } = SKFont;
	internal float SKFontScaleX { get; private set; } = SKFontScaleX;
	internal SKFontMetrics SKFontMetrics { get; private set; } = SKFontMetrics;
	internal Font Font { get; private set; } = Font;
	internal bool CanChange { get; private set; } = CanChange;

	private List<DependencyObject>? _waitingList;

	internal void RegisterElementForFontLoaded(DependencyObject dependencyObject)
		=> (_waitingList ??= new List<DependencyObject>()).Add(dependencyObject);

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

	internal void Update(SKTypeface skTypeFace)
	{
		SKFont = CreateSKFont(skTypeFace, SKFontSize);
		SKFontScaleX = SKFont.ScaleX;
		SKFontMetrics = SKFont.Metrics;
		Font = CreateHarfBuzzFont(skTypeFace);

		if (_waitingList is not null)
		{
			foreach (var element in _waitingList)
			{
				if (element is TextElement textElement)
				{
					textElement.OnFontLoaded();
				}
				else if (element is TextBlock textBlock)
				{
					textBlock.OnFontLoaded();
				}
				else
				{
					throw new InvalidOperationException($"Unknown element type '{element}' in waiting list");
				}
			}
		}

		_waitingList = null;
		CanChange = false;
	}

	internal void LoadFailed()
	{
		_waitingList = null;
		CanChange = false;
	}

	internal static FontDetails Create(SKTypeface skTypeFace, float fontSize, bool canChange)
	{
		var skFont = CreateSKFont(skTypeFace, fontSize);
		var hbFont = CreateHarfBuzzFont(skTypeFace);

		return new(skFont, skFont.Size, skFont.ScaleX, skFont.Metrics, hbFont, canChange);
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
