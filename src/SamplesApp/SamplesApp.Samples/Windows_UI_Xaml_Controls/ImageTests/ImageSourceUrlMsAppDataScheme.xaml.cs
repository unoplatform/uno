using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace UITests.Windows_UI_Xaml_Controls.ImageTests
{
	[Sample("Image", ViewModelType = typeof(ImageSourceUrlMsAppDataSchemeViewModel))]
	internal sealed partial class ImageSourceUrlMsAppDataScheme : Page
	{
		public ImageSourceUrlMsAppDataScheme()
		{
			this.InitializeComponent();
			this.DataContextChanged += ImageSourceUrlMsAppDataScheme_DataContextChanged;
		}

		private async void ImageSourceUrlMsAppDataScheme_DataContextChanged(DependencyObject sender, DataContextChangedEventArgs args)
		{
			if (DataContext is ImageSourceUrlMsAppDataSchemeViewModel viewModel)
			{
				await viewModel.LoadAsync();
			}
		}
	}

	internal class ImageSourceUrlMsAppDataSchemeViewModel : ViewModelBase
	{
		public ImageSourceUrlMsAppDataSchemeViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher)
			: base(dispatcher)
		{
		}

		public Uri AppDataUri { get; private set; }

		public async Task LoadAsync()
		{
			// copy image for app package to app data
			await CopySampleImageToAppDataAsync();

			// set uri after image is ready in app data
			AppDataUri = new Uri("ms-appdata:///Local/ImageAppDataUriSample/MsAppDataUriTest.png");
			RaisePropertyChanged(nameof(AppDataUri));
		}

		private async Task CopySampleImageToAppDataAsync()
		{
			var targetPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "ImageAppDataUriSample", "MsAppDataUriTest.png");
			if (!File.Exists(targetPath))
			{
				var assembly = Assembly.GetExecutingAssembly();
				var resourceName =
					assembly
						.GetManifestResourceNames()
						.First(n => n.IndexOf("MsAppDataUriTest.png", StringComparison.InvariantCultureIgnoreCase) >= 0);

				using (var stream = assembly.GetManifestResourceStream(resourceName))
				{
					Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
					using (var targetStream = File.Create(targetPath))
					{
						await stream.CopyToAsync(targetStream);
					}
				}
			}
		}
	}
}
