#nullable enable

using Uno.UI.Composition.Drawing;
using Windows.UI;

namespace Uno.UI.Composition.Composition;

/// <summary>
/// Captures the state of drop shadow for a visual.
/// </summary>
internal record ShadowState(float Dx, float Dy, float SigmaX, float SigmaY, Color Color)
{
	private IEffectFilter? _shadowFilter;

	/// <summary>
	/// A backend drop-shadow filter (offset + blur + color). Applied via
	/// <see cref="IDrawingSession.SaveLayer(IEffectFilter)"/> to derive a shadow from arbitrary content on the
	/// non-analytic fallback path.
	/// </summary>
	public IEffectFilter GetShadowFilter(IDrawingFactory factory) =>
		_shadowFilter ??= factory.CreateDropShadowFilter(Dx, Dy, SigmaX, SigmaY, Color);
}
