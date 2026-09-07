#nullable enable

using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
			"XamlDefaultComboBox",
			"XamlDefaultFrame",
			"XamlDefaultPasswordBox",
			"XamlDefaultPivot",
			"XamlDefaultProgressBar",
			"XamlDefaultRadioButton",
			"XamlDefaultRepeatButton",
			"XamlDefaultSlider",
			"XamlDefaultTextBox",
			"XamlDefaultToggleButton",
			"XamlDefaultToggleSwitch",
		};

		foreach (var key in keys)
		{
			Assert.IsNotNull(Application.Current!.Resources[key], $"Resource '{key}' was not resolved.");
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public void When_ScrollViewer_Compatibility_Style_Is_Applied()
	{
		var defaultStyle = Application.Current!.Resources["DefaultScrollViewerStyle"] as Style;
		var implicitStyle = Application.Current.Resources[typeof(ScrollViewer)] as Style;

		Assert.IsNotNull(defaultStyle);
		Assert.IsNotNull(implicitStyle);
		Assert.AreSame(defaultStyle, implicitStyle.BasedOn);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public void When_WebView2_Default_Template_Hosts_Native_View()
	{
		// CoreWebView2.GetNativeWebViewFromTemplate only attaches a native view when the first
		// child is a ContentPresenter named WebViewTemplateRoot; WinUI's generic.xaml has no
		// WebView2 style, so Uno has to supply this one.
		var style = Application.Current!.Resources[typeof(WebView2)] as Style;
		Assert.IsNotNull(style);

		var template = style.Setters
			.OfType<Setter>()
			.FirstOrDefault(setter => setter.Property == Control.TemplateProperty)?
			.Value as ControlTemplate;
		Assert.IsNotNull(template);

		var root = template.LoadContent() as ContentPresenter;
		Assert.IsNotNull(root);
		Assert.AreEqual("WebViewTemplateRoot", root.Name);
	}
}
#endif
