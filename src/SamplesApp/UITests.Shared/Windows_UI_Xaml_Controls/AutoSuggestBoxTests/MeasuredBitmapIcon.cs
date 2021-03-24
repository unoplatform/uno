using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml_Controls.AutoSuggestBoxTests
{
    public partial class MeasuredBitmapIcon : BitmapIcon
    {
		protected override Size ArrangeOverride(Size finalSize)
		{
			var size = base.ArrangeOverride(finalSize);
			global::System.Diagnostics.Debug.WriteLine("Arrange final size " + finalSize + ", result size " + size);
			return size;
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			var size = base.MeasureOverride(availableSize);
			global::System.Diagnostics.Debug.WriteLine("Measure available size " + availableSize + ", result size " + size);
			return size;
		}
	}
}
