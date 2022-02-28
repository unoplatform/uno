using System;
using System.Drawing;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.Views.Controls;
using Uno.UI.DataBinding;
using UIKit;
using Windows.UI.Xaml.Shapes;

namespace Windows.UI.Xaml.Controls
{
	public partial class Page
	{
		public override void LayoutSubviews()
		{
			base.LayoutSubviews();
			UpdateBorder();
		}

		private void UpdateBorder()
		{
			if (IsLoaded)
			{
				_borderRenderer.UpdateLayer(
					Background,
					InternalBackgroundSizing,
					Thickness.Empty,
					null,
					CornerRadius.None,
					null
				);
			}
		}
	}
}
