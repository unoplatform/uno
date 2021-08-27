using System.Runtime.CompilerServices;
using NotImplementedException = System.NotImplementedException;

namespace Windows.UI.Xaml.Controls
{
	partial class ScrollViewer
	{
		internal static void DisableOverpan()
		{
			// TODO
		}

		internal static void EnableOverpan()
		{
			// TODO
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
