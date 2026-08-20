#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

#if !WINAPPSDK
[TestClass]
[RunsOnUIThread]
public class Given_GenericResources
{
	[TestMethod]
	public void When_Compatibility_Resources_Are_Resolved()
	{
		var keys = new[]
		{
			"ProgressBarBorderThemeThickness",
			"SliderHorizontalThumbHeight",
			"SliderHorizontalThumbWidth",
			"SplitViewPaneRootCornerRadius",
			"SystemAccentColor",
			"SystemAccentColorDark1",
			"SystemAccentColorDark2",
			"SystemAccentColorDark3",
			"SystemAccentColorLight1",
			"SystemAccentColorLight2",
			"SystemAccentColorLight3",
			"SystemColorButtonFaceColor",
			"SystemColorButtonTextColor",
			"SystemColorGrayTextColor",
			"SystemColorHighlightColor",
			"SystemColorHighlightTextColor",
			"SystemColorHotlightColor",
			"SystemColorWindowColor",
			"SystemColorWindowTextColor",
			"XamlDefaultButton",
			"XamlDefaultCheckBox",
			"XamlDefaultFrame",
			"XamlDefaultPivot",
			"XamlDefaultProgressBar",
			"XamlDefaultSlider",
			"XamlDefaultToggleSwitch",
		};

		foreach (var key in keys)
		{
			Assert.IsNotNull(Application.Current!.Resources[key], $"Resource '{key}' was not resolved.");
		}
	}
}
#endif
