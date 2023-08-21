#pragma warning disable 108 // new keyword hiding
using System;

namespace Windows.UI.Xaml.Controls
{
	public partial class VirtualizingPanel : Panel, IVirtualizingPanel
	{
		public VirtualizingPanel()
		{

		}

		public VirtualizingPanelLayout GetLayouter() => GetLayouterCore();

		private protected virtual VirtualizingPanelLayout GetLayouterCore() => throw new NotSupportedException($"This method must be overridden by implementing classes.");

		internal override Orientation? InternalOrientation => GetLayouter().Orientation;

		protected override Size MeasureOverride(Size availableSize)
		{
			// Copied from FrameworkElement.MeasureOverride
			var child = this.FindFirstChild();
			return child != null ? MeasureElement(child, availableSize) : new Size(0, 0);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			// Copied from FrameworkElement.ArrangeOVerride
			var child = this.FindFirstChild();

			if (child != null)
			{
#if UNO_REFERENCE_API
				child.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
#else
				ArrangeElement(child, new Rect(0, 0, finalSize.Width, finalSize.Height));
#endif
				return finalSize;
			}
			else
			{
				return finalSize;
			}
		}
	}
}
