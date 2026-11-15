#nullable enable

using SkiaSharp;

namespace UITests.Shared.Windows_UI_Composition.GitHubContributions;

internal readonly record struct ColorScheme(SKColor Level0, SKColor Level1, SKColor Level2, SKColor Level3, SKColor Level4)
{
	internal SKColor ForLevel(int level) => level switch
	{
		1 => Level1,
		2 => Level2,
		3 => Level3,
		4 => Level4,
		_ => Level0,
	};
}

internal static class ColorSchemes
{
	private static readonly SKColor Empty = new(235, 237, 240);

	internal static ColorScheme GitHub { get; } = new(
		Empty,
		new SKColor(155, 233, 168),
		new SKColor(64, 196, 99),
		new SKColor(48, 161, 78),
		new SKColor(33, 110, 57));

	internal static ColorScheme Blue { get; } = new(
		Empty,
		new SKColor(174, 214, 241),
		new SKColor(93, 173, 226),
		new SKColor(52, 144, 220),
		new SKColor(21, 101, 192));

	internal static ColorScheme Purple { get; } = new(
		Empty,
		new SKColor(218, 191, 236),
		new SKColor(187, 143, 206),
		new SKColor(156, 95, 176),
		new SKColor(125, 47, 146));
}
