#if UNO_HAS_MANAGED_SCROLL_PRESENTER
#nullable enable

using System;
using System.Linq;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// Supports scrolling of the managed ScrollContentPresenter using RenderTransform
	/// </summary>
	internal class TransformScrollStrategy : IScrollStrategy
	{
		public static TransformScrollStrategy Instance { get; } = new TransformScrollStrategy();

		private TransformScrollStrategy() { }

		public void Initialize(ScrollContentPresenter presenter) { }

		public void Update(UIElement view, double horizontalOffset, double verticalOffset, double zoom, bool disableAnimation)
		{
			var transform = view.RenderTransform as CompositeTransform ?? (CompositeTransform)(view.RenderTransform = new CompositeTransform());

			transform.TranslateX = -horizontalOffset;
			transform.TranslateY = -verticalOffset;
			transform.ScaleX = zoom;
			transform.ScaleY = zoom;
		}
	}
}
#endif
