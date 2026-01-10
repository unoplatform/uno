using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace Uno.UI.Samples.UITests.ImageBrushTestControl
{
	[Sample("Brushes", "ImageBrushChangingCornerRadius")]
	public sealed partial class ImageBrushChangingCornerRadius : UserControl
	{
		public ImageBrushChangingCornerRadius()
		{
			this.InitializeComponent();
		}

		int n = 0;
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			n++;
			MyBorder.CornerRadius = new CornerRadius(n * 5);
		}
	}
}
