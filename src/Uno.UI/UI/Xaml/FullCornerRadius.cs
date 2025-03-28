using Windows.Foundation;

#if IS_UNO_COMPOSITION
namespace Uno.UI.Composition;
#else
namespace Windows.UI.Xaml;
#endif

internal partial record struct FullCornerRadius
(
	NonUniformCornerRadius Outer,
	NonUniformCornerRadius Inner
)
{
	public static FullCornerRadius None { get; }

	public bool IsEmpty => Outer.IsEmpty && Inner.IsEmpty;
}
