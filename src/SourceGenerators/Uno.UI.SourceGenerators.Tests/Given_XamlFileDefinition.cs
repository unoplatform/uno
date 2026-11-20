using System.Collections.Immutable;
using Uno.UI.SourceGenerators.XamlGenerator;

namespace Uno.UI.SourceGenerators.Tests;

[TestClass]
public class Given_XamlFileDefinition
{
	private static readonly string CheckoutA = Path.Join(Path.GetTempPath(), "checkout-a");
	private static readonly string CheckoutB = Path.Join(Path.GetTempPath(), "checkout-b");

	private static XamlFileDefinition Create(string file, string link, string? intermediateDirectory = null)
		=> new(file, link, "theme/Styles.xaml", "<ResourceDictionary />", ImmutableArray<byte>.Empty, intermediateDirectory);

	[TestMethod]
	public void When_Checkout_Root_Differs_UniqueID_Is_Stable()
	{
		var a = Create(Path.Join(CheckoutA, "theme", "Styles.xaml"), "theme/Styles.xaml");
		var b = Create(Path.Join(CheckoutB, "theme", "Styles.xaml"), "theme/Styles.xaml");

		a.UniqueID.Should().Be(b.UniqueID);
		a.UniqueID.Should().StartWith("Styles_");
	}

	[TestMethod]
	public void When_Link_Separator_Differs_UniqueID_Is_Stable()
	{
		var a = Create(Path.Join(CheckoutA, "theme", "Styles.xaml"), @"theme\Styles.xaml");
		var b = Create(Path.Join(CheckoutB, "theme", "Styles.xaml"), "theme/Styles.xaml");

		a.UniqueID.Should().Be(b.UniqueID);
	}

	[TestMethod]
	public void When_Same_FileName_In_Different_Folders_UniqueID_Differs()
	{
		var a = Create(Path.Join(CheckoutA, "theme", "Styles.xaml"), "theme/Styles.xaml");
		var b = Create(Path.Join(CheckoutA, "other", "Styles.xaml"), "other/Styles.xaml");

		a.UniqueID.Should().NotBe(b.UniqueID);
	}

	[TestMethod]
	public void When_Link_Is_Absolute_Falls_Back_To_FilePath()
	{
		var fileA = Path.Join(CheckoutA, "obj", "Generated.xaml");
		var fileB = Path.Join(CheckoutB, "obj", "Generated.xaml");

		var a = Create(fileA, fileA);
		var b = Create(fileB, fileB);

		a.UniqueID.Should().NotBe(b.UniqueID);
	}

	[TestMethod]
	public void When_Generated_Xaml_Is_Under_Intermediate_Directory_UniqueID_Is_Stable()
	{
		var fileA = Path.Join(CheckoutA, ".artifacts", "obj", "theme", "Generated.xaml");
		var fileB = Path.Join(CheckoutB, ".artifacts", "obj", "theme", "Generated.xaml");

		var a = Create(fileA, fileA, Path.Join(CheckoutA, ".artifacts", "obj"));
		var b = Create(fileB, fileB, Path.Join(CheckoutB, ".artifacts", "obj"));

		a.UniqueID.Should().Be(b.UniqueID);
		a.UniqueID.Should().StartWith("Generated_");
	}

	[TestMethod]
	public void When_Generated_Xaml_Is_Under_Intermediate_Directory_Identity_Is_Relative_To_It()
	{
		var source = Path.Join(CheckoutA, ".artifacts", "obj", "theme", "Generated.xaml");

		XamlFileDefinition.GetIdentityPath(source, source, Path.Join(CheckoutA, ".artifacts", "obj"))
			.Should().Be("obj:theme/Generated.xaml");
	}

	[TestMethod]
	public void When_Intermediate_Directory_Has_Parent_Segments_Identity_Is_Relative_To_It()
	{
		var source = Path.Join(CheckoutA, ".artifacts", "obj", "theme", "Generated.xaml");
		var intermediate = Path.Join(CheckoutA, "src", "App", "..", "..", ".artifacts", "obj");

		XamlFileDefinition.GetIdentityPath(source, source, intermediate)
			.Should().Be("obj:theme/Generated.xaml");
	}

	[TestMethod]
	public void When_Link_Is_Relative_Intermediate_Directory_Is_Ignored()
	{
		var source = Path.Join(CheckoutA, "theme", "Styles.xaml");

		XamlFileDefinition.GetIdentityPath(source, "theme/Styles.xaml", CheckoutA)
			.Should().Be("theme/Styles.xaml");
	}
}
