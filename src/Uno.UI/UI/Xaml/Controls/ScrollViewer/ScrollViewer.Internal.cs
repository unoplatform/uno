using System.Runtime.CompilerServices;
using NotImplementedException = System.NotImplementedException;

namespace Microsoft.UI.Xaml.Controls
{
	partial class ScrollViewer
	{
		internal void DisableOverpan()
		{
#if __SKIA__
			DisableOverpanImpl();
#endif
		}

		internal void EnableOverpan()
		{
#if __SKIA__
			EnableOverpanImpl();
#endif
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool ChangeViewWithOptionalAnimation(
			double? horizontalOffset,
			double verticalOffset,
			float? zoomFactor,
			bool disableAnimation)
		{
			return ChangeView(horizontalOffset, verticalOffset, zoomFactor, disableAnimation);
		}
	}
}
