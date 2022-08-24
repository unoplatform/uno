using System;
using System.Linq;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace UITests.Windows_UI_Xaml_Controls.ImageTests;

[Sample("Image")]
public sealed partial class SvgImageSource_Basic : Page
{
	private string _selectedSource = "uno-overalls.svg";
	private string _imageWidth = "100";
	private string _rasterizedWidth = "";
	private string _imageHeight = "100";
	private string _rasterizedHeight = "";
	private string _selectedStretch = "None";

	public SvgImageSource_Basic()
	{
		this.InitializeComponent();
		OnPropertyChanged();
	}

	public string[] Sources { get; } = new string[]
	{
		"uno-overalls.svg",
		"couch.svg",
		"heliocentric.svg",
		"heart.svg",
	};

	public string[] Stretches { get; } = Enum.GetNames(typeof(Stretch)).ToArray();

	public string SelectedSource
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

		if (svgImageSource.UriSource is null || !svgImageSource.UriSource.ToString().EndsWith(SelectedSource))
		{
			svgImageSource.UriSource = new Uri($"ms-appx:///Assets/Formats/{SelectedSource}");
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
			//svgImageSource.RasterizePixelWidth = double.PositiveInfinity;
		}

		if (double.TryParse(RasterizedHeight, out var rasterizedHeight))
		{
			svgImageSource.RasterizePixelHeight = rasterizedHeight;
		}
		else
		{
			//svgImageSource.RasterizePixelHeight = double.PositiveInfinity;
		}
	}
}
