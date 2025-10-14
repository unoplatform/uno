using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using SamplesApp;

namespace UITests.Microsoft_UI_Windowing;

[Sample("Windowing", Name = "TitleBar and Border", IsManualTest = true, Description = "Toggle Window/AppWindow TitleBar extension and apply OverlappedPresenter.SetBorderAndTitleBar.")]
public sealed partial class TitleBarAndBorder : Page
{
	private OverlappedPresenter _presenter = OverlappedPresenter.Create();

	public TitleBarAndBorder()
	{
		InitializeComponent();

		// Initialize UI from current window state
		WindowExtends.IsChecked = App.MainWindow.ExtendsContentIntoTitleBar;
		AppWindowExtends.IsChecked = App.MainWindow.AppWindow.TitleBar.ExtendsContentIntoTitleBar;
		PresenterNameText.Text = $"Presenter: {_presenter.GetType().Name}";
	}

	private void OnWindowExtendsChecked(object sender, RoutedEventArgs e) => App.MainWindow.ExtendsContentIntoTitleBar = true;
	private void OnWindowExtendsUnchecked(object sender, RoutedEventArgs e) => App.MainWindow.ExtendsContentIntoTitleBar = false;

	private void OnAppWindowExtendsChecked(object sender, RoutedEventArgs e) => App.MainWindow.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
	private void OnAppWindowExtendsUnchecked(object sender, RoutedEventArgs e) => App.MainWindow.AppWindow.TitleBar.ExtendsContentIntoTitleBar = false;

	private async void OnApplySetBorderAndTitleBar(object sender, RoutedEventArgs e)
	{
		try
		{
			_presenter.SetBorderAndTitleBar(HasBorderCheck.IsChecked == true, HasTitleBarCheck.IsChecked == true);
			App.MainWindow.AppWindow.SetPresenter(_presenter);
			PresenterNameText.Text = $"Presenter: {_presenter.GetType().Name} (HasBorder={_presenter.HasBorder}, HasTitleBar={_presenter.HasTitleBar})";
		}
		catch (Exception ex)
		{
			await new ContentDialog
			{
				Title = "Error",
				Content = ex.Message,
				CloseButtonText = "OK",
				XamlRoot = this.XamlRoot,
			}.ShowAsync();
		}
	}

	private void OnResetPresenter(object sender, RoutedEventArgs e)
	{
		_presenter = OverlappedPresenter.Create();
		App.MainWindow.AppWindow.SetPresenter(_presenter);
		PresenterNameText.Text = $"Presenter: {_presenter.GetType().Name}";
	}
}
