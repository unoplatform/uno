using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

partial class NavigationView
{
	//TODO: Uno specific - remove when #4689 is fixed

	private void OnRepeaterUnoBeforeElementPrepared(ItemsRepeater itemsRepeater, ItemsRepeaterElementPreparedEventArgs args) =>
		OnRepeaterElementPrepared(itemsRepeater, args);

	//TODO: Uno specific - remove when #4727 is fixed

	private Grid m_paneHeaderContentBorderWrapper;

	private void SetHeaderContentMinHeight(double minHeight)
	{
		m_paneHeaderContentBorderWrapper ??= GetTemplateChild("PaneHeaderContentBorderWrapper") as Grid;
		if (m_paneHeaderContentBorderWrapper != null)
		{
			m_paneHeaderContentBorderWrapper.MinHeight = minHeight;
		}
	}

	private bool IsThemeShadowSupported() => ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.ThemeShadow, Uno.UI");
}
