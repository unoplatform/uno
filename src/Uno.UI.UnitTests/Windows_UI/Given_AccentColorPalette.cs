using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Helpers.Theming;
using Windows.UI;

namespace Uno.UI.Tests.Windows_UI;

[TestClass]
public class Given_AccentColorPalette
{
	// Palettes captured from Windows 11 (UISettings.GetColorValue) after setting each accent as the system accent.
	// Order: Accent, Light1, Light2, Light3, Dark1, Dark2, Dark3.
	[TestMethod]
	[DataRow("0078D7", "0093FC", "61CCFF", "9AEBFF", "005FBA", "003F95", "001A6A")]
	[DataRow("0078D6", "0093FC", "61CCFF", "9AEBFF", "005FB9", "003F94", "001A6A")]
	[DataRow("E23219", "E84A2C", "F38862", "FAAB80", "C22714", "99190D", "690905")]
	[DataRow("87741A", "A99420", "DED256", "E9E587", "715A14", "543A0D", "331505")]
	[DataRow("CB28BF", "D83CCE", "E982E4", "F6A8F3", "AC1FA3", "861480", "590757")]
	[DataRow("CE3D7E", "D65493", "EA91C9", "F6B4E8", "B22D64", "881F45", "5A0B21")]
	[DataRow("6B7F34", "859B40", "BBCA7D", "DBE89D", "536529", "36451A", "132009")]
	[DataRow("2E750A", "378C0C", "40A40E", "4DC510", "245D08", "1B4606", "0E2503")]
	[DataRow("0D880E", "15AE11", "54ED3F", "8EF279", "0A700B", "075207", "022F03")]
	[DataRow("0A7570", "0C8C86", "0EA49D", "10C5BC", "085D59", "064643", "032523")]
	[DataRow("3B69FA", "567FFB", "9EB7FD", "C7D8FE", "1141F9", "0524CF", "040D91")]
	[DataRow("9949ED", "A960F0", "D49BF7", "EDBDFC", "731FE9", "4613BC", "1C0D7E")]
	[DataRow("C313F0", "CD2EF2", "E676F9", "F49EFC", "A30DD4", "790AAB", "4A037F")]
	[DataRow("C1B3C0", "CBBFCA", "D6CCD5", "E4DEE4", "B6A6B5", "AB99AA", "9D879B")]
	[DataRow("414141", "4D4D4D", "5A5A5A", "6B6B6B", "343434", "282828", "161616")]
	[DataRow("808080", "808080", "808080", "808080", "7F7F7F", "7F7F7F", "7F7F7F")]
	public void When_Accent_Is_Not_Normalized_By_Windows_Then_Matches_Windows(
		string accent, string light1, string light2, string light3, string dark1, string dark2, string dark3)
	{
		AssertPalette(AccentColorPalette.FromAccentColor(Parse(accent)), accent, light1, light2, light3, dark1, dark2, dark3);
	}

	[TestMethod]
	[DataRow("0078D4", "0091F8", "4CC2FF", "99EBFF", "0067C0", "003E92", "001A68")]
	[DataRow("E81123", "EF2733", "F46762", "FB9D8B", "D20E1E", "9E0912", "6F0306")]
	[DataRow("767676", "8B8B8B", "BBBBBA", "E6E6E6", "646464", "3B3B3B", "151515")]
	public void When_Accent_Is_Windows_Swatch_Then_Uses_Tuned_Palette(
		string accent, string light1, string light2, string light3, string dark1, string dark2, string dark3)
	{
		AssertPalette(AccentColorPalette.FromAccentColor(Parse(accent)), accent, light1, light2, light3, dark1, dark2, dark3);
	}

	// Windows adjusts these accents when picked; captured the same way. Order: Input, then Accent, Light1-3, Dark1-3.
	[TestMethod]
	[DataRow("000000", "3F3F3F", "4C4C4C", "595959", "6B6B6B", "333333", "262626", "141414")]
	[DataRow("FFFFFF", "BFBFBF", "CCCCCC", "D8D8D8", "EAEAEA", "B2B2B2", "A5A5A5", "939393")]
	[DataRow("000099", "8055F4", "946BF6", "CAA5FA", "E8C6FD", "5527F1", "2C0DCE", "12098A")]
	[DataRow("0000FF", "6C3EFF", "8455FF", "C191FF", "E5B3FF", "481CFF", "1F00F0", "0900BE")]
	[DataRow("00FF00", "009200", "03AE00", "13F800", "39FF23", "007A00", "005B00", "003800")]
	[DataRow("00FFCC", "008E64", "00AC7F", "00FDCD", "2CFFDF", "00764E", "005732", "003412")]
	[DataRow("263383", "6B6BC4", "8281CD", "BFBAE6", "E2DBF4", "4848B3", "353586", "13135C")]
	[DataRow("FF0000", "F40000", "FF150B", "FF6545", "FF9267", "D20000", "A80000", "770000")]
	public void When_Normalizing_Then_Matches_Windows(
		string input, string accent, string light1, string light2, string light3, string dark1, string dark2, string dark3)
	{
		AssertPalette(AccentPaletteGenerator.Generate(Parse(input), normalize: true), accent, light1, light2, light3, dark1, dark2, dark3);
	}

	[TestMethod]
	[DataRow("FFC600")]
	[DataRow("E01234")]
	[DataRow("007AFF")]
	[DataRow("1D295F")]
	[DataRow("000000")]
	[DataRow("FFFFFF")]
	public void When_Windows_Would_Normalize_Then_Accent_Is_Preserved_And_Shades_Are_Ordered(string accent)
	{
		var palette = AccentColorPalette.FromAccentColor(Parse(accent));

		Assert.AreEqual(Parse(accent), palette.Accent);

		var ramp = new[] { palette.Dark3, palette.Dark2, palette.Dark1, palette.Accent, palette.Light1, palette.Light2, palette.Light3 };
		var luminance = ramp.Select(c => 0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B).ToArray();
		for (var i = 1; i < luminance.Length; i++)
		{
			Assert.IsTrue(luminance[i] >= luminance[i - 1], $"Shade {i} of {accent} is darker than shade {i - 1}.");
		}
	}

	private static void AssertPalette(AccentColorPalette palette, string accent, string light1, string light2, string light3, string dark1, string dark2, string dark3)
	{
		Assert.AreEqual(Parse(accent), palette.Accent, "Accent");
		Assert.AreEqual(Parse(light1), palette.Light1, "Light1");
		Assert.AreEqual(Parse(light2), palette.Light2, "Light2");
		Assert.AreEqual(Parse(light3), palette.Light3, "Light3");
		Assert.AreEqual(Parse(dark1), palette.Dark1, "Dark1");
		Assert.AreEqual(Parse(dark2), palette.Dark2, "Dark2");
		Assert.AreEqual(Parse(dark3), palette.Dark3, "Dark3");
	}

	private static Color Parse(string rgb) =>
		Color.FromArgb(0xFF, System.Convert.ToByte(rgb[..2], 16), System.Convert.ToByte(rgb[2..4], 16), System.Convert.ToByte(rgb[4..], 16));
}
