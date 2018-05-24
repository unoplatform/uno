using Android.Graphics;
using Uno.Media;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Path : ArbitraryShapeBase
	{
		protected override Android.Graphics.Path GetPath()
		{
			var streamGeometry = Data.ToStreamGeometry();
			return streamGeometry?.ToPath();
		}

		partial void OnDataChanged()
		{
			RequestLayout();
		}
	}
}
