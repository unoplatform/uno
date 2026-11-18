using System.Collections.Immutable;
using Uno.UI.SourceGenerators.XamlGenerator;

namespace Uno.UI.SourceGenerators.Tests;

[TestClass]
public class Given_XamlFileDefinition
{
	private static XamlFileDefinition Create(string file, string link, string? stableIdentityPath = null)
		=> new(file, link, "theme/Styles.xaml", "<ResourceDictionary />", ImmutableArray<byte>.Empty, stableIdentityPath);

	[TestMethod]
	public void When_Checkout_Root_Differs_UniqueID_Is_Stable()
	{
		var a = Create(@"C:\checkout-a\theme\Styles.xaml", "theme/Styles.xaml");
		var b = Create(@"D:\checkout-b\theme\Styles.xaml", "theme/Styles.xaml");

		a.UniqueID.Should().Be(b.UniqueID);
		a.UniqueID.Should().StartWith("Styles_");
	}

	[TestMethod]
	public void When_Link_Separator_Differs_UniqueID_Is_Stable()
	{
		var a = Create(@"C:\a\theme\Styles.xaml", @"theme\Styles.xaml");
		var b = Create("/home/b/theme/Styles.xaml", "theme/Styles.xaml");

		a.UniqueID.Should().Be(b.UniqueID);
	}

	[TestMethod]
	public void When_Same_FileName_In_Different_Folders_UniqueID_Differs()
	{
		var a = Create(@"C:\a\theme\Styles.xaml", "theme/Styles.xaml");
		var b = Create(@"C:\a\other\Styles.xaml", "other/Styles.xaml");

		a.UniqueID.Should().NotBe(b.UniqueID);
	}

	[TestMethod]
	public void When_Link_Is_Absolute_Falls_Back_To_FilePath()
	{
		var a = Create(@"C:\a\obj\Generated.xaml", @"C:\a\obj\Generated.xaml");
		var b = Create(@"D:\b\obj\Generated.xaml", @"D:\b\obj\Generated.xaml");

		a.UniqueID.Should().NotBe(b.UniqueID);
	}

	[TestMethod]
	public void When_Stable_Identity_Provided_It_Wins()
	{
		var a = Create(@"C:\a\obj\Generated.xaml", @"C:\a\obj\Generated.xaml", "obj:theme/Generated.xaml");
		var b = Create(@"D:\b\obj\Generated.xaml", @"D:\b\obj\Generated.xaml", "obj:theme/Generated.xaml");

		a.UniqueID.Should().Be(b.UniqueID);
	}

	[TestMethod]
	public void When_Generated_Xaml_Is_Under_Intermediate_Directory_Identity_Is_Relative_To_It()
	{
		var root = Path.Combine(Path.GetTempPath(), "checkout");
		var source = Path.Combine(root, ".artifacts", "obj", "theme", "Generated.xaml");

		XamlFileParser.GetStableIdentityPath(source, source, Path.Combine(root, ".artifacts", "obj"))
			.Should().Be("obj:theme/Generated.xaml");
	}

	[TestMethod]
	public void When_Intermediate_Directory_Has_Parent_Segments_Identity_Is_Relative_To_It()
	{
		var root = Path.Combine(Path.GetTempPath(), "checkout");
		var source = Path.Combine(root, ".artifacts", "obj", "theme", "Generated.xaml");
		var intermediate = Path.Combine(root, "src", "App", "..", "..", ".artifacts", "obj");

		XamlFileParser.GetStableIdentityPath(source, source, intermediate)
			.Should().Be("obj:theme/Generated.xaml");
	}

	[TestMethod]
	public void When_Link_Is_Relative_No_Intermediate_Identity_Is_Used()
	{
		var source = Path.Combine(Path.GetTempPath(), "checkout", "theme", "Styles.xaml");

		XamlFileParser.GetStableIdentityPath(source, "theme/Styles.xaml", Path.GetTempPath())
			.Should().BeNull();
	}
}
