using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using SampleControl.Presentation;
using SamplesApp;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;


namespace UITests.Microsoft_UI_Windowing;

[Sample("Windowing", IsManualTest = true, ViewModelType = typeof(AppWindowFeaturesViewModel),
	Description =
		"Clicking the button should change the app icon on Win32 Desktop")]
public sealed partial class AppWindowFeatures : Page
{
	public AppWindowFeatures()
	{
		this.InitializeComponent();
		DataContextChanged += AppWindowFeatures_DataContextChanged;
	}

	internal AppWindowFeaturesViewModel ViewModel { get; private set; }

	private void AppWindowFeatures_DataContextChanged(Microsoft.UI.Xaml.FrameworkElement sender, Microsoft.UI.Xaml.DataContextChangedEventArgs args)
	{
		ViewModel = args.NewValue as AppWindowFeaturesViewModel;
	}
}

internal class AppWindowFeaturesViewModel : ViewModelBase
{
	public AppWindowFeaturesViewModel()
	{
	}

	public async void SetIcon()
	{
		var iconUri = new Uri("ms-appx:///Assets/bluecrystal.ico");
		var file = await StorageFile.GetFileFromApplicationUriAsync(iconUri);
		App.MainWindow.AppWindow.SetIcon(file.Path);
	}
}
