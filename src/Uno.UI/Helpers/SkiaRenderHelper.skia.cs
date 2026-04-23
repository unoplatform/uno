#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SkiaSharp;
using Uno.UI.Xaml.Core;
using static Uno.UI.Helpers.SkiaRenderHelper;

namespace Uno.UI.Helpers;

internal static class SkiaRenderHelper
{
	private static readonly SKPictureRecorder _recorder = new();

	private static readonly List<Visual> _emptyList = new();

	// This is used all the time, on all platforms but X11, when no native elements are present - DO NOT MODIFY
	private static readonly SKPath _emptyClipPath = new();

	// This is used on X11, when no native elements are present - DO NOT MODIFY
	private static float _invertedClipPathWidth;
	private static float _invertedClipPathHeight;
	private static SKPath? _invertedClipPath;

	internal static bool CanRecordPicture([NotNullWhen(true)] UIElement? rootElement) =>
		rootElement is { IsArrangeDirtyOrArrangeDirtyPath: false, IsMeasureDirtyOrMeasureDirtyPath: false };

	internal static (IntPtr picture, SKPath nativeClipPath, List<Visual> nativeVisualsInZOrder) RecordPictureAndReturnPath(float width, float height, ContainerVisual rootVisual, bool invertPath)
	{
		var canvas = _recorder.BeginRecording(Visual.InfiniteClipRect);
		using var _ = new SKAutoCanvasRestore(canvas, true);
		canvas.Clear(SKColors.Transparent);

		rootVisual.Compositor.RenderRootVisual(canvas, rootVisual);

		var (path, nativeVisualsInZOrder) = !ContentPresenter.HasNativeElements() ?
			(!invertPath ? _emptyClipPath : GetOrUpdateInvertedClippingPath(width, height), _emptyList) :
			CalculateClippingPath(width, height, rootVisual, invertPath);

		var picture = UnoSkiaApi.sk_picture_recorder_end_recording(_recorder.Handle);

		return (picture, path, nativeVisualsInZOrder);
	}

	internal static void RenderPicture(SKCanvas canvas, IntPtr picture, SKColor background, FpsHelper fpsHelper)
	{
		using var fpsHelperDisposable = fpsHelper.BeginFrame();
		using (new SKAutoCanvasRestore(canvas, true))
		{
			canvas.Clear(background);
			if (picture != IntPtr.Zero)
			{
				// This might happen if we get render request before the first frame is painted
				unsafe
				{
					UnoSkiaApi.sk_canvas_draw_picture(canvas.Handle, picture, null, IntPtr.Zero);
				}
			}

			fpsHelper.DrawFps(canvas);
		}

		// update the control
		canvas.Flush();
	}

	private static readonly SKPath _spareParentClipPath = new();

	/// <summary>
	/// Does a rendering cycle and returns a path that represents the visible area of the native views.
	/// </summary>
	private static (SKPath nativeClipPath, List<Visual> nativeVisualsInZOrder) CalculateClippingPath(float width, float height, ContainerVisual rootVisual, bool invertPath)
	{
		var clipPath = new SKPath();

		var rect = new SKRect(0f, 0f, width, height);

		var parentClipPath = _spareParentClipPath;
		parentClipPath.Rewind();
		parentClipPath.AddRect(rect);

		var nativeVisualsInZOrder = new List<Visual>();
		rootVisual.GetNativeViewPathAndZOrder(parentClipPath, clipPath, nativeVisualsInZOrder);

		if (!invertPath)
		{
			return (clipPath, nativeVisualsInZOrder);
		}
		else
		{
			var invertedPath = new SKPath();
			invertedPath.AddRect(rect);
			invertedPath.Op(clipPath, SKPathOp.Difference, invertedPath);

			clipPath.Dispose();

			return (invertedPath, nativeVisualsInZOrder);
		}
	}

