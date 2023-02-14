#if !__IOS__ && !__MACOS__ && !__SKIA__ && !__ANDROID__
#define LEGACY_SHAPE_MEASURE
#endif

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Shapes
{
	public partial class Ellipse
#if LEGACY_SHAPE_MEASURE
		: ArbitraryShapeBase
#endif
	{
	}
}
