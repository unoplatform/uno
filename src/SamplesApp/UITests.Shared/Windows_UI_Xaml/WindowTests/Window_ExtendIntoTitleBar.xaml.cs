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
using Windows.ApplicationModel.Core;
using Windows.UI.Popups;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.WindowTests;

[Sample("Windowing")]
public sealed partial class Window_ExtendIntoTitleBar : Page
{
	public Window_ExtendIntoTitleBar()
	{
		this.InitializeComponent();
		this.Unloaded += Window_ExtendIntoTitleBar_Unloaded;
	}

	private void Window_ExtendIntoTitleBar_Unloaded(object sender, RoutedEventArgs e)
	{
		CoreApplication.GetCurrentView().TitleBar.LayoutMetricsChanged -= TitleBar_LayoutMetricsChanged;
	}

	private async void IsExtended_Click(object sender, RoutedEventArgs e)
	{
		var isExtended = CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar;
		var messageDialog = new MessageDialog("IsExtended: " + isExtended);
		await messageDialog.ShowAsync();
	}

	private void GetMetrics_Click(object sender, RoutedEventArgs e) => UpdateMetrics();

	private void UpdateMetrics()
	{
		var titleBar = CoreApplication.GetCurrentView().TitleBar;
		LeftInsetRun.Text = titleBar.SystemOverlayLeftInset.ToString();
		RightInsetRun.Text = titleBar.SystemOverlayRightInset.ToString();
		HeightRun.Text = titleBar.Height.ToString();
	}

	private void OnLayoutMetricsToggled(object sender, RoutedEventArgs e)
	{
		CoreApplication.GetCurrentView().TitleBar.LayoutMetricsChanged -= TitleBar_LayoutMetricsChanged;
		if (LayoutMetricsToggleButton.IsChecked == true)
		{
			CoreApplication.GetCurrentView().TitleBar.LayoutMetricsChanged += TitleBar_LayoutMetricsChanged;
		}
	}

	private void TitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar titleBar, object args) => UpdateMetrics();

	private void SetTitleBar_Click(object sender, RoutedEventArgs e)
	{
		var rootFrame = (Frame)Window.Current.Content;
		var mainPage = (Page)rootFrame.Content;
		var sampleChooser = (SampleChooserControl)mainPage.Content;
		Window.Current.SetTitleBar(sampleChooser.TitleBarElement);
	}
}
