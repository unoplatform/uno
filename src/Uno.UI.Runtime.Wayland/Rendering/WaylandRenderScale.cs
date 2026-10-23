#nullable enable

using System;
using Uno.UI.Runtime.Wayland.Protocols;

namespace Uno.UI.Runtime.Wayland.Rendering;

internal sealed class WaylandRenderScale
{
	private readonly WlSurface _surface;
	private readonly WpViewport? _viewport;
	private int _integerScale = 1;
	private uint _preferredScale;
	public WaylandRenderScale(WlSurface surface, WpViewport? viewport)
	{
		_surface = surface;
		_viewport = viewport;
	}
	public double EffectiveScale => Compute(_viewport is not null, _preferredScale, _integerScale);
	public event EventHandler? Changed;
	public void SetIntegerScale(int scale)
	{
		if (scale < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(scale));
		}
		var previous = EffectiveScale;
		_integerScale = scale;
		if (previous != EffectiveScale)
		{
			Changed?.Invoke(this, EventArgs.Empty);
		}
	}
	public void SetPreferredScale(uint scale)
	{
		if (scale == 0)
		{
			throw new ArgumentOutOfRangeException(nameof(scale));
		}
		var previous = EffectiveScale;
		_preferredScale = scale;
		if (previous != EffectiveScale)
		{
			Changed?.Invoke(this, EventArgs.Empty);
		}
	}
	public (int Width, int Height) Apply(int width, int height)
	{
		if (width <= 0 || height <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(width));
		}
		if (_surface.Version >= 3)
		{
			_surface.SetBufferScale(_viewport is not null ? 1 : _integerScale);
		}
		_viewport?.SetDestination(width, height);
		return (ToBufferPixels(width, EffectiveScale), ToBufferPixels(height, EffectiveScale));
	}

	// wp_fractional_scale_v1: buffer size is the logical size times scale, rounded to nearest with halves up.
	public static int ToBufferPixels(double logical, double scale)
		=> checked((int)Math.Floor(logical * scale + 0.5));

	public static double Compute(bool hasViewport, uint preferredScale, int integerScale)
		=> hasViewport && preferredScale != 0 ? preferredScale / 120d : Math.Max(integerScale, 1);
}
