using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI.Samples.Controls;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace UITests.Windows_UI_Xaml_Controls.ImageTests;

[Sample("Image")]
public sealed partial class SvgImageSource_Basic : Page
{
	private SampleSvgSource _selectedSource;
	private string _imageWidth = "100";
	private string _rasterizedWidth = "";
	private string _imageHeight = "100";
	private string _rasterizedHeight = "";
	private string _selectedStretch = "None";

	public SvgImageSource_Basic()
	{
		this.InitializeComponent();

		_selectedSource = Sources[0];
		OnPropertyChanged();

		this.Loaded += SvgImageSource_Basic_Loaded;
	}

	private async void SvgImageSource_Basic_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
	{
		await SvgImageSourceHelpers.CopySourcesToAppDataAsync();
	}

	public SampleSvgSource[] Sources { get; } = new SampleSvgSource[]
	{
		new("Couch (ms-appx library)", new Uri("ms-appx:///Uno.UI.RuntimeTests/Assets/couch.svg")),
		new("Couch (ms-appx)", new Uri("ms-appx:///Assets/Formats/couch.svg")),
		new("Calendar (ms-appx)", new Uri("ms-appx:///Assets/Formats/czcalendar.svg")),
		new("Home (ms-appx)", new Uri("ms-appx:///Assets/Formats/home.svg")),
		new("Heliocentric (relative)", new Uri("/Assets/Formats/heliocentric.svg", UriKind.Relative)),
		new("Heart (relative)", new Uri("/Assets/Formats/heart.svg", UriKind.Relative)),
		new("Chef (app-data)", new Uri("ms-appdata:///Local/svg/chef.svg")),
		new("Bookstack (app-data)", new Uri("ms-appdata:///Local/svg/bookstack.svg")),
		new("Apple (web)", new Uri("https://raw.githubusercontent.com/unoplatform/uno/56069e83325786e0a652fdedfda7bbd9f0cee224/src/SamplesApp/UITests.Shared/Assets/Formats/apple.svg")),
		new("Road crossing (web)", new Uri("https://raw.githubusercontent.com/unoplatform/uno/56069e83325786e0a652fdedfda7bbd9f0cee224/src/SamplesApp/UITests.Shared/Assets/Formats/roadcrossing.svg"))
	};

	public string[] Stretches { get; } = Enum.GetNames(typeof(Stretch)).ToArray();

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

	private void OnPropertyChanged()
	{
		if (ImageElement.Source is not SvgImageSource svgImageSource)
		{
			svgImageSource = new SvgImageSource();
			ImageElement.Source = svgImageSource;
		}

		if (svgImageSource.UriSource is null || svgImageSource.UriSource != SelectedSource.Uri)
		{
			svgImageSource.UriSource = SelectedSource.Uri;
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
		else
		{
#if HAS_UNO
			svgImageSource.RasterizePixelWidth = double.NaN;
#endif
		}

		if (double.TryParse(RasterizedHeight, out var rasterizedHeight))
		{
			svgImageSource.RasterizePixelHeight = rasterizedHeight;
		}
		else
		{
#if HAS_UNO
			svgImageSource.RasterizePixelHeight = double.NaN;
#endif
		}
	}
}
