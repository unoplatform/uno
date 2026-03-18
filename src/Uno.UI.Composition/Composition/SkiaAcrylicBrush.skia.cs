#nullable enable
using System;
using SkiaSharp;

namespace Microsoft.UI.Composition;

internal class SkiaAcrylicBrush : CompositionBrush
{
	private float _blurSigma;
	private bool _isOpaque;
	private SKRect _cachedBounds;
	private bool? _cachedCompMode;

	private SKImageFilter? _filter;
	private SKColor _luminosityColor;
	private SKImage? _noiseImage;
	private float _noiseOpacity;

	private SKPaint? _noisePaint;
	private SKPaint? _opacityPaint;
	private SKPaint? _opaqueTintPaint;
	private SKColor _tintColor;

	public SkiaAcrylicBrush(Compositor compositor) : base(compositor)
	{
	}

	public float BlurSigma
	{
		get => _blurSigma;
		set => SetProperty(ref _blurSigma, value);
	}

	public bool IsOpaque
	{
		get => _isOpaque;
		set => SetProperty(ref _isOpaque, value);
	}

	public SKColor LuminosityColor
	{
		get => _luminosityColor;
		set => SetObjectProperty(ref _luminosityColor, value);
	}

	public SKColor TintColor
	{
		get => _tintColor;
		set => SetObjectProperty(ref _tintColor, value);
	}

	public float NoiseOpacity
	{
		get => _noiseOpacity;
		set => SetProperty(ref _noiseOpacity, value);
	}

	public SKImage? NoiseImage
	{
		get => _noiseImage;
		set => SetObjectProperty(ref _noiseImage, value);
	}

	internal override bool RequiresRepaintOnEveryFrame => !_isOpaque;

	internal override bool CanPaint() => true;

	internal override void Paint(SKCanvas canvas, float opacity, SKRect bounds)
	{
		if (_isOpaque)
		{
			PaintOpaque(canvas, opacity, bounds);
		}
		else
		{
			PaintTranslucent(canvas, opacity, bounds);
		}
	}

	private void PaintOpaque(SKCanvas canvas, float opacity, SKRect bounds)
	{
		// Opaque tint: no backdrop blur or luminosity needed — just solid tint + noise.
		_opaqueTintPaint ??= new SKPaint();
		_opaqueTintPaint.Color = opacity < 1
			? new SKColor(_tintColor.Red, _tintColor.Green, _tintColor.Blue, (byte)(_tintColor.Alpha * opacity))
			: _tintColor;

		canvas.DrawRect(bounds, _opaqueTintPaint);

		DrawNoise(canvas, opacity, bounds);
	}

	private void PaintTranslucent(SKCanvas canvas, float opacity, SKRect bounds)
	{
		EnsureFilter(bounds);

		// The blur + luminosity + tint effect is applied as a backdrop filter.
		// SaveLayer with Backdrop filters the canvas content behind and the layer
		// itself starts empty, so blend-mode canvas draws would not interact with
		// the blurred content. Instead, all blending is done in the filter chain.
		// When opacity < 1, the SaveLayerRec paint modulates the entire layer
		// (filtered backdrop + noise) when composited back onto the canvas.
		var rec = new SKCanvasSaveLayerRec { Backdrop = _filter };
		if (opacity < 1)
		{
			_opacityPaint ??= new SKPaint();
			_opacityPaint.Color = new SKColor(0xFF, 0xFF, 0xFF, (byte)(0xFF * opacity));
			rec.Paint = _opacityPaint;
		}

		canvas.SaveLayer(rec);

		DrawNoise(canvas, 1f, bounds);

		canvas.Restore();
	}

	private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
	{
		base.OnPropertyChangedCore(propertyName, isSubPropertyChange);

		switch (propertyName)
		{
			case nameof(NoiseImage):
				if (_noisePaint is not null)
				{
					_noisePaint.Shader = _noiseImage?.ToShader(SKShaderTileMode.Repeat, SKShaderTileMode.Repeat);
				}
				break;
			case nameof(NoiseOpacity):
				if (_noisePaint is not null)
				{
					_noisePaint.ColorFilter = SKColorFilter.CreateColorMatrix(new[]
					{
						1, 0, 0, 0,
						0, 0, 1, 0,
						0, 0, 0, 0,
						1, 0, 0, 0,
						0, 0, _noiseOpacity, 0
					});
				}
				break;
			case nameof(IsOpaque):
			case nameof(LuminosityColor):
			case nameof(TintColor):
			case nameof(BlurSigma):
				_filter?.Dispose();
				_filter = null;
				break;
		}
	}

