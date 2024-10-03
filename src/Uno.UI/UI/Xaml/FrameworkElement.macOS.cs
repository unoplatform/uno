using CoreGraphics;
using Uno.Extensions;
using Uno.UI.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Windows.Foundation;
using Uno.Foundation.Logging;
using Uno.UI;
using AppKit;

namespace Microsoft.UI.Xaml
{
	public partial class FrameworkElement
	{
#if UNO_USES_LAYOUTER
		/// <summary>
		/// When set, measure and invalidate requests will not be propagated further up the visual tree, ie they won't trigger a relayout.
		/// Used where repeated unnecessary measure/arrange passes would be unacceptable for performance (eg scrolling in a list).
		/// </summary>
		internal bool ShouldInterceptInvalidate { get; set; }
#endif

		public override bool NeedsLayout
		{
			set
			{
#if UNO_USES_LAYOUTER
				if (!_inLayoutSubviews)
				{
					base.NeedsLayout = value;
				}

				if (ShouldInterceptInvalidate)
				{
					return;
				}
#else
				base.NeedsLayout = value;
#endif
			}
		}

		protected internal override void OnInvalidateMeasure()
		{
			base.OnInvalidateMeasure();

			// Note that the reliance on NSView.NeedsLayout to invalidate the measure / arrange phases for
			// self and parents.NeedsLayout is set to true when NSView.Frame is different from iOS, causing
			// a chain of multiple unneeded updates for the element and its parents. OnInvalidateMeasure
			// sets NeedsLayout to true and propagates to the parent but NeedsLayout by itself does not.

			if (!IsMeasureDirty)
			{
				InvalidateMeasure();
				InvalidateArrange();

				if (Parent is FrameworkElement fe)
				{
					if (!fe.IsMeasureDirty)
					{
						fe.InvalidateMeasure();
					}
				}
				else if (Parent is IFrameworkElement ife)
				{
					ife.InvalidateMeasure();
				}
			}
		}

		public override void Layout()
		{
#if UNO_USES_LAYOUTER
			try
			{
				_inLayoutSubviews = true;

				var bounds = Bounds.Size;
				if (IsMeasureDirty)
				{
					XamlMeasure(bounds);
				}

				OnBeforeArrange();

				var size = SizeFromUISize(bounds);

				_layouter.Arrange(new Rect(0, 0, size.Width, size.Height));

				OnAfterArrange();
			}
			catch (Exception e)
			{
				this.Log().Error($"Layout failed in {GetType()}", e);
			}
			finally
			{
				_inLayoutSubviews = false;

				ClearLayoutFlags(LayoutFlag.MeasureDirty | LayoutFlag.ArrangeDirty);
			}
#else
			base.Layout();

			if (!IsVisualTreeRoot && !_isSettingFrameByArrangeVisual)
			{
				// This handles native-only elements with managed child/children.
				// When the parent is native-only element, it will layout its children with the proper rect.
				// So we response to the requested bounds and do the managed arrange.

				var logical = this.Frame.PhysicalToLogicalPixels();
				this.Arrange(logical);
			}
#endif
		}

		public CGSize SizeThatFits(CGSize size)
		{
			this.Measure(size);
			return this.DesiredSize;
		}
	}
}
