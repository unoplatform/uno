using System.Runtime.CompilerServices;
using NotImplementedException = System.NotImplementedException;

namespace Microsoft.UI.Xaml.Controls
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
			double? verticalOffset,
			float? zoomFactor,
			bool disableAnimation)
		{
			return ChangeView(horizontalOffset, verticalOffset, zoomFactor, disableAnimation);
		}

		// MUX Reference ScrollViewer_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d
		internal bool IsThumbDragging
			=> (_horizontalScrollbar?.IsDragging ?? false) || (_verticalScrollbar?.IsDragging ?? false);

		// TODO Uno: there is no DirectManipulation zoom state on Uno.
		internal bool IsInDirectManipulationZoom => false;

		internal double HeaderHeight => Presenter?.Content is ItemsPresenter itemsPresenter ? itemsPresenter.GetHeaderSize().Height : 0.0;
	}
}
