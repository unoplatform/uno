#if !__IOS__ && !__MACOS__ && !__SKIA__ && !__ANDROID__
#define LEGACY_SHAPE_MEASURE
#endif

#if LEGACY_SHAPE_MEASURE
#nullable enable

namespace Windows.UI.Xaml.Shapes;

public abstract partial class ArbitraryShapeBase : Shape
{

}
#endif
