#nullable enable

using Windows.UI;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public partial interface IThemableAnimatedVisualSource : IAnimatedVisualSource
	{
		void SetColorThemeProperty(string propertyName, Color? color);
		Color? GetColorThemeProperty(string propertyName);
	}
}
