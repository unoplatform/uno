using System;
using System.Collections.Generic;
using Windows.Foundation;
using System.Linq;
using Uno.Disposables;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno;
using Uno.Foundation.Logging;
using View = Windows.UI.Xaml.UIElement;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	partial class Layouter : ILayouter
	{
		protected Size MeasureChildOverride(View view, Size slotSize)
		{
			view.Measure(slotSize);

			return view.DesiredSize;
		}

		protected void ArrangeChildOverride(View view, Rect frame)
		{
			view.Arranged = frame;
			view.LayoutSlotWithMarginsAndAlignments = frame;

			LayoutInformation.SetLayoutSlot(view, frame);
		}

		protected Size DesiredChildSize(View view)
		{
			return view.DesiredSize;
		}
	}
}
