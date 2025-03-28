using System;
using System.Collections.Generic;
using System.Text;
using UIKit;

namespace Windows.UI.Xaml.Controls
{
	public partial class PivotItem
	{
		public UIImage Image
		{
			get { return (UIImage)this.GetValue(ImageProperty); }
			set { this.SetValue(ImageProperty, value); }
		}

		public static DependencyProperty ImageProperty { get; } =
			DependencyProperty.Register("Image", typeof(UIImage), typeof(PivotItem), new FrameworkPropertyMetadata(default(UIImage)));
	}
}
