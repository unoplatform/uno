using System.Runtime.CompilerServices;
using NotImplementedException = System.NotImplementedException;

namespace Windows.UI.Xaml.Controls
{
	partial class ScrollViewer
	{
		internal void DisableOverpan()
		{
			// TODO
		}

		internal void EnableOverpan()
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
