using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Media
{
	internal static partial class ThemeShadowManager
	{
		static partial void UnsetShadow(UIElement uiElement) => uiElement.Visual.HasThemeShadow = false;

		static partial void SetShadow(UIElement uiElement) => uiElement.Visual.HasThemeShadow = true;
	}
}
