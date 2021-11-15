using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Media
{
	internal static partial class ThemeShadowManager
	{
		static partial void UnsetShadow(UIElement uiElement)
		{
			uiElement.SetStyle("box-shadow", "unset");
		}

		static partial void SetShadow(UIElement uiElement)
		{
			var translation = uiElement.Translation;
			var boxShadowValue = CreateBoxShadow(translation.Z);
			uiElement.SetStyle("box-shadow", boxShadowValue);
		}

		private static string CreateBoxShadow(float translationZ)
		{
			var z = (int)translationZ;
			var halfZ = z / 2;
			var quarterZ = z / 4;
			return $"{quarterZ}px {quarterZ}px {halfZ}px 0px rgba(0,0,0,0.3)";
		} 
	}
}