	private void DrawNoise(SKCanvas canvas, float opacity, SKRect bounds)
	{
		if (_noiseImage is not null)
		{
			if (_noisePaint is null)
			{
				_noisePaint = new SKPaint();
				_noisePaint.Shader = _noiseImage.ToShader(SKShaderTileMode.Repeat, SKShaderTileMode.Repeat);
				_noisePaint.ColorFilter = SKColorFilter.CreateColorMatrix(new[]
				{
					1, 0, 0, 0, 0,
					0, 1, 0, 0, 0,
					0, 0, 1, 0, 0,
					0, 0, 0, _noiseOpacity, 0
				});
			}

			// Paint Color alpha modulates shader + color filter output,
			// giving us effective alpha = noiseTextureAlpha * _noiseOpacity * opacity.
			_noisePaint.Color = new SKColor(0xFF, 0xFF, 0xFF, (byte)(0xFF * opacity));
			canvas.DrawRect(bounds, _noisePaint);
		}
	}

	private void EnsureFilter(SKRect bounds)
	{
		if (_cachedBounds == bounds && _filter is not null && _cachedCompMode == Compositor.IsSoftwareRenderer)
		{
			return;
		}

		_filter?.Dispose();

		const int blurPadding = 100;
		var blurBounds = bounds with
		{
			Left = -blurPadding,
			Top = -blurPadding,
			Right = bounds.Right + blurPadding,
			Bottom = bounds.Bottom + blurPadding
		};

		var scaleFactor = Math.Max(1, (int)(_blurSigma / 8));
		if (scaleFactor <= 1)
		{
			// 1. Blur
			var blurFilter = SKImageFilter.CreateBlur(_blurSigma, _blurSigma, null, blurBounds);

			// 2. Luminosity blend
			var luminositySource = SKImageFilter.CreateColorFilter(
				SKColorFilter.CreateBlendMode(_luminosityColor, SKBlendMode.Src), null, bounds);
			var luminosityBlend = SKImageFilter.CreateBlendMode(
				SKBlendMode.Luminosity, blurFilter, luminositySource, bounds);

			// 3. Tint blend
			var tintSource = SKImageFilter.CreateColorFilter(
				SKColorFilter.CreateBlendMode(_tintColor, SKBlendMode.Src), null, bounds);
			_filter = SKImageFilter.CreateBlendMode(
				SKBlendMode.Color, luminosityBlend, tintSource, bounds);
		}
		else
		{
			// Downscale → blur → luminosity → tint at reduced resolution → upscale.
			var inv = 1.0f / scaleFactor;
			var sampling = new SKSamplingOptions(SKFilterMode.Linear);

			// 1. Blur at reduced resolution
			var downscaled = SKImageFilter.CreateMatrix(SKMatrix.CreateScale(inv, inv), sampling);
			var blurred = SKImageFilter.CreateBlur(_blurSigma * inv, _blurSigma * inv, downscaled);

			// 2. Luminosity blend at reduced resolution
			var luminositySource = SKImageFilter.CreateColorFilter(
				SKColorFilter.CreateBlendMode(_luminosityColor, SKBlendMode.Src), null);
			var luminosityBlend = SKImageFilter.CreateBlendMode(
				SKBlendMode.Luminosity, blurred, luminositySource);

			// 3. Tint blend at reduced resolution
			var tintSource = SKImageFilter.CreateColorFilter(
				SKColorFilter.CreateBlendMode(_tintColor, SKBlendMode.Src), null);
			var tintBlend = SKImageFilter.CreateBlendMode(
				SKBlendMode.Color, luminosityBlend, tintSource);

			// 4. Upscale back to full resolution
			var upscaled = SKImageFilter.CreateMatrix(
				SKMatrix.CreateScale(scaleFactor, scaleFactor), sampling, tintBlend);
			_filter = SKImageFilter.CreateMerge([upscaled], blurBounds);
		}

		_cachedBounds = bounds;
		_cachedCompMode = Compositor.IsSoftwareRenderer;
	}

	private protected override void DisposeInternal()
	{
		base.DisposeInternal();
		_filter?.Dispose();
		_noisePaint?.Dispose();
		_opacityPaint?.Dispose();
		_opaqueTintPaint?.Dispose();
	}
}
