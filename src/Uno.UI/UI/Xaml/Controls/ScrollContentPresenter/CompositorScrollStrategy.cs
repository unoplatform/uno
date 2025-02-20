#if UNO_HAS_MANAGED_SCROLL_PRESENTER
#if __SKIA__
#nullable enable
using System.Numerics;

namespace Windows.UI.Xaml.Controls
{
	internal class CompositorScrollStrategy : IScrollStrategy
	{
		public static CompositorScrollStrategy Instance { get; } = new CompositorScrollStrategy();

		private CompositorScrollStrategy() { }

		/// <inheritdoc />
		public void Initialize(ScrollContentPresenter presenter)
			=> presenter.Visual.Clip = presenter.Visual.Compositor.CreateInsetClip(0, 0, 0, 0);

		public void Update(UIElement view, double horizontalOffset, double verticalOffset, double zoom, bool disableAnimation)
		{
			view.Visual.AnchorPoint = new Vector2((float)-horizontalOffset, (float)-verticalOffset);
		}
	}
}
#endif
#endif
