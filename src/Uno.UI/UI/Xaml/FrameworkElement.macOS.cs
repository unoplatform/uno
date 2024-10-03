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
		public override bool NeedsLayout
		{
			set
			{
				base.NeedsLayout = value;
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
			base.Layout();

			if (!IsVisualTreeRoot && !_isSettingFrameByArrangeVisual)
			{
				// This handles native-only elements with managed child/children.
				// When the parent is native-only element, it will layout its children with the proper rect.
				// So we response to the requested bounds and do the managed arrange.

				var logical = this.Frame.PhysicalToLogicalPixels();
				this.Arrange(logical);
			}
		}

		public CGSize SizeThatFits(CGSize size)
		{
			this.Measure(size);
			return this.DesiredSize;
		}
	}
}
