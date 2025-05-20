#nullable enable

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SkiaSharp;

namespace Uno.UI.Helpers;

internal static class SkiaRenderHelper
{
	/// <summary>
	/// Does a rendering cycle and returns a path that represents the visible area of the native views.
	/// Takes the current TotalMatrix of the surface's canvas into account
	/// </summary>
	public static SKPath RenderRootVisualAndReturnNegativePath(int width, int height, ContainerVisual rootVisual, SKCanvas canvas)
	{
		rootVisual.Compositor.RenderRootVisual(canvas, rootVisual);
		if (!ContentPresenter.HasNativeElements())
		{
			return new SKPath();
		}
		var initialCanvasTransform = canvas.TotalMatrix;
		rootVisual.Compositor.RenderRootVisual(canvas, rootVisual);
		var parentClipPath = new SKPath();
		parentClipPath.AddRect(new SKRect(0, 0, width, height));
		var outPath = new SKPath();
		rootVisual.GetNativeViewPath(parentClipPath, outPath);
		outPath.Transform(initialCanvasTransform, outPath);
		return outPath;
	}

	/// <summary>
	/// Does a rendering cycle and returns a path that represents the total area that was drawn
	/// minus the native view areas.
	/// </summary>
	public static SKPath RenderRootVisualAndReturnPath(int width, int height, ContainerVisual rootVisual, SKCanvas canvas)
	{
		var outPath = new SKPath();
		outPath.AddRect(new SKRect(0, 0, width, height));
		outPath.Transform(canvas.TotalMatrix, outPath);
		outPath.Op(RenderRootVisualAndReturnNegativePath(width, height, rootVisual, canvas), SKPathOp.Difference, outPath);
		return outPath;
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
