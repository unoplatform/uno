using CoreGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UIKit;
using Uno.Media;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Path
	{
		protected override CGPath GetPath()
		{
			var streamGeometry = Data.ToStreamGeometry();
			return streamGeometry?.ToCGPath();
		}

		partial void OnDataChanged()
		{
			SetNeedsLayout();
		}
	}
}
