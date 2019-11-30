using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ListView
{
	public partial class MeasureDetectorInner : ContentControl
	{
		public event Action WasMeasured;
		public event Action WasArranged;

		protected override Size MeasureOverride(Size availableSize)
		{
			WasMeasured?.Invoke();
			return base.MeasureOverride(availableSize);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			WasArranged?.Invoke();
			return base.ArrangeOverride(finalSize);
		}
	}
}
