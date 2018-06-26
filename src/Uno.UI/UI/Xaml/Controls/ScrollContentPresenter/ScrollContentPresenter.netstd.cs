using Uno.Extensions;
using Uno.Logging;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	public partial class ScrollContentPresenter : ContentPresenter
	{
		private ScrollBarVisibility _verticalScrollBarVisibility;
		private ScrollBarVisibility _horizotalScrollBarVisibility;

		public ScrollMode HorizontalScrollMode { get; set; }

		public ScrollMode VerticalScrollMode { get; set; }

		public float MinimumZoomScale { get; private set; }

		public float MaximumZoomScale { get; private set; }

		public ScrollBarVisibility VerticalScrollBarVisibility
		{
			get { return _verticalScrollBarVisibility; }
			set
			{
				_verticalScrollBarVisibility = value;
				SetStyle("overflow-y", GetCssOverflow(VerticalScrollBarVisibility));
			}
		}

		public ScrollBarVisibility HorizontalScrollBarVisibility
		{
			get { return _horizotalScrollBarVisibility; }
			set
			{
				_horizotalScrollBarVisibility = value;
				SetStyle("overflow-x", GetCssOverflow(HorizontalScrollBarVisibility));
			}
		}

		private string GetCssOverflow(ScrollBarVisibility scrollBarVisibility)
		{
			switch (scrollBarVisibility)
			{
				case ScrollBarVisibility.Auto:
					return "auto";
				case ScrollBarVisibility.Disabled:
					return "hidden";
				case ScrollBarVisibility.Hidden:
					return "scroll"; // TODO
				case ScrollBarVisibility.Visible:
					return "scroll";
				default:
					return "hidden"; // TODO
			}
		}

		protected override Foundation.Size MeasureOverride(Foundation.Size size)
		{
			var child = Content as UIElement;
			if (child != null)
			{
				var slotSize = size;
				if (VerticalScrollBarVisibility != ScrollBarVisibility.Disabled)
				{
					slotSize.Height = double.PositiveInfinity;
				}
				if (HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled)
				{
					slotSize.Width = double.PositiveInfinity;
				}

				child.Measure(slotSize);

				return new Size(
					Math.Min(size.Width, child.DesiredSize.Width),
					Math.Min(size.Height, child.DesiredSize.Height)
				);
			}

			return new Foundation.Size(0, 0);
		}

		protected override Foundation.Size ArrangeOverride(Foundation.Size finalSize)
		{
			var child = Content as UIElement;
			if (child != null)
			{
				var slotSize = finalSize;

				var desiredChildSize = child.DesiredSize;

				if (VerticalScrollBarVisibility != ScrollBarVisibility.Disabled)
				{
					slotSize.Height = Math.Max(desiredChildSize.Height, finalSize.Height);
				}
				if (HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled)
				{
					slotSize.Width = Math.Max(desiredChildSize.Width, finalSize.Width);
				}

				child.Arrange(new Rect(new Point(0, 0), slotSize));
			}

			return finalSize;
		}

		internal override bool IsViewHit()
		{
			return true;
		}
	}
}
