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

namespace Windows.UI.Xaml
{
	public partial class FrameworkElement
	{
		/// <summary>
		/// When set, measure and invalidate requests will not be propagated further up the visual tree, ie they won't trigger a relayout.
		/// Used where repeated unnecessary measure/arrange passes would be unacceptable for performance (eg scrolling in a list).
		/// </summary>
		internal bool ShouldInterceptInvalidate { get; set; }

		public override bool NeedsLayout
		{
			set
			{
				if (!_inLayoutSubviews)
				{
					base.NeedsLayout = value;
				}

				if (ShouldInterceptInvalidate)
				{
					return;
				}
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
		}

		public CGSize SizeThatFits(CGSize size)
		{
			try
			{
				_inLayoutSubviews = true;

				var xamlMeasure = XamlMeasure(size);

				if (xamlMeasure != null)
				{
					return _lastMeasure = xamlMeasure.Value;
				}
				else
				{
					return _lastMeasure = CGSize.Empty;
				}
			}
			finally
			{
				_inLayoutSubviews = false;
			}
		}
	}
}
