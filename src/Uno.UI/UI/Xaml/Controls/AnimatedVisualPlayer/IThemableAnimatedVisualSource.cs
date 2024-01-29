#nullable enable
namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public partial interface IThemableAnimatedVisualSource : IAnimatedVisualSource
	{
		void SetColorThemeProperty(string propertyName, Windows.UI.Color? color);
		Windows.UI.Color? GetColorThemeProperty(string propertyName);
	}
}
