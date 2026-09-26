using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.RichTextBlockTests;

[Sample("RichTextBlock", Name = "RichTextBlock_Theming", Description = "Verifies Foreground inheritance/override and SelectionHighlightColor react correctly to a theme change. Toggle the theme and check the manual test steps at the top.", IsManualTest = true)]
public sealed partial class RichTextBlock_Theming : Page
{
	public RichTextBlock_Theming()
	{
		this.InitializeComponent();
		UpdateCurrentThemeDisplay();
	}

	private void OnToggleThemeClick(object sender, RoutedEventArgs e)
	{
		// Flip the page's own RequestedTheme rather than the app/window theme,
		// so this is a pure element-level theming test.
		RequestedTheme = ActualTheme == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark;
		UpdateCurrentThemeDisplay();
	}

	private void UpdateCurrentThemeDisplay() => CurrentThemeDisplay.Text = $"Current theme: {ActualTheme}";
}
