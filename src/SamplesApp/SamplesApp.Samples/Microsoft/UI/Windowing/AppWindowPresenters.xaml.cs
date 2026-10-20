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


namespace UITests.Microsoft_UI_Windowing;

[Sample("Windowing", IsManualTest = true, ViewModelType = typeof(AppWindowPresentersViewModel),
	Description =
		"Clicking the buttons should change the window presenter and the mode of the window." +
		"CompactOverlay is not yet implemented in Uno Platform, so it will show an error instead.")]
public sealed partial class AppWindowPresenters : Page
{
	public AppWindowPresenters()
	{
		this.InitializeComponent();
		DataContextChanged += AppWindowPresenters_DataContextChanged;
	}

	internal AppWindowPresentersViewModel ViewModel { get; private set; }

	private void AppWindowPresenters_DataContextChanged(Microsoft.UI.Xaml.FrameworkElement sender, Microsoft.UI.Xaml.DataContextChangedEventArgs args)
	{
		ViewModel = args.NewValue as AppWindowPresentersViewModel;
	}
}

internal class AppWindowPresentersViewModel : ViewModelBase
{
	public AppWindowPresentersViewModel()
	{
	}

	public string CurrentPresenter => App.MainWindow.AppWindow.Presenter.GetType().Name;

	public void FullScreen() => SetPresenter(AppWindowPresenterKind.FullScreen);

	public void CompactOverlay() => SetPresenter(AppWindowPresenterKind.CompactOverlay);

	public void Overlapped() => SetPresenter(AppWindowPresenterKind.Overlapped);

	public void Default() => SetPresenter(AppWindowPresenterKind.Default);

	private async void SetPresenter(AppWindowPresenterKind kind)
	{
		try
		{
			App.MainWindow.AppWindow.SetPresenter(kind);
			RaisePropertyChanged(nameof(CurrentPresenter));
		}
		catch (Exception ex)
		{
			await new ContentDialog
			{
				Title = "Error",
				XamlRoot = SampleChooserViewModel.Instance.Owner.XamlRoot,
				Content = ex.Message,
				CloseButtonText = "OK"
			}.ShowAsync();
		}
	}
}
