#nullable disable

#if HAS_UNO_WINUI

namespace Microsoft.UI.Input
{
	public interface IPointerPointTransform
	{
		IPointerPointTransform Inverse
		{
			get;
		}

		bool TryTransform(Windows.Foundation.Point inPoint, out Windows.Foundation.Point outPoint);

		bool TryTransformBounds(Windows.Foundation.Rect inRect, out Windows.Foundation.Rect outRect);
	}
}
#endif
