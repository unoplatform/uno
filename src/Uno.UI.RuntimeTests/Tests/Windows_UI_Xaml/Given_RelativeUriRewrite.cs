using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
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
		Assert.AreEqual("ms-resource:///Files/Assets/cart.png", SUT.bitmapIconSource.UriSource.ToString());
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

		// A rooted path is resolved against the application root, dropping the assembly prefix.
		Assert.AreEqual("ms-appx:///Assets/cart.png", ((BitmapImage)SUT.imageRooted.Source).UriSource.ToString());
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

	/// <summary>
	/// A local-resource URI converted to an ImageSource keeps the value it was given, the way WinUI
	/// reports a Uri-typed property; the mapping to ms-appx happens behind the property.
	/// </summary>
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_Local_Resource_Uri_Converted_To_ImageSource()
	{
#if HAS_UNO
		// The implicit Uri conversion is Uno-only surface, hence the guard.
		Microsoft.UI.Xaml.Media.ImageSource source = new Uri("ms-resource:///Files/Assets/cart.png");

		Assert.AreEqual("ms-resource:///Files/Assets/cart.png", ((BitmapImage)source).UriSource.ToString());
#endif
	}

	// The loading tests below assert Uno's ms-resource -> ms-appx mapping, which has no WinUI analog:
	// there, MRT resolves the local-resource URI out of the package. A non-zero ActualHeight means the
	// asset resolved - When_Local_Resource_On_UriSource_Loads is the case that fails when it does not.
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Local_Resource_Uri_Loads()
	{
		var SUT = new RelativeUriRewritePage();

		await UITestHelper.Load(SUT);
		await TestServices.WindowHelper.WaitFor(() => SUT.localResourceImage.ActualHeight > 0, 3000);

		Assert.IsGreaterThan(0, SUT.localResourceImage.ActualHeight);
	}

	/// <summary>
	/// A UriSource assigned directly keeps the value it was given (WinUI does the same), so the mapping
	/// has to happen where the URI is consumed - not only in the ImageSource conversion.
	/// </summary>
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Local_Resource_On_UriSource_Loads()
	{
		var SUT = new RelativeUriRewritePage();

		await UITestHelper.Load(SUT);
		await TestServices.WindowHelper.WaitFor(() => SUT.localResourceBitmapImage.ActualHeight > 0, 3000);

		Assert.AreEqual("ms-resource:///Files/Assets/cart.png", SUT.localResourceBitmap.UriSource.ToString());
		Assert.IsGreaterThan(0, SUT.localResourceBitmapImage.ActualHeight);
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

	/// <summary>
	/// BitmapIcon.UriSource has no changed callback - the value reaches the image through the binding
	/// its constructor sets up - so a URI assigned outside the compiled XAML has to map there too.
	/// </summary>
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Local_Resource_Assigned_To_BitmapIcon_Loads()
	{
		var SUT = new BitmapIcon { UriSource = new Uri("ms-resource:///Files/Assets/cart.png") };

		await UITestHelper.Load(SUT);
		await TestServices.WindowHelper.WaitFor(() => SUT.ActualHeight > 0, 3000);

		Assert.IsGreaterThan(0, SUT.ActualHeight);
	}

	/// <summary>
	/// BitmapIconSource hands its value to a BitmapIcon through a binding, so the mapping has to
	/// survive a copy between two Uri-typed properties.
	/// </summary>
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_BitmapIconSource_With_Relative_Uri_Loads()
	{
		var SUT = new RelativeUriRewritePage();

		await UITestHelper.Load(SUT);
		await TestServices.WindowHelper.WaitFor(() => SUT.bitmapIconSourceElement.ActualHeight > 0, 3000);

		Assert.IsGreaterThan(0, SUT.bitmapIconSourceElement.ActualHeight);
	}

	/// <summary>
	/// An ImageSource-typed property keeps the assembly prefix, so a library's own asset resolves. The
	/// ms-resource form cannot express that prefix, which is why a Uri-typed property in a library
	/// resolves against the app root instead.
	/// </summary>
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Library_Asset_On_ImageSource_Property_Loads()
	{
		var SUT = new RelativeUriRewritePage();

		await UITestHelper.Load(SUT);
		await TestServices.WindowHelper.WaitFor(() => SUT.image.ActualHeight > 0, 3000);

		Assert.IsGreaterThan(0, SUT.image.ActualHeight);
	}
}
