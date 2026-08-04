using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

/// <summary>
/// Covers the URI rewrite the XAML compiler applies to relative URIs. Expected values were measured on
/// native WinUI (WinAppSDK 1.7) for both application and library XAML.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_RelativeUriRewrite
{
	// Uno resolves the ms-appx form against the assembly root; WinUI resolves it against the XAML file's
	// folder, so the ms-appx expectations below are Uno-only.
	private const string AssemblyBase = "ms-appx:///Uno.UI.RuntimeTests/";

	[TestMethod]
	public void When_Relative_Uri_On_Uri_Property()
	{
		var SUT = new RelativeUriRewritePage();

		// A raw prefix concat, with the leading '/' trimmed - the file's folder plays no part.
		Assert.AreEqual("ms-resource:///Files/Assets/cart.png", SUT.customUri.Uri.ToString());
		Assert.AreEqual("ms-resource:///Files/Assets/cart.png", SUT.customUriRooted.Uri.ToString());
		Assert.AreEqual("ms-resource:///Files/Assets/UnoA4.pdf", SUT.navigateUri.NavigateUri.ToString());
		Assert.AreEqual("ms-resource:///Files/Assets/cart.png", SUT.bitmapIcon.UriSource.ToString());
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_Relative_Uri_On_ImageSource_Property()
	{
		var SUT = new RelativeUriRewritePage();
		var expected = AssemblyBase + "Assets/Transitive-ingredient01.png";

		Assert.AreEqual(expected, ((BitmapImage)SUT.image.Source).UriSource.ToString());
		Assert.AreEqual(expected, ((BitmapImage)SUT.customImageSource.ImageSource).UriSource.ToString());
		Assert.AreEqual(expected, ((BitmapImage)SUT.imageBrush.ImageSource).UriSource.ToString());
		Assert.AreEqual(expected, SUT.bitmapImage.UriSource.ToString());
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_Relative_Uri_On_SvgImageSource()
	{
		var SUT = new RelativeUriRewritePage();

		// WinUI emits the local-resource form here; Uno keeps the ms-appx form so that a library's
		// svg assets stay reachable through the assembly prefix.
		Assert.AreEqual(AssemblyBase + "Assets/couch.svg", SUT.svgImageSource.UriSource.ToString());
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_Local_Resource_Uri_Is_Mapped_To_Appx()
	{
		var SUT = new RelativeUriRewritePage();

		Assert.AreEqual("ms-appx:///Assets/cart.png", ((BitmapImage)SUT.localResourceImage.Source).UriSource.ToString());
	}

	// The two loading tests assert Uno's ms-resource -> ms-appx mapping, which has no WinUI analog:
	// there, MRT resolves the local-resource URI out of the package.
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Local_Resource_Uri_Loads()
	{
		var SUT = new RelativeUriRewritePage();

		await UITestHelper.Load(SUT);
		await TestServices.WindowHelper.WaitFor(() => SUT.localResourceImage.ActualHeight > 0, 3000);

		Assert.IsGreaterThan(0, SUT.localResourceImage.ActualHeight);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_BitmapIcon_With_Relative_Uri_Loads()
	{
		var SUT = new RelativeUriRewritePage();

		await UITestHelper.Load(SUT);
		await TestServices.WindowHelper.WaitFor(() => SUT.bitmapIcon.ActualHeight > 0, 3000);

		Assert.IsGreaterThan(0, SUT.bitmapIcon.ActualHeight);
	}
}
