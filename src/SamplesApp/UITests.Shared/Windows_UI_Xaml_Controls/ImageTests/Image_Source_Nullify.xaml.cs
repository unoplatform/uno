using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Uno.UI.Samples.Controls;
using System.Windows.Input;
using Uno.UI.Common;
using Windows.UI.Core;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Xaml.Media.Imaging;

namespace UITests.Windows_UI_Xaml_Controls.ImageTests;

[Sample("Image", ViewModelType = typeof(ImageSourceNullifyViewModel))]
public sealed partial class Image_Source_Nullify : Page
{
	public Image_Source_Nullify()
	{
		this.InitializeComponent();
		DataContextChanged += Image_Source_Nullify_DataContextChanged;
	}

	private void Image_Source_Nullify_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
	{
		ViewModel = (ImageSourceNullifyViewModel)args.NewValue;
	}

	internal ImageSourceNullifyViewModel ViewModel { get; set; }
}

internal class ImageSourceNullifyViewModel : ViewModelBase
{
	private ImageSource _testImageSource;

	public ImageSourceNullifyViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
	{
		LoadImageCommand = new DelegateCommand(() =>
		{
			TestImageSource = new BitmapImage(new Uri("ms-appx:///Assets/square100.png"));
		});

		ClearImageCommand = new DelegateCommand(() =>
		{
			TestImageSource = null;
		});
	}

	/// <summary> Gets or sets the clear image command. </summary>
	/// <value> The clear image command. </value>
	public ICommand ClearImageCommand { get; }

	/// <summary> Gets or sets the load image command. </summary>
	/// <value> The load image command. </value>
	public ICommand LoadImageCommand { get; }

	/// <summary> Gets or sets the test image source. </summary>
	/// <value> The test image source. </value>
	public ImageSource TestImageSource
	{
		get => _testImageSource;
		private set
		{
			_testImageSource = value;
			RaisePropertyChanged();
		}
	}
}
