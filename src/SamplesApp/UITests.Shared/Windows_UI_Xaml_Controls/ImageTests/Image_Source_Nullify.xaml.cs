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
using Uno.UI.Samples.UITests.Helpers;

namespace UITests.Windows_UI_Xaml_Controls.ImageTests;

[Sample("Image", ViewModelType = typeof(ImageSourceNullifyViewModel))]
public sealed partial class Image_Source_Nullify : Page
{
	public Image_Source_Nullify()
	{
		this.InitializeComponent();
	}
}

public class ImageSourceNullifyViewModel : ViewModelBase
{
	private ICommand clearImageCommand;

	private ICommand loadImageCommand;

	private ImageSource testImageSource;

	public ImageSourceNullifyViewModel(Dispatcher )
	{
		this.LoadImageCommand = new RelayCommand(async () =>
		{
			this.TestImageSource = new BitmapImage(new Uri("ms-appx:///Assets/ingredient1.png"));
		});

		this.ClearImageCommand = new DelegateCommand(() =>
		{
			this.TestImageSource = null;
		});
	}

	/// <summary> Gets or sets the clear image command. </summary>
	/// <value> The clear image command. </value>
	public ICommand ClearImageCommand
	{
		get => this.clearImageCommand;
		private set => this.SetProperty(ref this.clearImageCommand, value);
	}

	/// <summary> Gets or sets the load image command. </summary>
	/// <value> The load image command. </value>
	public ICommand LoadImageCommand
	{
		get => this.loadImageCommand;
		private set => this.SetProperty(ref this.loadImageCommand, value);
	}

	/// <summary> Gets or sets the test image source. </summary>
	/// <value> The test image source. </value>
	public ImageSource TestImageSource
	{
		get => this.testImageSource;
		private set => this.SetProperty(ref this.testImageSource, value);
	}
}
