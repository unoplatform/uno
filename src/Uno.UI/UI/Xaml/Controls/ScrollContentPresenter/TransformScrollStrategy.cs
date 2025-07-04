#if UNO_HAS_MANAGED_SCROLL_PRESENTER
#nullable enable

using System;
using System.Linq;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Supports scrolling of the managed ScrollContentPresenter using RenderTransform
	/// </summary>
	internal class TransformScrollStrategy : IScrollStrategy
	{
		public event EventHandler<StrategyUpdateEventArgs>? Updated;

		public static TransformScrollStrategy Instance { get; } = new();

		private TransformScrollStrategy() { }

		public void Initialize(ScrollContentPresenter presenter) { }

		public void Update(UIElement view, double horizontalOffset, double verticalOffset, double zoom, ScrollOptions options)
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
