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

	internal static void RenderPicture(SKCanvas canvas, IntPtr picture, SKColor background, Action<SKCanvas>? postRenderAction)
	{
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

			postRenderAction?.Invoke(canvas);
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
		private static readonly SKPaint _idleBackgroundPaint = new() { Color = new SKColor(0x1A, 0x23, 0x3B, 0xCC), IsAntialias = true };
		private static readonly SKPaint _textPaint = new() { Color = SKColors.White, IsAntialias = true };
		private static readonly SKPaint _fpsIconPaint = CreateStrokePaint(new SKColor(0x4C, 0xAF, 0x50));
		private static readonly SKPaint _fpsIconFillPaint = CreateFillPaint(new SKColor(0x4C, 0xAF, 0x50));
		private static readonly SKPaint _droppedIconPaint = CreateStrokePaint(new SKColor(0xF4, 0x43, 0x36));
		private static readonly SKPaint _unpresentedIconPaint = CreateStrokePaint(new SKColor(0xFF, 0xC1, 0x07), SKPathEffect.CreateDash(new[] { 2f, 1.5f }, 0));
		private static readonly SKPaint _frameTimeIconPaint = CreateStrokePaint(new SKColor(0x00, 0xBC, 0xD4));
		private static readonly SKPaint _frameTimeIconFillPaint = CreateFillPaint(new SKColor(0x00, 0xBC, 0xD4));
		private static readonly SKPaint _clockIconPaint = CreateStrokePaint(new SKColor(0x21, 0x96, 0xF3));
		private static readonly SKFont _font = new() { Size = 14, Embolden = true };

		// Minimum per-column widths so the panel doesn't shrink when FPS drops from e.g. 120.0 to 15.0.
		// Sized to fit a three-digit reference value — measured once at type load.
		private static readonly float _minColumn1Width = MeasureWidth("120.0");
		private static readonly float _minColumn2Width = MeasureWidth("120.0 ms");

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
		// TimeSpan ticks (100ns units); accessed across threads via Interlocked to avoid torn reads on 32-bit.
		private readonly long[] _drawToPresentTimeTicks;
		private readonly Timer _fpsTimer;
		private int _frameTimesHead;
		private int _drawToPresentTimesHead;
		private int _framesRenderedInLastSecond;
		private int _droppedThisSecond;
		private int _unpresentedThisSecond;
		private long _currentFrameBeginTimestamp;
		private bool _measureThisFrame;
		private long _pictureReadyTimestamp;
		// Generation counter incremented by OnFrameRecorded (UI thread).
		// OnFramePresentRequested (native render thread) reads it and remembers the last-presented value.
		// Mismatches give us dropped-vs-unpresented accounting without relying on _lastRenderedFrame,
		// which is always re-populated by CompositionTarget.ReturnFrame after each Draw.
		private long _currentFrameGeneration;
		private long _lastPresentedGeneration;
		private long _lastTimerTickGeneration;
		private int _consecutiveIdleTicks;
		private bool _isIdle;
		private bool _timerRunning;
		// Set when TimerTick triggers the final "show Idle" redraw, consumed by the next
		// BeginFrame so that one render doesn't restart the 1 Hz timer and re-enter the
		// active state we just left.
		private bool _idleRedrawPending;

		public FpsHelper(int numberOfFramesToCalculateFrameTime = 10)
		{
			_frameTimes = new TimeSpan[numberOfFramesToCalculateFrameTime];
			_drawToPresentTimeTicks = new long[numberOfFramesToCalculateFrameTime];
			_fpsTimer = new Timer(static state => (state as FpsHelper)?.TimerTick(), this, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
		}

		public double Fps { get; private set; }
		public double FrameTime { get; private set; }
		public int DroppedFrames { get; private set; }
		public int UnpresentedFrames { get; private set; }
		public double DrawToPresentDelayMs { get; private set; }

		public float? Scale { private get; set; }

		internal Action? RequestRedraw;

		// All hooks early-return when the counter is disabled so the rendering pipeline
		// pays only a single property read per call site. Null-safe against headless/
		// test/early-init scenarios where Application.Current or DebugSettings may not
		// yet be available.
		private static bool IsEnabled => Application.Current?.DebugSettings?.EnableFrameRateCounter ?? false;

		public FrameDisposable BeginFrame()
		{
			_measureThisFrame = IsEnabled;
			if (_measureThisFrame)
			{
				if (_idleRedrawPending)
				{
					// This render is the final "show Idle" pass we asked for from TimerTick.
					// Restarting the 1 Hz timer here would immediately observe the just-bumped
					// frame generation and flip _isIdle back to false, defeating idle detection.
					_idleRedrawPending = false;
				}
				else if (!_timerRunning)
				{
					_fpsTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));
					_timerRunning = true;
				}
				_currentFrameBeginTimestamp = Stopwatch.GetTimestamp();
			}
			else if (_timerRunning)
			{
				StopTimer();
			}
			return new FrameDisposable(this);
		}

		private void EndFrame()
		{
			if (!_measureThisFrame)
			{
				return;
			}

			_frameTimes[_frameTimesHead] = Stopwatch.GetElapsedTime(_currentFrameBeginTimestamp);
			_frameTimesHead = (_frameTimesHead + 1) % _frameTimes.Length;
			var acc = TimeSpan.Zero;
			foreach (var t in _frameTimes)
			{
				acc += t;
			}
			FrameTime = acc.TotalMilliseconds / _frameTimes.Length;

			Interlocked.Increment(ref _framesRenderedInLastSecond);
		}

		/// <summary>
		/// Called from CompositionTarget.Render after a freshly-recorded SKPicture has been
		/// swapped into _lastRenderedFrame. Stamps the moment the picture became ready. If the
		/// previous generation was never consumed by Draw before this new recording starts,
		/// that previous CPU work is wasted — count it as "drawn-but-not-presented".
		/// </summary>
		public void OnFrameRecorded()
		{
			if (!IsEnabled)
			{
				return;
			}

			var current = Interlocked.Read(ref _currentFrameGeneration);
			var lastPresented = Interlocked.Read(ref _lastPresentedGeneration);
			if (current > lastPresented)
			{
				Interlocked.Increment(ref _unpresentedThisSecond);
			}

			Interlocked.Exchange(ref _pictureReadyTimestamp, Stopwatch.GetTimestamp());
			Interlocked.Increment(ref _currentFrameGeneration);
		}

		/// <summary>
		/// Called from CompositionTarget.Draw at entry. If no new frame has been recorded
		/// since the previous Draw, the native VSync fired but the UI thread didn't produce
		/// anything new — we'll re-blit the same picture. Count that as a dropped frame.
		/// Otherwise, sample the delay from picture-ready to present.
		/// </summary>
		public void OnFramePresentRequested()
		{
			if (!IsEnabled)
			{
				return;
			}

			var current = Interlocked.Read(ref _currentFrameGeneration);
			var lastPresented = Interlocked.Read(ref _lastPresentedGeneration);

			// No frame has ever been recorded yet (counter just enabled / very first VSync).
			// Treating this as a dropped frame would inflate the metric at startup.
			if (current == 0)
			{
				return;
			}

			if (current == lastPresented)
			{
				Interlocked.Increment(ref _droppedThisSecond);
				return;
			}

			var pictureReady = Interlocked.Read(ref _pictureReadyTimestamp);
			if (pictureReady != 0)
			{
				var elapsedTicks = Stopwatch.GetElapsedTime(pictureReady).Ticks;
				Interlocked.Exchange(ref _drawToPresentTimeTicks[_drawToPresentTimesHead], elapsedTicks);
				_drawToPresentTimesHead = (_drawToPresentTimesHead + 1) % _drawToPresentTimeTicks.Length;
			}

			Interlocked.Exchange(ref _lastPresentedGeneration, current);
		}

		public void DrawFps(SKCanvas canvas)
		{
			if (!IsEnabled)
			{
				return;
			}

			var culture = CultureInfo.InvariantCulture;
			var fpsText = Fps.ToString("F1", culture);
			var droppedText = DroppedFrames.ToString(culture);
			var unpresentedText = UnpresentedFrames.ToString(culture);
			var frameTimeText = FormattableString.Invariant($"{FrameTime:F1} ms");
			var isIdle = _isIdle;
			var delayText = isIdle ? "Idle" : FormattableString.Invariant($"{DrawToPresentDelayMs:F1} ms");

			var col1Width = Math.Max(_minColumn1Width, MaxTextWidth(fpsText, droppedText, unpresentedText));
			var col2Width = Math.Max(_minColumn2Width, MaxTextWidth(frameTimeText, delayText));

			var panelWidth = Padding + IconSize + IconTextGap + col1Width + ColumnGap + IconSize + IconTextGap + col2Width + Padding;
			var panelHeight = Padding + 3 * RowHeight + Padding;

			var scale = Scale;
			if (scale is { } scaleValue)
			{
				canvas.Save();
				canvas.Scale(scaleValue, scaleValue);
			}

			canvas.DrawRoundRect(new SKRect(0, 0, panelWidth, panelHeight), BackgroundCornerRadius, BackgroundCornerRadius, isIdle ? _idleBackgroundPaint : _backgroundPaint);

			var col1IconX = Padding;
			var col1TextX = col1IconX + IconSize + IconTextGap;
			var col2IconX = col1TextX + col1Width + ColumnGap;
			var col2TextX = col2IconX + IconSize + IconTextGap;

			DrawCell(canvas, col1IconX, col1TextX, 0, fpsText, DrawSpeedometerIcon);
			DrawCell(canvas, col1IconX, col1TextX, 1, droppedText, DrawDownArrowIcon);
			DrawCell(canvas, col1IconX, col1TextX, 2, unpresentedText, DrawDashedFrameIcon);

			DrawCell(canvas, col2IconX, col2TextX, 0, frameTimeText, DrawFrameTimeIcon);
			DrawCell(canvas, col2IconX, col2TextX, 1, delayText, DrawClockIcon);

			if (scale is not null)
			{
				canvas.Restore();
			}
		}

		private static float MaxTextWidth(params string[] texts)
		{
			float max = 0;
			foreach (var t in texts)
			{
				var width = MeasureWidth(t);
				if (width > max)
				{
					max = width;
				}
			}
			return max;
		}

		private static float MeasureWidth(string text)
		{
			_font.MeasureText(text, out var rect);
			return rect.Width;
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
			if (!IsEnabled)
			{
				StopTimer();
				return;
			}

			Fps = Interlocked.Exchange(ref _framesRenderedInLastSecond, 0);

			DroppedFrames = Interlocked.Exchange(ref _droppedThisSecond, 0);
			UnpresentedFrames = Interlocked.Exchange(ref _unpresentedThisSecond, 0);

			long accTicks = 0;
			for (var i = 0; i < _drawToPresentTimeTicks.Length; i++)
			{
				accTicks += Interlocked.Read(ref _drawToPresentTimeTicks[i]);
			}
			DrawToPresentDelayMs = TimeSpan.FromTicks(accTicks).TotalMilliseconds / _drawToPresentTimeTicks.Length;

			var currentGen = Interlocked.Read(ref _currentFrameGeneration);
			var noNewFrames = currentGen == _lastTimerTickGeneration;
			_lastTimerTickGeneration = currentGen;

			if (noNewFrames)
			{
				_consecutiveIdleTicks++;
				if (_consecutiveIdleTicks >= 2)
				{
					_isIdle = true;
					_idleRedrawPending = true;
					_fpsTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
					_timerRunning = false;
					RequestRedraw?.Invoke();
					return;
				}
			}
			else
			{
				_consecutiveIdleTicks = 0;
				_isIdle = false;
			}
		}

		private void StopTimer()
		{
			_fpsTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
			_timerRunning = false;

			Interlocked.Exchange(ref _framesRenderedInLastSecond, 0);
			Interlocked.Exchange(ref _droppedThisSecond, 0);
			Interlocked.Exchange(ref _unpresentedThisSecond, 0);
			for (var i = 0; i < _drawToPresentTimeTicks.Length; i++)
			{
				Interlocked.Exchange(ref _drawToPresentTimeTicks[i], 0);
			}
			Array.Clear(_frameTimes);
			_frameTimesHead = 0;
			_drawToPresentTimesHead = 0;
			_lastTimerTickGeneration = Interlocked.Read(ref _currentFrameGeneration);
			_consecutiveIdleTicks = 0;
			_isIdle = false;
			_idleRedrawPending = false;
			Fps = 0;
			FrameTime = 0;
			DroppedFrames = 0;
			UnpresentedFrames = 0;
			DrawToPresentDelayMs = 0;
		}

		public void Dispose() => _fpsTimer.Dispose();
	}
}
