#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Globalization;

namespace Uno.UI.Tests.Windows_Globalization;

[TestClass]
public class Given_Language
{
	[TestMethod]
	[DataRow("en")]
	[DataRow("en-US")]
	[DataRow("de-DE")]
	[DataRow("zh-Hant-TW")]
	[DataRow("zh-yue-HK")]
	[DataRow("sr-Latn-RS")]
	[DataRow("es-419")]
	[DataRow("de-CH-1901")]
	[DataRow("sl-rozaj-biske")]
	[DataRow("hy-Latn-IT-arevela")]
	[DataRow("en-US-u-ca-gregory")]
	[DataRow("en-x-private")]
	[DataRow("x-whatever")]
	[DataRow("qaa-Qaaa-QM-x-southern")]
	[DataRow("i-klingon")]
	[DataRow("EN-gb-OED")]
	public void When_Tag_Is_WellFormed(string tag)
		=> Assert.IsTrue(Language.IsWellFormed(tag));

	[TestMethod]
	[DataRow("")]
	[DataRow("e")]
	[DataRow("x")]
	[DataRow("toolongtag")]
	[DataRow("en_US")]
	[DataRow("de-DE_phoneb")]
	[DataRow("en-")]
	[DataRow("-en")]
	[DataRow("en--US")]
	[DataRow("a-DE")]
	[DataRow("en-x")]
	[DataRow("en-US\n")]
	[DataRow("Ken")]
	public void When_Tag_Is_Not_WellFormed(string tag)
		=> Assert.IsFalse(Language.IsWellFormed(tag));

	[TestMethod]
	public void When_Tag_Is_Null()
		=> Assert.IsFalse(Language.IsWellFormed(null!));
}
