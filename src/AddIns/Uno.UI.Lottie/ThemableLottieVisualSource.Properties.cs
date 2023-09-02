using System.Collections.Generic;
using Windows.UI;

#if HAS_UNO_WINUI
namespace CommunityToolkit.WinUI.Lottie
#else
namespace Microsoft.Toolkit.Uwp.UI.Lottie
#endif
{
	partial class ThemableLottieVisualSource
	{
		private readonly Dictionary<string, ColorBinding> _colorsBindings
			= new Dictionary<string, ColorBinding>(2);

		public void SetColorThemeProperty(string propertyName, Color? color)
		{
			if (_colorsBindings.TryGetValue(propertyName, out var existing))
			{
				existing.NextValue = color;
			}
			else
			{
				_colorsBindings[propertyName] = new ColorBinding { NextValue = color };
			}

			if (_currentDocument == null)
			{
				return; // no document to change yet
			}

			if (ApplyProperties())
			{
				NotifyCallback();
			}
		}

		public Color? GetColorThemeProperty(string propertyName)
		{
			if (_colorsBindings.TryGetValue(propertyName, out var existing))
			{
				return existing.NextValue ?? existing.CurrentValue;
			}

			return default;
		}

	}
}
