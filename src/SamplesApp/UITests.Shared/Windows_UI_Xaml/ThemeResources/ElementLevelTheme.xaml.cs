using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml.ThemeResources
{
	[Sample("XAML", Name = nameof(ElementLevelTheme), Description = "Element-level RequestedTheme with nested theme overrides", IsManualTest = true)]
	public sealed partial class ElementLevelTheme : Page
	{
		public ElementLevelTheme()
		{
			this.InitializeComponent();
		}

		private void PageThemeSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
		{
			if (sender.SelectedItem == PageThemeLightItem)
			{
				SetPageTheme(ElementTheme.Light);
			}
			else if (sender.SelectedItem == PageThemeDarkItem)
			{
				SetPageTheme(ElementTheme.Dark);
			}
			else
			{
				SetPageTheme(ElementTheme.Default);
			}
		}

		private void SetPageTheme(ElementTheme theme)
		{
			this.RequestedTheme = theme;

			// Update the Default column's nested card to be the opposite of the resolved page theme
			var opposite = ActualTheme == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark;
			DefaultColumnNestedBorder.RequestedTheme = opposite;
			DefaultColumnNestedLabel.Text = opposite == ElementTheme.Dark
				? "Nested Opposite (Dark)"
				: "Nested Opposite (Light)";
			DefaultColumnIcon.Text = ActualTheme == ElementTheme.Dark
				? "Default Theme (Dark)"
				: "Default Theme (Light)";
		}

		private void ThemeToggle_Toggled(object sender, RoutedEventArgs e)
		{
			if (DynamicThemeCard is null)
			{
				return;
			}

			var useDark = ThemeToggle.IsOn;
			DynamicThemeCard.RequestedTheme = useDark ? ElementTheme.Dark : ElementTheme.Light;
			DynamicThemeLabel.Text = useDark ? "Currently: Dark" : "Currently: Light";
		}
	}
}