	private static SKPath GetOrUpdateInvertedClippingPath(float width, float height)
	{
		if (_invertedClipPath != null && _invertedClipPathWidth == width && _invertedClipPathHeight == height)
		{
			return _invertedClipPath;
		}
		else
		{
			var result = new SKPath();
			result.AddRect(new SKRect(0f, 0f, width, height));
			result.Op(_emptyClipPath, SKPathOp.Difference, result);

			_invertedClipPathWidth = width;
			_invertedClipPathHeight = height;
			_invertedClipPath = result;

			return result;
		}
	}

	public class FpsHelper
	{
		// Panel geometry at 1x scale, before Scale is applied.
		private const float Padding = 8f;
		private const float IconSize = 14f;
		private const float IconTextGap = 6f;
		private const float RowHeight = 22f;
		private const float ColumnGap = 20f;
		private const float BackgroundCornerRadius = 4f;

		public readonly record struct FrameDisposable(FpsHelper @this) : IDisposable
		{
			public void Dispose() => @this.EndFrame();
		}

		private static readonly SKPaint _backgroundPaint = new() { Color = new SKColor(0, 0, 0, 0xCC), IsAntialias = true };
		private static readonly SKPaint _textPaint = new() { Color = SKColors.White, IsAntialias = true };
		private static readonly SKPaint _iconDotPaint = new() { Color = SKColors.White, IsAntialias = true };
		private static readonly SKPaint _fpsIconPaint = CreateStrokePaint(new SKColor(0x4C, 0xAF, 0x50));
		private static readonly SKPaint _fpsIconFillPaint = CreateFillPaint(new SKColor(0x4C, 0xAF, 0x50));
		private static readonly SKPaint _droppedIconPaint = CreateStrokePaint(new SKColor(0xF4, 0x43, 0x36));
		private static readonly SKPaint _unpresentedIconPaint = CreateStrokePaint(new SKColor(0xFF, 0xC1, 0x07), SKPathEffect.CreateDash(new[] { 2f, 1.5f }, 0));
		private static readonly SKPaint _frameTimeIconPaint = CreateStrokePaint(new SKColor(0x00, 0xBC, 0xD4));
		private static readonly SKPaint _frameTimeIconFillPaint = CreateFillPaint(new SKColor(0x00, 0xBC, 0xD4));
		private static readonly SKPaint _clockIconPaint = CreateStrokePaint(new SKColor(0x21, 0x96, 0xF3));
		private static readonly SKFont _font = new() { Size = 14, Embolden = true };

		private static SKPaint CreateStrokePaint(SKColor color, SKPathEffect? pathEffect = null) => new()
		{
			Color = color,
			IsAntialias = true,
			IsStroke = true,
			StrokeWidth = 1.5f,
			StrokeCap = SKStrokeCap.Round,
			PathEffect = pathEffect,
		};

		private static SKPaint CreateFillPaint(SKColor color) => new()
		{
			Color = color,
			IsAntialias = true,
		};

		private readonly TimeSpan[] _frameTimes;
		private readonly TimeSpan[] _drawToPresentTimes;
		private readonly Timer _fpsTimer;
		private int _frameTimesHead;
		private int _drawToPresentTimesHead;
		private int _framesRenderedInLastSecond;
		private int _droppedThisSecond;
		private int _unpresentedThisSecond;
		private long _currentFrameBeginTimestamp;
		private long _pictureReadyTimestamp;

		public FpsHelper(int numberOfFramesToCalculateFrameTime = 10)
		{
			_frameTimes = new TimeSpan[numberOfFramesToCalculateFrameTime];
			_drawToPresentTimes = new TimeSpan[numberOfFramesToCalculateFrameTime];
			_fpsTimer = new Timer(static state => (state as FpsHelper)?.TimerTick(), this, TimeSpan.Zero, TimeSpan.FromSeconds(1));
		}

		public double Fps { get; private set; }
		public double FrameTime { get; private set; }
		public int DroppedFrames { get; private set; }
		public int UnpresentedFrames { get; private set; }
		public double DrawToPresentDelayMs { get; private set; }

		public float? Scale { private get; set; }

		public FrameDisposable BeginFrame()
		{
			_currentFrameBeginTimestamp = Stopwatch.GetTimestamp();
			return new FrameDisposable(this);
		}

