using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

partial class NavigationView
{
#if !UNO_HAS_ENHANCED_LIFECYCLE
	// Native Android/iOS only: ElementPrepared fires after OnApplyTemplate there (no enhanced lifecycle),
	// so items are prepared early via this repeater-specific event.
	private void OnRepeaterUnoBeforeElementPrepared(ItemsRepeater itemsRepeater, ItemsRepeaterElementPreparedEventArgs args) =>
		OnRepeaterElementPrepared(itemsRepeater, args);
#endif

#if HAS_UNO // Uno: workaround template part + helpers with no WinUI counterpart
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
#endif
}
