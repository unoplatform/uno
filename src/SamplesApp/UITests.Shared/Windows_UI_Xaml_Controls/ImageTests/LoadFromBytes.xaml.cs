using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.UITests.ImageTestsControl
{
	[Sample("Image", Name = "LoadFromBytes")]
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
