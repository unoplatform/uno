using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace WindowingSamples;

/// <summary>
/// Window demonstrating ExtendsContentIntoTitleBar API.
/// This API allows extending XAML UI into the title bar area while preserving caption buttons.
/// </summary>
public sealed partial class ExtendContentIntoTitleBarWindow : Window
{
	public ExtendContentIntoTitleBarWindow(TitleBarHeightOption height)
	{
		InitializeComponent();

		Title = "Extend Content Into Title Bar";

		// Enable extending content into title bar
		AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
		AppWindow.TitleBar.PreferredHeightOption = height;
	}

	public void CloseClick(object sender, RoutedEventArgs args) => Close();
}
