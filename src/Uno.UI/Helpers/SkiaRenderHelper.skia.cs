#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using SkiaSharp;
using Uno.UI.Composition.Drawing;
using Uno.UI.Xaml.Core;
using static Uno.UI.Helpers.SkiaRenderHelper;

namespace Uno.UI.Helpers;

internal static class SkiaRenderHelper
{
	private static readonly List<Visual> _emptyList = new();

	// This is used all the time, on all platforms but X11, when no native elements are present - DO NOT MODIFY
	private static readonly SKPath _emptyClipPath = new();

	// This is used on X11, when no native elements are present - DO NOT MODIFY
	private static float _invertedClipPathWidth;
	private static float _invertedClipPathHeight;
	private static SKPath? _invertedClipPath;

	internal static bool CanRecordPicture([NotNullWhen(true)] UIElement? rootElement) =>
		rootElement is { IsArrangeDirtyOrArrangeDirtyPath: false, IsMeasureDirtyOrMeasureDirtyPath: false };

	/// <summary>
	/// Phase 1 of the render cycle (UI thread): walks the visual tree into <paramref name="session"/> (the
	/// recording session provided by the backend) and computes the native-element clip path. Backend-agnostic;
	/// the caller obtains the frame via <see cref="ICommandRecorder.Finish"/>.
	/// </summary>
	internal static (SKPath nativeClipPath, List<Visual> nativeVisualsInZOrder) RecordFrame(ICommandRecorder session, float width, float height, ContainerVisual rootVisual, bool invertPath)
	{
		session.Clear(global::Windows.UI.Colors.Transparent);

		rootVisual.Compositor.RenderRootVisual(session, rootVisual);

		return !ContentPresenter.HasNativeElements() ?
			(!invertPath ? _emptyClipPath : GetOrUpdateInvertedClippingPath(width, height), _emptyList) :
			CalculateClippingPath(width, height, rootVisual, invertPath);
	}

