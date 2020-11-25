namespace Microsoft.UI.Xaml.Controls
{
	internal enum NavigationViewVisualStateDisplayMode
	{
		Compact,
		Expanded,
		Minimal,
		MinimalWithBackButton
	}

	internal enum NavigationViewRepeaterPosition
	{
		LeftNav,
		TopPrimary,
		TopOverflow,
		LeftFooter,
		TopFooter
	}

	public enum NavigationViewPropagateTarget
	{
		LeftListView,
		TopListView,
		OverflowListView,
		All
	}

	// TODO:
	internal class NavigationViewItemHelper
	{
		internal const string c_OnLeftNavigationReveal = "OnLeftNavigationReveal";
		internal const string c_OnLeftNavigation = "OnLeftNavigation";
		internal const string c_OnTopNavigationPrimary = "OnTopNavigationPrimary";
		internal const string c_OnTopNavigationPrimaryReveal = "OnTopNavigationPrimaryReveal";
		internal const string c_OnTopNavigationOverflow = "OnTopNavigationOverflow";
	}
}
