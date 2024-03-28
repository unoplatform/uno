using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace UITests.Windows_UI_Xaml_Controls.ImageTests;

[Sample("Image")]
public sealed partial class SvgImageSource_MultiLoad : Page
{
	private const int Rows = 4;
	private const int Columns = 4;

	private SampleSvgSource _selectedSource;
	private bool _reuseImageSource;

	public SvgImageSource_MultiLoad()
	{
		InitializeComponent();

		CreateImages();

		this.Loaded += SvgImageSource_Basic_Loaded;
	}

	private async void SvgImageSource_Basic_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
	{
		await SvgImageSourceHelpers.CopySourcesToAppDataAsync();
	}

	public SampleSvgSource[] Sources { get; } = new SampleSvgSource[]
	{
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

	public SampleSvgSource SelectedSource
	{
		get => _selectedSource;
		set
		{
			_selectedSource = value;
			OnPropertyChanged();
		}
	}

	public bool ReuseImageSource
	{
		get => _reuseImageSource;
		set
		{
			_reuseImageSource = value;
			OnPropertyChanged();
		}
	}

	private void CreateImages()
	{
		for (int row = 0; row < Rows; row++)
		{
			ImageContainer.RowDefinitions.Add(new RowDefinition() { Height = GridLengthHelper2.FromValueAndType(1, GridUnitType.Star) });
		}

		for (int column = 0; column < Columns; column++)
		{
			ImageContainer.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLengthHelper2.FromValueAndType(1, GridUnitType.Star) });
		}

		for (int row = 0; row < Rows; row++)
		{
			for (int column = 0; column < Columns; column++)
			{
				var image = new Image();
				image.Stretch = Stretch.Uniform;
				image.HorizontalAlignment = HorizontalAlignment.Stretch;
				image.VerticalAlignment = VerticalAlignment.Stretch;
				image.SetValue(Grid.RowProperty, row);
				image.SetValue(Grid.ColumnProperty, column);
				ImageContainer.Children.Add(image);
			}
		}
	}

	private void OnPropertyChanged()
	{
		SvgImageSource imageSource = null;

		foreach (Image image in ImageContainer.Children)
		{
			if (ReuseImageSource)
			{
				imageSource ??= CreateImageSource();
			}
			else
			{
				imageSource = CreateImageSource();
			}

			image.Source = imageSource;
		}
	}

	private SvgImageSource CreateImageSource()
	{
		var svgImageSource = new SvgImageSource();

		if (svgImageSource.UriSource is null || svgImageSource.UriSource != SelectedSource.Uri)
		{
			svgImageSource.UriSource = SelectedSource.Uri;
		}

		return svgImageSource;
	}
}
