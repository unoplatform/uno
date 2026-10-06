#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

#if HAS_UNO
[TestClass]
[RunsOnUIThread]
public class Given_GenericResources
{
	[TestCleanup]
	public void Cleanup()
	{
		TestServices.WindowHelper.WindowContent = null;
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/12839")]
	public void When_Compatibility_Resources_Are_Resolved()
	{
		var keys = new[]
		{
			"ProgressBarBorderThemeThickness",
			"SliderHorizontalThumbHeight",
			"SliderHorizontalThumbWidth",
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
		};

		foreach (var key in keys)
		{
			Assert.IsNotNull(Application.Current!.Resources[key], $"Resource '{key}' was not resolved.");
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/12839")]
	public void When_Overlay_Style_Is_Resolved()
	{
		foreach (var type in new[] { typeof(GridView), typeof(ItemsControl), typeof(WebView), typeof(WebView2) })
		{
			Assert.IsInstanceOfType<Style>(Application.Current!.Resources[type], $"Implicit style for '{type.Name}' was not resolved.");
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/12839")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_GridView_Default_Style_Uses_WrapPanel()
	{
		// ItemsWrapGrid is not implemented on Skia, so the overlay swaps in a WrapPanel.
		var sut = new GridView { Width = 200, Height = 200, ItemsSource = new[] { 1, 2, 3 } };

		await UITestHelper.Load(sut, x => x.IsLoaded);

		Assert.IsInstanceOfType<WrapPanel>(sut.ItemsPanelRoot);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/12839")]
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
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/12839")]
	// Same hosts as Given_WebView2: these have no native web view to attach.
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaIslands | RuntimeTestPlatforms.SkiaFrameBuffer)]
	public async Task When_WebView2_Default_Template_Hosts_Native_View()
	{
		// CoreWebView2.GetNativeWebViewFromTemplate only attaches a native view when the first
		// child is a ContentPresenter named WebViewTemplateRoot; WinUI's generic.xaml has no
		// WebView2 style, so Uno has to supply this one.
		var sut = new WebView2 { Width = 100, Height = 100 };

		await UITestHelper.Load(sut, x => x.IsLoaded);

		Assert.IsNotNull(sut.Template);
		Assert.AreEqual(1, VisualTreeHelper.GetChildrenCount(sut));
		Assert.AreEqual("WebViewTemplateRoot", (VisualTreeHelper.GetChild(sut, 0) as ContentPresenter)?.Name);
	}
}
#endif
