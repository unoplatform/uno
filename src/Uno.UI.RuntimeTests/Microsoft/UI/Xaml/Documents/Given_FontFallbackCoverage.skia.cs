using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Composition.Drawing;
using Windows.UI.Text;

#nullable enable

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Documents
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_FontFallbackCoverage
	{
		private const int Alef = 0x0627;
		private const int NArySummation = 0x2211;
		private const int MathematicalBoldCapitalA = 0x1D400;

		// Noto Sans Math carries the basic Arabic letters as bases for its Arabic math alphabet, with no joining forms.
		private static readonly FontFallbackCoverageRange[] _table =
		[
			new(Alef, Alef + 1, ["Noto Sans Arabic", "Noto Sans Math"]),
			new(NArySummation, NArySummation + 1, ["Noto Sans CJK", "Noto Sans Math"]),
			new(MathematicalBoldCapitalA, MathematicalBoldCapitalA + 1, ["Noto Sans Math"]),
		];

		[TestMethod]
		public async Task When_Symbol_Font_Fetched_Then_Letter_Still_Gets_Its_Script_Font()
		{
			var fetched = new List<string>();
			var SUT = new CoverageTableFontFallbackService(_table, Provider(fetched));

			Assert.AreEqual("Noto Sans Math", await SUT.GetFontFamilyForCodepoint(MathematicalBoldCapitalA));
			Assert.AreEqual("Noto Sans Arabic", await SUT.GetFontFamilyForCodepoint(Alef));
			CollectionAssert.AreEqual(new[] { "Noto Sans Math", "Noto Sans Arabic" }, fetched);
		}

		[TestMethod]
		public async Task When_Script_Font_Fails_To_Load_Then_Symbol_Font_Is_Used()
		{
			var fetched = new List<string>();
			var SUT = new CoverageTableFontFallbackService(_table, Provider(fetched, failing: "Noto Sans Arabic"));

			Assert.AreEqual("Noto Sans Math", await SUT.GetFontFamilyForCodepoint(MathematicalBoldCapitalA));
			Assert.AreEqual("Noto Sans Math", await SUT.GetFontFamilyForCodepoint(Alef));
			CollectionAssert.AreEqual(new[] { "Noto Sans Math", "Noto Sans Arabic" }, fetched);
		}

		[TestMethod]
		public async Task When_Symbol_Covered_By_Loaded_Symbol_Font_Then_Nothing_Else_Is_Fetched()
		{
			var fetched = new List<string>();
			var SUT = new CoverageTableFontFallbackService(_table, Provider(fetched));

			Assert.AreEqual("Noto Sans Math", await SUT.GetFontFamilyForCodepoint(MathematicalBoldCapitalA));
			Assert.AreEqual("Noto Sans Math", await SUT.GetFontFamilyForCodepoint(NArySummation));
			CollectionAssert.AreEqual(new[] { "Noto Sans Math" }, fetched);
		}

		private static Func<string, FontWeight, FontStretch, FontStyle, CancellationToken, Task<Stream?>> Provider(List<string> fetched, string? failing = null)
			=> (family, _, _, _, _) =>
			{
				fetched.Add(family);
				return Task.FromResult<Stream?>(family == failing ? null : new MemoryStream(new byte[] { 1 }));
			};
	}
}
