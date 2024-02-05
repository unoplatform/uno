using System;
using System.Drawing;
using AppKit;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.DataBinding;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class Page
	{
		private BorderLayerRenderer _borderRenderer = new BorderLayerRenderer();

		private void InitializeBorder()
		{
			Loaded += (s, e) => UpdateBorder();
			Unloaded += (s, e) => _borderRenderer.Clear();
			LayoutUpdated += (s, e) => UpdateBorder();
		}

		public override void Layout()
		{
			base.Layout();
			UpdateBorder();
		}

		private void UpdateBorder()
		{
			if (IsLoaded)
			{
				_borderRenderer.UpdateLayer(
					this,
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
