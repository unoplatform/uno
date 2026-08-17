#nullable enable

using Windows.Foundation;
using Windows.UI;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Composition;

/// <summary>
/// The direct acrylic material brush, reimplemented on the neutral drawing seam (bc's SkiaAcrylicBrush, which used
/// SKImageFilter directly). The translucent look is a backdrop effect — blur → luminosity blend → tint blend —
/// expressed as a neutral <see cref="EffectNode"/> tree and applied with <see cref="IDrawingSession.DrawEffectBackdrop"/>;
/// a tiled noise texture is drawn on top. The opaque short-circuit skips the backdrop and just draws tint + noise.
/// </summary>
internal class SkiaAcrylicBrush : CompositionBrush
{
	// The blur samples well outside the painted bounds, so a translucent acrylic must repaint whenever anything
	// behind it changes; its damage region is likewise grown by this margin (see DamageRegionSamplingMargin).
	private const int BlurPadding = 100;

	private float _blurSigma;
	private bool _isOpaque;
	private Color _luminosityColor;
	private Color _tintColor;
	private float _noiseOpacity;
	private ITexture? _noiseTexture;

	private IEffectFilter? _filter;
	private Rect _cachedBounds;

	public SkiaAcrylicBrush(Compositor compositor) : base(compositor)
	{
	}

	public float BlurSigma { get => _blurSigma; set => SetProperty(ref _blurSigma, value); }
	public bool IsOpaque { get => _isOpaque; set => SetProperty(ref _isOpaque, value); }
	public Color LuminosityColor { get => _luminosityColor; set => SetObjectProperty(ref _luminosityColor, value); }
	public Color TintColor { get => _tintColor; set => SetObjectProperty(ref _tintColor, value); }
	public float NoiseOpacity { get => _noiseOpacity; set => SetProperty(ref _noiseOpacity, value); }
	public ITexture? NoiseTexture { get => _noiseTexture; set => SetObjectProperty(ref _noiseTexture, value); }

	// A translucent acrylic filters the live backdrop, so it must repaint every frame; an opaque one is static.
	internal override bool RequiresRepaintOnEveryFrame => !_isOpaque;

	// The blur reaches BlurPadding beyond the painted bounds, so the damage region must be grown to match.
	internal override float DamageRegionSamplingMargin => _isOpaque ? 0 : BlurPadding;

	internal override bool CanPaint() => true;

	internal override bool TryPaint(IDrawingSession session, float opacity, Rect bounds)
	{
		if (_isOpaque)
		{
			// Opaque tint: no backdrop blur or luminosity needed — just solid tint + noise.
			session.DrawRect(bounds, opacity < 1 ? WithOpacity(_tintColor, opacity) : _tintColor, antialias: false);
		}
		else
		{
			EnsureFilter(bounds);
			if (_filter is { } filter)
			{
				session.DrawEffectBackdrop(filter, opacity);
			}
		}

		DrawNoise(session, opacity, bounds);
		return true;
	}

	private void DrawNoise(IDrawingSession session, float opacity, Rect bounds)
	{
		if (_noiseTexture is not { PixelWidth: > 0, PixelHeight: > 0 } texture)
		{
			return;
		}

		var effectiveOpacity = _noiseOpacity * opacity;
		if (effectiveOpacity <= 0f)
		{
			return;
		}

		// Tile the noise texture across the bounds (bc used a Repeat shader; the neutral seam has no image shader,
		// so tile explicitly). Clip so the last row/column doesn't spill past the bounds.
		session.Save();
		session.ClipRect(bounds);
		for (var y = bounds.Top; y < bounds.Bottom; y += texture.PixelHeight)
		{
			for (var x = bounds.Left; x < bounds.Right; x += texture.PixelWidth)
			{
				session.DrawImage(texture, (float)x, (float)y, ImageSampling.NearestNeighbor, effectiveOpacity, antialias: false);
			}
		}
		session.Restore();
	}

	private void EnsureFilter(Rect bounds)
	{
		if (_filter is not null && _cachedBounds == bounds)
		{
			return;
		}

		_filter?.Dispose();

		// Backdrop → Gaussian blur → luminosity blend (with the luminosity colour) → colour blend (with the tint).
		// Mirrors bc's SKImageFilter chain (CreateBlur → Luminosity blend → Color blend) on the neutral effect tree.
		EffectNode tree =
			new BlendEffectNode(
				new BlendEffectNode(
					new BlurEffectNode(new SourceInput(), _blurSigma, ClampEdge: true),
					new ColorInput(_luminosityColor),
					BlendMode.Luminosity),
				new ColorInput(_tintColor),
				BlendMode.Color);

		_filter = DrawingFactory.Current.CreateEffectFilter(tree, bounds);
		_cachedBounds = bounds;
	}

	private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
	{
		base.OnPropertyChangedCore(propertyName, isSubPropertyChange);

		switch (propertyName)
		{
			case nameof(IsOpaque):
			case nameof(LuminosityColor):
			case nameof(TintColor):
			case nameof(BlurSigma):
				_filter?.Dispose();
				_filter = null;
				break;
		}
	}

	private static Color WithOpacity(Color color, float opacity)
		=> Color.FromArgb((byte)(color.A * opacity), color.R, color.G, color.B);

	private protected override void DisposeInternal()
	{
		base.DisposeInternal();
		_filter?.Dispose();
	}
}
