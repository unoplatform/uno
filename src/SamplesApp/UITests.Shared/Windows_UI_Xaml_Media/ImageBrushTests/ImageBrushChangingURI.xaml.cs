using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.ImageTests.Models;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Uno.UI.Samples.UITests.ImageBrushTestControl
{
	[SampleControlInfo("ImageBrushTestControl", "ImageBrushChangingURI")]
	public sealed partial class ImageBrushChangingURI : UserControl
	{
		public static readonly DependencyProperty TileContentProperty = DependencyProperty.Register(nameof(TileContent), typeof(Uri), typeof(ImageBrushChangingURI), new PropertyMetadata(new Uri("https://cdn.pixabay.com/photo/2020/03/09/17/51/narcis-4916584_960_720.jpg")));

		public Uri TileContent
		{
			get { return (Uri)GetValue(TileContentProperty); }
			set { SetValue(TileContentProperty, value); }
		}

		public ImageBrushChangingURI()
		{
			this.InitializeComponent();

			TileContent = new Uri("ms-appx:///Assets/rect.png");
		}

		private void OnButton1(object sender, RoutedEventArgs e)
		{
			TileContent = new Uri("ms-appx:///Assets/cart.png");
			txtStatus.Text = "Changed";
		}
		private void OnButton2(object sender, RoutedEventArgs e)
		{
			TileContent = new Uri("ms-appx:///Assets/rect.png");
			txtStatus.Text = "Changed";
		}

	}
}
