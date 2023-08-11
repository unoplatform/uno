using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class NativeFlipView : PagedCollectionView
	{
		private Brush _background;

		public NativeFlipView()
		{
			ShowsHorizontalScrollIndicator = false;
		}

		public Brush Background
		{
			get => _background;
			set
			{
				_background = value;
				BackgroundColor = Brush.GetColorWithOpacity(_background);
			}
		}
	}
}
