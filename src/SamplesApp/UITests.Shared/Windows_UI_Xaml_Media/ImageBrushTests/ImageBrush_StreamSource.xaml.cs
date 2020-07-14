using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Helper;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace Uno.UI.Samples.UITests.ImageBrushTestControl
{
	[SampleControlInfo("ImageBrushTestControl", "ImageBrush_StreamSource")]
	public sealed partial class ImageBrush_StreamSource : UserControl
	{
		private readonly ImageBrush_StreamSource_Data _context;

		public ImageBrush_StreamSource()
		{
			this.InitializeComponent();

			DataContext = _context = new ImageBrush_StreamSource_Data();

			this.RunWhileLoaded(async ct =>
			{
				var data = await new HttpClient().GetByteArrayAsync("http://www.google.com/logos/2006/worldcup06_pt.gif");

				var bitmapImage = new BitmapImage();

#if NETFX_CORE
				var stream = new MemoryStream(data).AsRandomAccessStream();
#else
				var stream = new MemoryStream(data);
#endif
				using (stream)  //Test that SetSourceAsync does not depend on the lifetime of this stream
				{
					await bitmapImage.SetSourceAsync(stream);
					_context.MySource = bitmapImage;
				}
			});
		}
	}

	public partial class ImageBrush_StreamSource_Data : DependencyObject
	{
		public ImageBrush_StreamSource_Data()
		{
		}

#region MySource DependencyProperty

		public ImageSource MySource
		{
			get { return (ImageSource)this.GetValue(MySourceProperty); }
			set { this.SetValue(MySourceProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MySource.  This enables animation, styling, binding, etc...
		public static DependencyProperty MySourceProperty { get ; } =
			DependencyProperty.Register("MySource", typeof(ImageSource), typeof(ImageBrush_StreamSource_Data), new FrameworkPropertyMetadata(null));

#endregion

	}
}
