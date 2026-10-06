#nullable enable

using System;
using System.Diagnostics;
using SkiaSharp;
using Uno.WinUI.Graphics2DSK;
using Windows.Foundation;

namespace UITests.Shared.Windows_UI_Composition.GitHubContributions;

internal sealed partial class ContributionsCanvas : SKCanvasElement
{
	private const float Cell = 14f;
	private const float Gap = 3f;
	private const float Radius = 3f;
	private const int Rows = 7;
	private const float LabelWidth = 26f;
	private const float LabelGap = 8f;
	private const float Inset = 16f;
	private const float CardRadius = 16f;

	private static readonly string[] DayLabels = { "Mon", "", "Wed", "", "Fri", "", "" };

	private readonly Stopwatch _clock = Stopwatch.StartNew();
	private readonly Random _rng = new(20261006);

	private int[] _levels = Array.Empty<int>();
	private Spring[] _springs = Array.Empty<Spring>();
	private int _weeks;
	private long _lastTicks;

	internal ContributionsCanvas() => ColorScheme = ColorSchemes.GitHub;

	internal ColorScheme ColorScheme { get; set; }

	internal bool Filled { get; private set; }

	internal int SquareCount => _levels.Length;

	/// <summary>Runs the wave in, or clears it back to empty if it has already run.</summary>
	internal void Toggle()
	{
		Filled = !Filled;

		for (var i = 0; i < _springs.Length; i++)
		{
			var week = i / Rows;
			var day = i % Rows;

			// Filling sweeps bottom-left to top-right on a fixed cadence; clearing scatters, so the
			// grid comes apart rather than un-drawing itself along the same diagonal.
			var delay = Filled
				? 0.045f * (week + (Rows - 1 - day))
				: (float)_rng.NextDouble() * 0.5f;

			_springs[i].To(Filled ? 1f : 0f, delay);
		}
	}

	protected override void RenderOverride(SKCanvas canvas, Size area)
	{
		var now = _clock.ElapsedTicks;
		var dt = _lastTicks == 0 ? 0f : (float)((now - _lastTicks) / (double)Stopwatch.Frequency);
		_lastTicks = now;

		var w = (float)area.Width;
		var h = (float)area.Height;
		canvas.Clear(new SKColor(0xF7, 0xF7, 0xF7));
		if (w <= 0 || h <= 0)
		{
			return;
		}

		var gridWidth = w - 2 * Inset - LabelWidth - LabelGap - 2 * Inset;
		var weeks = Math.Max(1, (int)((gridWidth + Gap) / (Cell + Gap)));
		if (weeks != _weeks)
		{
			Rebuild(weeks);
		}

		var gridW = _weeks * (Cell + Gap) - Gap;
		var gridH = Rows * (Cell + Gap) - Gap;
		var cardW = gridW + LabelWidth + LabelGap + 2 * Inset;
		var cardH = gridH + 2 * Inset;
		var cardX = (w - cardW) * 0.5f;
		var cardY = (h - cardH) * 0.5f;

		using (var shadow = new SKPaint { Color = new SKColor(0, 0, 0, 13), IsAntialias = true })
		{
			shadow.ImageFilter = SKImageFilter.CreateBlur(10f, 10f);
			canvas.DrawRoundRect(new SKRect(cardX, cardY + 2, cardX + cardW, cardY + cardH + 2), CardRadius, CardRadius, shadow);
		}

		using (var card = new SKPaint { Color = SKColors.White, IsAntialias = true })
		{
			canvas.DrawRoundRect(new SKRect(cardX, cardY, cardX + cardW, cardY + cardH), CardRadius, CardRadius, card);
		}

		var originX = cardX + Inset + LabelWidth + LabelGap;
		var originY = cardY + Inset;

		using var font = new SKFont(SKTypeface.Default, 11f);
		using var labelPaint = new SKPaint { Color = new SKColor(0x65, 0x6D, 0x76), IsAntialias = true };
		for (var r = 0; r < Rows; r++)
		{
			if (DayLabels[r].Length == 0)
			{
				continue;
			}

			var y = originY + r * (Cell + Gap) + Cell * 0.5f + 4f;
			canvas.DrawText(DayLabels[r], cardX + Inset, y, SKTextAlign.Left, font, labelPaint);
		}

		using var paint = new SKPaint { IsAntialias = true };
		var empty = ColorScheme.Level0;

		for (var i = 0; i < _springs.Length; i++)
		{
			_springs[i].Advance(dt, Filled ? SpringConfig.Fill : SpringConfig.Clear);

			var p = _springs[i].Value;
			var week = i / Rows;
			var day = i % Rows;

			// Dip to 0.4 and back: the square shrinks away before it returns carrying its colour.
			var scale = p < 0.5f
				? 1f + (0.4f - 1f) * (p / 0.5f)
				: 0.4f + (1f - 0.4f) * ((p - 0.5f) / 0.5f);

			var target = ColorScheme.ForLevel(_levels[i]);
			paint.Color = Lerp(empty, target, Math.Clamp(p, 0f, 1f));

			var cx = originX + week * (Cell + Gap) + Cell * 0.5f;
			var cy = originY + day * (Cell + Gap) + Cell * 0.5f;
			var half = Cell * 0.5f * Math.Max(0.01f, scale);

			canvas.DrawRoundRect(
				new SKRect(cx - half, cy - half, cx + half, cy + half),
				Radius * scale,
				Radius * scale,
				paint);
		}
	}

	private void Rebuild(int weeks)
	{
		_weeks = weeks;
		var count = weeks * Rows;
		_levels = new int[count];
		_springs = new Spring[count];

		var rng = new Random(20261006);
		for (var i = 0; i < count; i++)
		{
			// Weighted low: a real year of contributions is mostly quiet, and an evenly random grid
			// reads as noise rather than as somebody's activity.
			var r = rng.NextDouble();
			_levels[i] = r switch
			{
				< 0.42 => 0,
				< 0.66 => 1,
				< 0.84 => 2,
				< 0.95 => 3,
				_ => 4,
			};

			_springs[i].Value = Filled ? 1f : 0f;
			_springs[i].Target = _springs[i].Value;
			_springs[i].Resting = true;
		}
	}

	private static SKColor Lerp(SKColor a, SKColor b, float t) => new(
		(byte)(a.Red + (b.Red - a.Red) * t),
		(byte)(a.Green + (b.Green - a.Green) * t),
		(byte)(a.Blue + (b.Blue - a.Blue) * t));
}