		private void EndFrame()
		{
			_frameTimes[_frameTimesHead] = Stopwatch.GetElapsedTime(_currentFrameBeginTimestamp);
			_frameTimesHead = (_frameTimesHead + 1) % _frameTimes.Length;
			var acc = TimeSpan.Zero;
			foreach (var t in _frameTimes)
			{
				acc += t;
			}
			FrameTime = acc.TotalMilliseconds / _frameTimes.Length;

			_framesRenderedInLastSecond++;
		}

		/// <summary>
		/// Called from CompositionTarget.Render after a freshly-recorded SKPicture has been
		/// swapped into _lastRenderedFrame. Stamps the moment the picture became ready, and
		/// flags a "drawn-but-not-presented" event when a previous picture was discarded
		/// before it was ever blit.
		/// </summary>
		public void OnFrameRecorded(bool replacedPendingFrame)
		{
			Interlocked.Exchange(ref _pictureReadyTimestamp, Stopwatch.GetTimestamp());
			if (replacedPendingFrame)
			{
				Interlocked.Increment(ref _unpresentedThisSecond);
			}
		}

		/// <summary>
		/// Called from CompositionTarget.Draw at entry. When <paramref name="hadFrameToPresent"/>
		/// is false, the native VSync fired but Uno had nothing recorded — a dropped frame.
		/// When true, samples the delay from picture-ready to present for the rolling average.
		/// </summary>
		public void OnFramePresentRequested(bool hadFrameToPresent)
		{
			if (!hadFrameToPresent)
			{
				Interlocked.Increment(ref _droppedThisSecond);
				return;
			}

			var pictureReady = Interlocked.Read(ref _pictureReadyTimestamp);
			if (pictureReady == 0)
			{
				return;
			}

			_drawToPresentTimes[_drawToPresentTimesHead] = Stopwatch.GetElapsedTime(pictureReady);
			_drawToPresentTimesHead = (_drawToPresentTimesHead + 1) % _drawToPresentTimes.Length;
		}

		public void DrawFps(SKCanvas canvas)
		{
			if (!Application.Current.DebugSettings.EnableFrameRateCounter)
			{
				return;
			}

			var culture = CultureInfo.InvariantCulture;
			var fpsText = Fps.ToString("F1", culture);
			var droppedText = DroppedFrames.ToString(culture);
			var unpresentedText = UnpresentedFrames.ToString(culture);
			var frameTimeText = FormattableString.Invariant($"{FrameTime:F1} ms");
			var delayText = FormattableString.Invariant($"{DrawToPresentDelayMs:F1} ms");

			var col1Width = MaxTextWidth(fpsText, droppedText, unpresentedText);
			var col2Width = MaxTextWidth(frameTimeText, delayText);

			var panelWidth = Padding + IconSize + IconTextGap + col1Width + ColumnGap + IconSize + IconTextGap + col2Width + Padding;
			var panelHeight = Padding + 3 * RowHeight + Padding;

			var applyScale = Scale is { } scale;
			if (applyScale)
			{
				canvas.Save();
				canvas.Scale(Scale!.Value, Scale.Value);
			}

			canvas.DrawRoundRect(new SKRect(0, 0, panelWidth, panelHeight), BackgroundCornerRadius, BackgroundCornerRadius, _backgroundPaint);

			var col1IconX = Padding;
			var col1TextX = col1IconX + IconSize + IconTextGap;
			var col2IconX = col1TextX + col1Width + ColumnGap;
			var col2TextX = col2IconX + IconSize + IconTextGap;

			DrawCell(canvas, col1IconX, col1TextX, 0, fpsText, DrawSpeedometerIcon);
			DrawCell(canvas, col1IconX, col1TextX, 1, droppedText, DrawDownArrowIcon);
			DrawCell(canvas, col1IconX, col1TextX, 2, unpresentedText, DrawDashedFrameIcon);

			DrawCell(canvas, col2IconX, col2TextX, 0, frameTimeText, DrawFrameTimeIcon);
			DrawCell(canvas, col2IconX, col2TextX, 1, delayText, DrawClockIcon);

			if (applyScale)
			{
				canvas.Restore();
			}
		}

