using System;
using System.Drawing;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Windows.UI.Xaml.Controls
{
	public partial class Page
	{
		private void InitializeBorder()
		{
		}

		private void UpdateBorder()
		{
			SetBorder(Thickness.Empty, null, CornerRadius.None);
			SetAndObserveBackgroundBrush(Background);
		}
	}
}