	/// <summary>
	/// Does a rendering cycle and returns a path that represents the visible area of the native views.
	/// </summary>
	private static (SKPath nativeClipPath, List<Visual> nativeVisualsInZOrder) CalculateClippingPath(float width, float height, ContainerVisual rootVisual, bool invertPath)
	{
		var rect = new SKRect(0f, 0f, width, height);

		var parentClip = DrawingBackend.Current.CreateRectangleGeometry(rect.ToRect());
		var seedClip = DrawingBackend.Current.CreateRectangleGeometry(new global::Windows.Foundation.Rect(0, 0, 0, 0));

		var nativeVisualsInZOrder = new List<Visual>();
		var accumulated = rootVisual.GetNativeViewPathAndZOrder(parentClip, seedClip, nativeVisualsInZOrder);

		// The native-clipping consumers below still operate on SKPath; unwrap the geometry handle here.
		var clipPath = ((SkiaGeometrySource2D)accumulated).Geometry;

		if (!invertPath)
		{
			return (clipPath, nativeVisualsInZOrder);
		}
		else
		{
			var invertedPath = Microsoft.UI.Composition.SkiaExtensions.CreateRectPath(rect);
			invertedPath.Op(clipPath, SKPathOp.Difference, invertedPath);

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
			var result = Microsoft.UI.Composition.SkiaExtensions.CreateRectPath(new SKRect(0f, 0f, width, height));
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

		private const float IconStrokeWidth = 1.5f;
		private static readonly Color _backgroundColor = Color.FromArgb(0xCC, 0, 0, 0);
		private static readonly Color _idleBackgroundColor = Color.FromArgb(0xCC, 0x1A, 0x23, 0x3B);
		private static readonly Color _textColor = Colors.White;
		private static readonly Color _fpsIconColor = Color.FromArgb(0xFF, 0x4C, 0xAF, 0x50);
		private static readonly Color _droppedIconColor = Color.FromArgb(0xFF, 0xF4, 0x43, 0x36);
		private static readonly Color _unpresentedIconColor = Color.FromArgb(0xFF, 0xFF, 0xC1, 0x07);
		private static readonly Color _frameTimeIconColor = Color.FromArgb(0xFF, 0x00, 0xBC, 0xD4);
		private static readonly Color _clockIconColor = Color.FromArgb(0xFF, 0x21, 0x96, 0xF3);
		// Kept for text shaping and measurement only (font work, not rendering).
		private static readonly SKFont _font = new() { Size = 14, Embolden = true };
		private static readonly IFont _fontHandle = new SkiaFont(_font);

		// Minimum per-column widths so the panel doesn't shrink when FPS drops from e.g. 120.0 to 15.0.
		// Sized to fit a three-digit reference value — measured once at type load.
		private static readonly float _minColumn1Width = MeasureWidth("120.0");
		private static readonly float _minColumn2Width = MeasureWidth("120.0 ms");

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

		private double _fps;
		private double _frameTime;
		private int _droppedFrames;
		private int _unpresentedFrames;
		private double _drawToPresentDelayMs;

		public Action? RequestRedraw { private get; set; }

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
			_frameTime = acc.TotalMilliseconds / _frameTimes.Length;

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

		public void DrawFps(IDrawingSession session)
		{
			if (!IsEnabled)
			{
				return;
			}

			var culture = CultureInfo.InvariantCulture;
			var fpsText = _fps.ToString("F1", culture);
			var droppedText = _droppedFrames.ToString(culture);
			var unpresentedText = _unpresentedFrames.ToString(culture);
			var frameTimeText = FormattableString.Invariant($"{_frameTime:F1} ms");
			var isIdle = _isIdle;
			var delayText = isIdle ? "Idle" : FormattableString.Invariant($"{_drawToPresentDelayMs:F1} ms");

			var col1Width = Math.Max(_minColumn1Width, MaxTextWidth(fpsText, droppedText, unpresentedText));
			var col2Width = Math.Max(_minColumn2Width, MaxTextWidth(frameTimeText, delayText));

			var panelWidth = Padding + IconSize + IconTextGap + col1Width + ColumnGap + IconSize + IconTextGap + col2Width + Padding;
			var panelHeight = Padding + 3 * RowHeight + Padding;

			using (var panel = RoundedRectangle(new Rect(0, 0, panelWidth, panelHeight), BackgroundCornerRadius))
			{
				session.DrawPath(panel, isIdle ? _idleBackgroundColor : _backgroundColor, antialias: true);
			}

			var col1IconX = Padding;
			var col1TextX = col1IconX + IconSize + IconTextGap;
			var col2IconX = col1TextX + col1Width + ColumnGap;
			var col2TextX = col2IconX + IconSize + IconTextGap;

			DrawCell(session, col1IconX, col1TextX, 0, fpsText, DrawSpeedometerIcon);
			DrawCell(session, col1IconX, col1TextX, 1, droppedText, DrawDownArrowIcon);
			DrawCell(session, col1IconX, col1TextX, 2, unpresentedText, DrawDashedFrameIcon);

			DrawCell(session, col2IconX, col2TextX, 0, frameTimeText, DrawFrameTimeIcon);
			DrawCell(session, col2IconX, col2TextX, 1, delayText, DrawClockIcon);
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

		private static void DrawCell(IDrawingSession session, float iconX, float textX, int row, string value, Action<IDrawingSession, float, float> drawIcon)
		{
			var rowTop = Padding + row * RowHeight;
			var iconY = rowTop + (RowHeight - IconSize) / 2;
			drawIcon(session, iconX, iconY);

			_font.MeasureText(value, out var textRect);
			var textY = rowTop + (RowHeight - textRect.Height) / 2 - textRect.Top;
			DrawText(session, value, textX, textY, _textColor);
		}

		// Shape the string with the font (font work stays on Skia) and draw the glyph outlines through the
		// neutral path verb — the same way a TextBlock renders.
		private static void DrawText(IDrawingSession session, string text, float x, float baselineY, Color color)
		{
			var glyphs = _font.GetGlyphs(text);
			if (glyphs.Length == 0)
			{
				return;
			}

			var positions = _font.GetGlyphPositions(text, new SKPoint(x, baselineY));
			var positionsV = MemoryMarshal.Cast<SKPoint, Vector2>(positions);
			using var geometry = _fontHandle.BuildGlyphRunOutline(glyphs, positionsV, 0f);
			session.DrawPath(geometry, color, antialias: true);
		}

		private static void DrawSpeedometerIcon(IDrawingSession session, float x, float y)
		{
			var cx = x + IconSize / 2;
			var cy = y + IconSize / 2;
			var r = IconSize / 2 - 1;
			using (var circle = Ellipse(cx, cy, r))
			{
				session.StrokePath(circle, _fpsIconColor, IconStrokeWidth, antialias: true);
			}
			// Needle pointing up-right (~45°)
			var needleLen = r * 0.85f;
			session.DrawLine(new Vector2(cx, cy), new Vector2(cx + needleLen * 0.707f, cy - needleLen * 0.707f), _fpsIconColor, IconStrokeWidth, antialias: true);
			using (var dot = Ellipse(cx, cy, 1.2f))
			{
				session.DrawPath(dot, _fpsIconColor, antialias: true);
			}
		}

		private static void DrawDownArrowIcon(IDrawingSession session, float x, float y)
		{
			var cx = x + IconSize / 2;
			session.DrawLine(new Vector2(cx, y + 1), new Vector2(cx, y + IconSize - 2), _droppedIconColor, IconStrokeWidth, antialias: true);
			session.DrawLine(new Vector2(cx - 3.5f, y + IconSize - 5), new Vector2(cx, y + IconSize - 1), _droppedIconColor, IconStrokeWidth, antialias: true);
			session.DrawLine(new Vector2(cx, y + IconSize - 1), new Vector2(cx + 3.5f, y + IconSize - 5), _droppedIconColor, IconStrokeWidth, antialias: true);
		}

		private static void DrawDashedFrameIcon(IDrawingSession session, float x, float y)
		{
			// "Frame that didn't make it to screen". The neutral layer has no dashed stroke, so this is a solid outline.
			using var frame = Rectangle(new Rect(x + 1, y + 1, IconSize - 2, IconSize - 2));
			session.StrokePath(frame, _unpresentedIconColor, IconStrokeWidth, antialias: true);
		}

		private static void DrawFrameTimeIcon(IDrawingSession session, float x, float y)
		{
			var rect = new Rect(x + 1, y + 4, IconSize - 2, IconSize - 8);
			using (var outline = Rectangle(rect))
			{
				session.StrokePath(outline, _frameTimeIconColor, IconStrokeWidth, antialias: true);
			}
			var inner = new Rect(rect.X + 1.5, rect.Y + 1.5, rect.Width * 0.65 - 1.5, rect.Height - 3);
			session.DrawRect(inner, _frameTimeIconColor, antialias: true);
		}

		private static void DrawClockIcon(IDrawingSession session, float x, float y)
		{
			var cx = x + IconSize / 2;
			var cy = y + IconSize / 2;
			var r = IconSize / 2 - 1;
			using (var circle = Ellipse(cx, cy, r))
			{
				session.StrokePath(circle, _clockIconColor, IconStrokeWidth, antialias: true);
			}
			session.DrawLine(new Vector2(cx, cy), new Vector2(cx, cy - r * 0.55f), _clockIconColor, IconStrokeWidth, antialias: true);
			session.DrawLine(new Vector2(cx, cy), new Vector2(cx + r * 0.75f, cy), _clockIconColor, IconStrokeWidth, antialias: true);
		}

		private static IGeometry Ellipse(float cx, float cy, float r)
		{
			var builder = DrawingBackend.Current.CreatePrimitiveGeometryBuilder();
			builder.AddEllipse(new Vector2(cx, cy), r, r);
			return builder.Build();
		}

		private static IGeometry Rectangle(Rect rect)
		{
			var builder = DrawingBackend.Current.CreatePrimitiveGeometryBuilder();
			builder.AddRectangle(rect);
			return builder.Build();
		}

		private static IGeometry RoundedRectangle(Rect rect, float radius)
		{
			var builder = DrawingBackend.Current.CreatePrimitiveGeometryBuilder();
			builder.AddRoundedRectangle(rect, radius, radius);
			return builder.Build();
		}

		private void TimerTick()
		{
			if (!IsEnabled)
			{
				StopTimer();
				return;
			}

			_fps = Interlocked.Exchange(ref _framesRenderedInLastSecond, 0);

			_droppedFrames = Interlocked.Exchange(ref _droppedThisSecond, 0);
			_unpresentedFrames = Interlocked.Exchange(ref _unpresentedThisSecond, 0);

			long accTicks = 0;
			for (var i = 0; i < _drawToPresentTimeTicks.Length; i++)
			{
				accTicks += Interlocked.Read(ref _drawToPresentTimeTicks[i]);
			}
			_drawToPresentDelayMs = TimeSpan.FromTicks(accTicks).TotalMilliseconds / _drawToPresentTimeTicks.Length;

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
			_fps = 0;
			_frameTime = 0;
			_droppedFrames = 0;
			_unpresentedFrames = 0;
			_drawToPresentDelayMs = 0;
		}

		public void Dispose() => _fpsTimer.Dispose();
	}
}
