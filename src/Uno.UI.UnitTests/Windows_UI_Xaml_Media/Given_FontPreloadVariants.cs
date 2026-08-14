using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI;
using Uno.UI.Xaml.Media;
using Windows.UI.Text;

namespace Uno.UI.Tests.Windows_UI_Xaml_Media;

[TestClass]
public class Given_FontPreloadVariants
{
	private const FontPreloadVariants Default =
		FontPreloadVariants.Normal | FontPreloadVariants.SemiBold | FontPreloadVariants.Bold;

	private static FontInfo Font(ushort weight, FontStretch stretch = FontStretch.Normal, FontStyle style = FontStyle.Normal)
		=> new() { FamilyName = "ms-appx:///test.ttf", FontWeight = weight, FontStretch = stretch, FontStyle = style };

	[TestMethod]
	public void When_Default_Then_Upright_Regular_Semibold_Bold_Selected()
	{
		Assert.IsTrue(FontFamilyHelper.IsVariantSelected(Font(FontWeights.Normal.Weight), Default));
		Assert.IsTrue(FontFamilyHelper.IsVariantSelected(Font(FontWeights.SemiBold.Weight), Default));
		Assert.IsTrue(FontFamilyHelper.IsVariantSelected(Font(FontWeights.Bold.Weight), Default));
	}

	[TestMethod]
	public void When_Default_Then_Other_Weights_Excluded()
	{
		Assert.IsFalse(FontFamilyHelper.IsVariantSelected(Font(FontWeights.Thin.Weight), Default));
		Assert.IsFalse(FontFamilyHelper.IsVariantSelected(Font(FontWeights.Light.Weight), Default));
		Assert.IsFalse(FontFamilyHelper.IsVariantSelected(Font(FontWeights.Medium.Weight), Default));
		Assert.IsFalse(FontFamilyHelper.IsVariantSelected(Font(FontWeights.ExtraBold.Weight), Default));
		Assert.IsFalse(FontFamilyHelper.IsVariantSelected(Font(FontWeights.Black.Weight), Default));
	}

	[TestMethod]
	public void When_Italic_Not_Requested_Then_Italic_Excluded()
	{
		var italicRegular = Font(FontWeights.Normal.Weight, style: FontStyle.Italic);

		Assert.IsFalse(FontFamilyHelper.IsVariantSelected(italicRegular, Default));
		Assert.IsTrue(FontFamilyHelper.IsVariantSelected(italicRegular, Default | FontPreloadVariants.Italic));
	}

	[TestMethod]
	public void When_Condensed_Not_Requested_Then_Condensed_Excluded()
	{
		var condensedRegular = Font(FontWeights.Normal.Weight, stretch: FontStretch.Condensed);

		Assert.IsFalse(FontFamilyHelper.IsVariantSelected(condensedRegular, Default));
		Assert.IsTrue(FontFamilyHelper.IsVariantSelected(condensedRegular, Default | FontPreloadVariants.Condensed));
	}

	[TestMethod]
	public void When_Condensed_Requested_Then_Weight_Still_Filtered()
	{
		var condensedLight = Font(FontWeights.Light.Weight, stretch: FontStretch.Condensed);

		Assert.IsFalse(FontFamilyHelper.IsVariantSelected(condensedLight, Default | FontPreloadVariants.Condensed));
	}

	[TestMethod]
	public void When_None_Then_Nothing_Selected()
		=> Assert.IsFalse(FontFamilyHelper.IsVariantSelected(Font(FontWeights.Normal.Weight), FontPreloadVariants.None));

	[TestMethod]
	public void When_All_Then_Every_Variant_Selected()
	{
		var variants = FontPreloadVariants.All;

		Assert.IsTrue(FontFamilyHelper.IsVariantSelected(Font(FontWeights.Thin.Weight), variants));
		Assert.IsTrue(FontFamilyHelper.IsVariantSelected(Font(FontWeights.Black.Weight, FontStretch.Condensed, FontStyle.Italic), variants));
	}

	[TestMethod]
	public void When_Weight_Is_Between_Named_Values_Then_Rounded_Up()
	{
		// OpenType allows arbitrary weights; 350 sits between Light (300) and Normal (400).
		Assert.IsTrue(FontFamilyHelper.IsVariantSelected(Font(350), Default));
		Assert.IsFalse(FontFamilyHelper.IsVariantSelected(Font(350), FontPreloadVariants.Light));
	}
}
