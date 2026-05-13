using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

partial class NavigationView
{
#if HAS_UNO // Uno workaround (#4727): the PaneHeaderContentBorderWrapper template part has no WinUI counterpart and is needed on Skia because Skia uses the Uno NavigationView template.
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
#endif

#if !__SKIA__ // Uno workaround: ThemeShadow support check; Skia always supports ThemeShadow (WinUI applies it unconditionally).
	private bool IsThemeShadowSupported() => ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.ThemeShadow, Uno.UI");
#endif
}
