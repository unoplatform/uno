using Android.Views;
using Android.Widget;
using Uno.Foundation.Logging;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Uno.UI.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using Android.Graphics;
using Android.Graphics.Drawables;
using System.Drawing;
using System.Linq;
using Uno.UI;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class Page
	{
		private void UpdateBorder()
		{
			UpdateBorder(false);
		}

		private void UpdateBorder(bool willUpdateMeasures)
		{
			if (IsLoaded)
			{
				_borderRenderer.UpdateLayer(
					Background,
					BackgroundSizing.InnerBorderEdge,
					Thickness.Empty,
					null,
					CornerRadius.None,
					Thickness.Empty,
					willUpdateMeasures
				);
			}
		}
	}
}
