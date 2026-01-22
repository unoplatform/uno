using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Media.Animation;

[Sample("Animation", Name = "NavigationTransitionSample", Description = "Demonstrates various NavigationTransitionInfo types", IgnoreInSnapshotTests = true, IsManualTest = true)]
public sealed partial class NavigationTransitionSample : Page
{
	public NavigationTransitionSample()
	{
		this.InitializeComponent();
	}

	private void OnGoBackClick(object sender, RoutedEventArgs e)
	{
		if (ContentFrame.CanGoBack)
		{
			ContentFrame.GoBack(GetSelectedTransitionInfo());
		}
	}

	private void OnNavigateToPage1Click(object sender, RoutedEventArgs e)
	{
		ContentFrame.Navigate(typeof(NavigationTransitionPage1), null, GetSelectedTransitionInfo());
	}

	private void OnNavigateToPage2Click(object sender, RoutedEventArgs e)
	{
		ContentFrame.Navigate(typeof(NavigationTransitionPage2), null, GetSelectedTransitionInfo());
	}

	private void OnNavigateToPage3Click(object sender, RoutedEventArgs e)
	{
		ContentFrame.Navigate(typeof(NavigationTransitionPage3), null, GetSelectedTransitionInfo());
	}

	private void OnFrameNavigated(object sender, NavigationEventArgs e)
	{
		BackButton.IsEnabled = ContentFrame.CanGoBack;
	}

	private NavigationTransitionInfo GetSelectedTransitionInfo()
	{
		var selectedItem = TransitionSelector.SelectedItem as ComboBoxItem;
		var tag = selectedItem?.Tag?.ToString() ?? "DrillIn";

		return tag switch
		{
			"DrillIn" => new DrillInNavigationTransitionInfo(),
			"SlideFromBottom" => new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromBottom },
			"SlideFromLeft" => new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromLeft },
			"SlideFromRight" => new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight },
			"Entrance" => new EntranceNavigationTransitionInfo(),
			"Common" => new CommonNavigationTransitionInfo(),
			"Continuum" => new ContinuumNavigationTransitionInfo(),
			"Suppress" => new SuppressNavigationTransitionInfo(),
			_ => new DrillInNavigationTransitionInfo()
		};
	}
}
