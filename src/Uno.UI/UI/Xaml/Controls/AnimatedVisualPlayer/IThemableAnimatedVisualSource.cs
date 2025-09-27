#nullable enable

using Windows.UI;

namespace Microsoft.UI.Xaml.Controls
{
	public partial interface IThemableAnimatedVisualSource : IAnimatedVisualSource
	{
		void SetColorThemeProperty(string propertyName, Color? color);
		Color? GetColorThemeProperty(string propertyName);
	}
}
