#pragma warning disable CA1848 // Use LoggerMessage 

using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI.Samples.Controls;
using Windows.Storage;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace UITests.Windows_UI_Xaml_Controls.ImageTests;

[Sample("Image")]
public sealed partial class SvgImageSource_FromMsAppData : Page
{
	private SampleSvgSource _selectedSource;
	private string _imageWidth = "100";
	private string _rasterizedWidth = "";
	private string _imageHeight = "100";
	private string _rasterizedHeight = "";
	private string _selectedStretch = "None";

	public SvgImageSource_FromMsAppData()
	{
		this.InitializeComponent();

		_selectedSource = Sources[0];
		OnPropertyChanged();
	}

	public SampleSvgSource[] Sources { get; } = new SampleSvgSource[]
	{
		new("Temp", new Uri("ms-appx:///Assets/Formats/couch.svg")),
		new("Local", new Uri("ms-appx:///Assets/Formats/czcalendar.svg")),
		new("Roaming", new Uri("ms-appx:///Assets/Formats/heliocentric.svg"))
	};

	public string[] Stretches { get; } = Enum.GetNames<Stretch>();

	public SampleSvgSource SelectedSource
	{
		get => _selectedSource;
		set
		{
			_selectedSource = value;
			OnPropertyChanged();
		}
	}

	public string SelectedStretch
	{
		get => _selectedStretch;
		set
		{
			_selectedStretch = value;
			OnPropertyChanged();
		}
	}

	public string ImageWidth
	{
		get => _imageWidth;
		set
		{
			_imageWidth = value;
			OnPropertyChanged();
		}
	}

	public string ImageHeight
	{
		get => _imageHeight;
		set
		{
			_imageHeight = value;
			OnPropertyChanged();
		}
	}

	public string RasterizedWidth
	{
		get => _rasterizedWidth;
		set
		{
			_rasterizedWidth = value;
			OnPropertyChanged();
		}
	}

	public string RasterizedHeight
	{
		get => _rasterizedHeight;
		set
		{
			_rasterizedHeight = value;
			OnPropertyChanged();
		}
	}

	private async void OnPropertyChanged()
	{
		try
		{
			if (ImageElement.Source is not SvgImageSource svgImageSource)
			{
				svgImageSource = new SvgImageSource();
				ImageElement.Source = svgImageSource;
			}

			if (SelectedSource != null)
			{
				var file = await StorageFile.GetFileFromApplicationUriAsync(SelectedSource.Uri);

				var targetFolder = SelectedSource.Name switch
				{
					"Local" => ApplicationData.Current.LocalFolder,
					"Temp" => ApplicationData.Current.TemporaryFolder,
					"Roaming" => ApplicationData.Current.RoamingFolder,
					_ => throw new NotSupportedException("Unsupported ms-appx target")
				};

				await file.CopyAsync(targetFolder, file.Name, NameCollisionOption.ReplaceExisting);

				svgImageSource.UriSource = new Uri($"ms-appdata:///local/{file.Name}");

				var text = await FileIO.ReadTextAsync(file);
				using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
				await svgImageSource.SetSourceAsync(stream.AsRandomAccessStream());
			}

			if (Enum.TryParse(SelectedStretch, out Stretch stretch))
			{
				ImageElement.Stretch = stretch;
			}

			if (double.TryParse(ImageWidth, out var width))
			{
				ImageElement.Width = width;
			}
			else
			{
				ImageElement.Width = double.NaN;
			}

			if (double.TryParse(ImageHeight, out var height))
			{
				ImageElement.Height = height;
			}
			else
			{
				ImageElement.Height = double.NaN;
			}

			if (double.TryParse(RasterizedWidth, out var rasterizedWidth))
			{
				svgImageSource.RasterizePixelWidth = rasterizedWidth;
			}

			if (double.TryParse(RasterizedHeight, out var rasterizedHeight))
			{
				svgImageSource.RasterizePixelHeight = rasterizedHeight;
			}
		}
		catch (Exception ex)
		{
			this.Log().LogError(ex, "Error while changing SVG properties");
		}
	}
}
