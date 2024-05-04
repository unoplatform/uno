namespace Uno.UI.Composition;

internal readonly record struct Thickness(float Left, float Top, float Right, float Bottom)
{
	public Thickness Inverse() => new Thickness(-Left, -Top, -Right, -Bottom);
}
