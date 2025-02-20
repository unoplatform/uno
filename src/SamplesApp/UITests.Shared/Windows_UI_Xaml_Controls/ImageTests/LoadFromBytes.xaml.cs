using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.UITests.ImageTestsControl
{
	[SampleControlInfo("Image", "LoadFromBytes")]
	public sealed partial class LoadFromBytes : UserControl
	{
		public LoadFromBytes()
		{
			InitializeComponent();
			Loaded += OnLoaded;
		}

		private async void OnLoaded(object sender, RoutedEventArgs e)
		{
			var assembly = typeof(LoadFromBytes).Assembly;

			var query = from name in assembly.GetManifestResourceNames()
						where name.EndsWith("mslug.png")
						select assembly.GetManifestResourceStream(name);

			using (var stream = query.First())
			{
				MyImage.Source = await CreateImageSource(stream);
			}
		}

		private async Task<ImageSource> CreateImageSource(Stream stream)
		{
			if (stream == null)
			{
				throw new ArgumentNullException(nameof(stream));
			}

			var image = new BitmapImage();
			await image.SetSourceAsync(stream.AsRandomAccessStream()).AsTask();
			return image;
		}
	}
}
