using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Xaml;

namespace Uno.UI.Tests.Windows_UI_Xaml_Internal;

/// <summary>
/// Covers the mapping from the MRT local-resource form the XAML compiler emits for relative URIs
/// to the <c>ms-appx</c> form asset resolution understands. The mapping is total: anything it does
/// not recognise has to come back unchanged rather than throw or map to something else.
/// </summary>
[TestClass]
public class Given_XamlFilePathHelper
{
	[TestMethod]
	[DataRow("ms-resource:///Files/logo.png", "ms-appx:///logo.png")]
	[DataRow("ms-resource:///Files/Assets/logo.png", "ms-appx:///Assets/logo.png")]
	// The Uri parser lower-cases the scheme, so the Ordinal scheme comparison is safe.
	[DataRow("MS-RESOURCE:///Files/logo.png", "ms-appx:///logo.png")]
	// The folder segment is matched case-insensitively, as MRT resolves it.
	[DataRow("ms-resource:///files/logo.png", "ms-appx:///logo.png")]
	[DataRow("ms-resource:///FILES/logo.png", "ms-appx:///logo.png")]
	// A query belongs to the asset path and rides along.
	[DataRow("ms-resource:///Files/logo.png?v=2", "ms-appx:///logo.png?v=2")]
	// SvgImageSource resolves against the fragment, so dropping it would change what renders.
	[DataRow("ms-resource:///Files/icon.svg#layer1", "ms-appx:///icon.svg#layer1")]
	// Percent-escaping has to survive rather than be decoded into a different path.
	[DataRow("ms-resource:///Files/my%20logo.png", "ms-appx:///my%20logo.png")]
	public void When_Local_Resource_Then_Mapped_To_Appx(string input, string expected)
	{
		var result = XamlFilePathHelper.NormalizeMsResourceFilesUri(new Uri(input, UriKind.RelativeOrAbsolute));

		Assert.AreEqual(expected, result.AbsoluteUri);
	}

	[TestMethod]
	// Another package's resources are not reachable through ms-appx; mapping this would silently
	// resolve the *app's* asset of the same name.
	[DataRow("ms-resource://OtherPackage/Files/logo.png")]
	// Outside the Files/ subtree MRT holds strings, not files.
	[DataRow("ms-resource:///Resources/Greeting")]
	[DataRow("ms-resource:///logo.png")]
	[DataRow("ms-appx:///Assets/logo.png")]
	[DataRow("https://example.com/logo.png")]
	[DataRow("ms-appdata:///local/logo.png")]
	public void When_Not_A_Local_Resource_Then_Unchanged(string input)
	{
		var uri = new Uri(input, UriKind.RelativeOrAbsolute);

		Assert.AreSame(uri, XamlFilePathHelper.NormalizeMsResourceFilesUri(uri));
	}

	[TestMethod]
	[DataRow("Assets/logo.png")]
	[DataRow("/Assets/logo.png")]
	public void When_Relative_Then_Unchanged(string input)
	{
		var uri = new Uri(input, UriKind.Relative);

		Assert.AreSame(uri, XamlFilePathHelper.NormalizeMsResourceFilesUri(uri));
	}

	[TestMethod]
	[DataRow("ms-appx:///Assets/logo.png", "Assets/logo.png")]
	[DataRow("ms-appx:///logo.png", "logo.png")]
	[DataRow("MS-APPX:///Assets/logo.png", "Assets/logo.png")]
	public void When_Appx_Then_Asset_Path_Extracted(string input, string expected)
	{
		Assert.IsTrue(XamlFilePathHelper.TryGetMsAppxAssetPath(new Uri(input), out var path));
		Assert.AreEqual(expected, path);
	}

	[TestMethod]
	[DataRow("ms-resource:///Files/logo.png")]
	[DataRow("https://example.com/logo.png")]
	public void When_Not_Appx_Then_No_Asset_Path(string input)
	{
		Assert.IsFalse(XamlFilePathHelper.TryGetMsAppxAssetPath(new Uri(input), out var path));
		Assert.IsNull(path);
	}
}
