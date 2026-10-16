#nullable enable

using System.Numerics;
using Windows.Foundation;
using Windows.UI;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Composition;

/// <summary>
/// The acrylic material brush, built directly on the neutral drawing seam rather than as a WinUI
/// composition-effect graph. The translucent look is a backdrop effect — blur → luminosity blend → tint blend —
/// expressed as a neutral <see cref="EffectNode"/> tree and applied with <see cref="IDrawingSession.DrawEffectBackdrop"/>;
/// a tiled noise texture is drawn on top. The opaque short-circuit skips the backdrop and just draws tint + noise.
/// </summary>
internal class AcrylicMaterialBrush : CompositionBrush
{
	// The blur samples well outside the painted bounds, so a translucent acrylic must repaint whenever anything
	// behind it changes; its damage region is likewise grown by this margin (see DamageRegionSamplingMargin).
	private const int BlurPadding = 100;

	private float _blurSigma;
	private bool _isOpaque;
	private Color _luminosityColor;
	private Color _tintColor;
	private float _noiseOpacity;
	private IImage? _noiseImage;
	// The texture is minted from the session that paints, and re-minted if a different one ever paints
	// this brush: a texture belongs to one device, and one brush can be shared across windows.
	private ITexture? _noiseTexture;
	private IDrawingFactory? _noiseFactory;

	private IEffectFilter? _filter;
	private Rect _cachedBounds;
	private Vector2 _cachedScale;
	// The filter is device-bound, so a re-bound renderer (DrawingFactory.Current swapped after a context loss)
	// invalidates it even at identical bounds.
	private IDrawingFactory? _cachedFactory;

	public AcrylicMaterialBrush(Compositor compositor) : base(compositor)
	{
	}

	public float BlurSigma { get => _blurSigma; set => SetProperty(ref _blurSigma, value); }
	public bool IsOpaque { get => _isOpaque; set => SetProperty(ref _isOpaque, value); }
	public Color LuminosityColor { get => _luminosityColor; set => SetObjectProperty(ref _luminosityColor, value); }
	public Color TintColor { get => _tintColor; set => SetObjectProperty(ref _tintColor, value); }
	public float NoiseOpacity { get => _noiseOpacity; set => SetProperty(ref _noiseOpacity, value); }
	public IImage? NoiseImage { get => _noiseImage; set => SetObjectProperty(ref _noiseImage, value); }

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
			session.DrawRect(bounds, opacity < 1 ? WithOpacity(_tintColor, opacity) : _tintColor);
		}
		else
		{
			EnsureFilter(session.Factory, bounds, GetRasterizationScale(session));
			if (_filter is { } filter)
			{
				session.DrawEffectBackdrop(filter, opacity);
			}
		}

		DrawNoise(session, opacity, bounds);
		return true;
	}

	private ITexture? ResolveNoiseTexture(IDrawingFactory factory)
	{
		if (_noiseImage is null)
		{
			return null;
		}

		if (_noiseTexture is null || !ReferenceEquals(_noiseFactory, factory))
		{
			_noiseTexture?.Dispose();
			_noiseTexture = factory.CreateTexture(_noiseImage);
			_noiseFactory = factory;
		}

		return _noiseTexture;
	}

	private void DrawNoise(IDrawingSession session, float opacity, Rect bounds)
	{
		if (ResolveNoiseTexture(session.Factory) is not { PixelWidth: > 0, PixelHeight: > 0 } texture)
		{
			return;
		}

		var effectiveOpacity = _noiseOpacity * opacity;
		if (effectiveOpacity <= 0f)
		{
			return;
		}

		// The grain repeats at 1:1, so it lands on texel centres and stays crisp; drawing it scaled to the bounds
		// would filter it into mush.
		session.DrawImageTiled(texture, bounds, EdgeExtend.Wrap, EdgeExtend.Wrap, effectiveOpacity);
	}

	private void EnsureFilter(IDrawingFactory factory, Rect bounds, Vector2 scale)
	{
		if (_filter is not null && _cachedBounds == bounds && _cachedScale == scale && ReferenceEquals(_cachedFactory, factory))
		{
			return;
		}

		if (ReferenceEquals(_cachedFactory, factory))
		{
			_filter?.Dispose();
		}

		// A filter from a re-bound renderer belongs to a device that is gone; drop it rather than calling into it.
		_filter = null;

		// Backdrop → Gaussian blur → luminosity blend (with the luminosity colour) → colour blend (with the tint).
		// Blur → Luminosity blend → Color blend, as a neutral effect tree.
		EffectNode tree =
			new BlendEffectNode(
				new BlendEffectNode(
					new BlurEffectNode(new SourceInput(), _blurSigma, ClampEdge: true),
					new ColorInput(_luminosityColor),
					BlendMode.Luminosity),
				new ColorInput(_tintColor),
				BlendMode.Color);

		_filter = factory.CreateEffectFilter(tree, bounds);
		_cachedBounds = bounds;
		_cachedScale = scale;
		_cachedFactory = factory;
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
