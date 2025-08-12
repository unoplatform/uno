#nullable enable

using System;
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
	internal static bool CanRecordPicture([NotNullWhen(true)] UIElement? rootElement) =>
		rootElement is not null &&
		(!FeatureConfiguration.Rendering.GenerateNewFramesOnlyWhenUITreeIsArranged || rootElement is { IsArrangeDirtyOrArrangeDirtyPath: false, IsMeasureDirtyOrMeasureDirtyPath: false });

	internal static (SKPicture, SKPath) RecordPictureAndReturnPath(int width, int height, UIElement rootElement, bool invertPath, bool applyScaling)
	{
		var xamlRoot = rootElement.XamlRoot;
		var scale = (float)(xamlRoot?.RasterizationScale ?? 1.0f);

		using var recorder = new SKPictureRecorder();
		using var canvas = recorder.BeginRecording(new SKRect(-999999, -999999, 999999, 999999));
		using var _ = new SKAutoCanvasRestore(canvas, true);
		canvas.Clear(SKColors.Transparent);
		canvas.Scale(scale);
		rootElement.Visual.Compositor.RenderRootVisual(canvas, rootElement.Visual);
		var path = CalculateClippingPath(width, height, rootElement.Visual, canvas, invertPath, applyScaling);
		var picture = recorder.EndRecording();

		xamlRoot?.InvokeFramePainted();

		return (picture, path);
	}

	internal static void RenderPicture(SKSurface surface, SKPicture? picture, SKColor background, FpsHelper fpsHelper)
	{
		using var fpsHelperDisposable = fpsHelper.BeginFrame();
		var canvas = surface.Canvas;
		using (new SKAutoCanvasRestore(canvas, true))
		{
			canvas.Clear(background);
			if (picture is not null)
			{
				// This might happen if we get render request before the first frame is painted
				canvas.DrawPicture(picture);
			}

			fpsHelper.DrawFps(canvas);
		}

		// update the control
		canvas.Flush();
		surface.Flush();
	}

	/// <summary>
	/// Does a rendering cycle and returns a path that represents the visible area of the native views.
	/// Takes the current TotalMatrix of the surface's canvas into account
	/// </summary>
	private static SKPath CalculateClippingPath(int width, int height, ContainerVisual rootVisual, SKCanvas canvas, bool invertPath, bool applyScaling)
	{
		SKPath outPath = new SKPath();
		if (ContentPresenter.HasNativeElements())
		{
			var parentClipPath = new SKPath();
			parentClipPath.AddRect(new SKRect(0, 0, width, height));
			rootVisual.GetNativeViewPath(parentClipPath, outPath);
			if (applyScaling)
			{
				outPath.Transform(canvas.TotalMatrix, outPath); // canvas.TotalMatrix should be the same before and after RenderRootVisual because of the Save and Restore calls inside
			}
		}

		if (invertPath)
		{
			var invertedPath = new SKPath();
			invertedPath.AddRect(new SKRect(0, 0, width, height));
			if (applyScaling)
			{
				invertedPath.Transform(canvas.TotalMatrix, invertedPath);
			}
			invertedPath.Op(outPath, SKPathOp.Difference, invertedPath);
			return invertedPath;
		}
		else
		{
			return outPath;
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
