using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class MeasureCountCarouselPanel : CarouselPanel
	{
		public static int MeasureCount { get; private set; }
		public static int ArrangeCount { get; private set; }

		public static void Reset()
		{
			MeasureCount = 0;
			ArrangeCount = 0;
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			MeasureCount++;
			return base.MeasureOverride(availableSize);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			ArrangeCount++;
			return base.ArrangeOverride(finalSize);
		}
	}
}
