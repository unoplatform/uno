using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Media
{
	internal static partial class ThemeShadowManager
	{
		internal static void UpdateShadow(UIElement uiElement)
		{
			if (uiElement.Shadow == null || uiElement.Translation.Z <= 0)
			{
				UnsetShadow(uiElement);
			}
			else
			{
				SetShadow(uiElement);
			}
		}
	}
}
