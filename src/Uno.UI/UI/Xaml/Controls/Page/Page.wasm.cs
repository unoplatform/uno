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
		private BorderLayerRenderer _borderRenderer;

		private void InitializeBorder()
		{
			_borderRenderer ??= new BorderLayerRenderer(this);

			Loaded += (s, e) => UpdateBorder();
			Unloaded += (s, e) => _borderRenderer.Clear();
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