		private static float MaxTextWidth(params string[] texts)
		{
			float max = 0;
			foreach (var t in texts)
			{
				_font.MeasureText(t, out var rect);
				if (rect.Width > max)
				{
					max = rect.Width;
				}
			}
			return max;
		}

		private static void DrawCell(SKCanvas canvas, float iconX, float textX, int row, string value, Action<SKCanvas, float, float> drawIcon)
		{
			var rowTop = Padding + row * RowHeight;
			var iconY = rowTop + (RowHeight - IconSize) / 2;
			drawIcon(canvas, iconX, iconY);

			_font.MeasureText(value, out var textRect);
			var textY = rowTop + (RowHeight - textRect.Height) / 2 - textRect.Top;
			canvas.DrawText(value, textX, textY, _font, _textPaint);
		}

		private static void DrawSpeedometerIcon(SKCanvas canvas, float x, float y)
		{
			var cx = x + IconSize / 2;
			var cy = y + IconSize / 2;
			var r = IconSize / 2 - 1;
			canvas.DrawCircle(cx, cy, r, _fpsIconPaint);
			// Needle pointing up-right (~45°)
			var needleLen = r * 0.85f;
			canvas.DrawLine(cx, cy, cx + needleLen * 0.707f, cy - needleLen * 0.707f, _fpsIconPaint);
			canvas.DrawCircle(cx, cy, 1.2f, _fpsIconFillPaint);
		}

		private static void DrawDownArrowIcon(SKCanvas canvas, float x, float y)
		{
			var cx = x + IconSize / 2;
			canvas.DrawLine(cx, y + 1, cx, y + IconSize - 2, _droppedIconPaint);
			canvas.DrawLine(cx - 3.5f, y + IconSize - 5, cx, y + IconSize - 1, _droppedIconPaint);
			canvas.DrawLine(cx, y + IconSize - 1, cx + 3.5f, y + IconSize - 5, _droppedIconPaint);
		}

		private static void DrawDashedFrameIcon(SKCanvas canvas, float x, float y)
		{
			// Dashed rectangle outline — "frame that didn't make it to screen".
			canvas.DrawRect(new SKRect(x + 1, y + 1, x + IconSize - 1, y + IconSize - 1), _unpresentedIconPaint);
		}

		private static void DrawFrameTimeIcon(SKCanvas canvas, float x, float y)
		{
			var rect = new SKRect(x + 1, y + 4, x + IconSize - 1, y + IconSize - 4);
			canvas.DrawRect(rect, _frameTimeIconPaint);
			var inner = new SKRect(
				rect.Left + 1.5f,
				rect.Top + 1.5f,
				rect.Left + (rect.Right - rect.Left) * 0.65f,
				rect.Bottom - 1.5f);
			canvas.DrawRect(inner, _frameTimeIconFillPaint);
		}

		private static void DrawClockIcon(SKCanvas canvas, float x, float y)
		{
			var cx = x + IconSize / 2;
			var cy = y + IconSize / 2;
			var r = IconSize / 2 - 1;
			canvas.DrawCircle(cx, cy, r, _clockIconPaint);
			canvas.DrawLine(cx, cy, cx, cy - r * 0.55f, _clockIconPaint);
			canvas.DrawLine(cx, cy, cx + r * 0.75f, cy, _clockIconPaint);
		}

		private void TimerTick()
		{
			Fps = _framesRenderedInLastSecond;
			_framesRenderedInLastSecond = 0;

			DroppedFrames = Interlocked.Exchange(ref _droppedThisSecond, 0);
			UnpresentedFrames = Interlocked.Exchange(ref _unpresentedThisSecond, 0);

			var acc = TimeSpan.Zero;
			foreach (var t in _drawToPresentTimes)
			{
				acc += t;
			}
			DrawToPresentDelayMs = acc.TotalMilliseconds / _drawToPresentTimes.Length;
		}

		public void Dispose() => _fpsTimer.Dispose();
	}
}
