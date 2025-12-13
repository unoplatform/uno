#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
		var canvas = _recorder.BeginRecording(new SKRect(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue));
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
		public readonly record struct FrameDisposable(FpsHelper @this) : IDisposable
		{
			public void Dispose() => @this.EndFrame();
		}

		private static readonly SKPaint _blackPaint = new() { Color = SKColors.Black };
		private static readonly SKPaint _redPaint = new() { Color = SKColors.Red };
		private static readonly SKFont _font = new() { Size = 20, Embolden = true };

		private readonly TimeSpan[] _frameTimes;
		private readonly Timer _fpsTimer;
		private int _frameTimesHead;
		private int _framesRenderedInLastSecond;
		private long _currentFrameBeginTimestamp;

		public FpsHelper(int numberOfFramesToCalculateFrameTime = 10)
		{
			_frameTimes = new TimeSpan[numberOfFramesToCalculateFrameTime];
			_fpsTimer = new Timer(static state => (state as FpsHelper)?.TimerTick(), this, TimeSpan.Zero, TimeSpan.FromSeconds(1));
		}

		public double Fps { get; private set; }
		public double FrameTime { get; private set; }

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

		public void DrawFps(SKCanvas canvas)
		{
			if (!Application.Current.DebugSettings.EnableFrameRateCounter)
			{
				return;
			}

			var text = $"{Fps:F1}   {FrameTime:F1}";
			_font.MeasureText(text, out var rect);
			if (Scale is { } scale)
			{
				canvas.Save();
				canvas.Scale(scale, scale);
			}
			canvas.DrawRect(new SKRect(0, 0, rect.Width, rect.Height), _blackPaint);
			canvas.DrawText(text, 0, -rect.Top, _font, _redPaint);
			if (Scale is not null)
			{
				canvas.Restore();
			}
		}

		private void TimerTick()
		{
			Fps = _framesRenderedInLastSecond;
			_framesRenderedInLastSecond = 0;
		}

		public void Dispose() => _fpsTimer.Dispose();
	}
}
