#if HAS_UNO_WINUI

using Windows.Foundation;

namespace Microsoft.UI.Input
{
	public partial interface IPointerPointTransform
	{
		IPointerPointTransform Inverse
		{
			get;
		}

		bool TryTransform(Point inPoint, out Point outPoint);

		bool TryTransformBounds(Rect inRect, out Rect outRect);
	}
}
#endif
