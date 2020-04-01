using Windows.Foundation;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Ellipse
#if !__IOS__
		: ArbitraryShapeBase
#endif
	{
	}
}
