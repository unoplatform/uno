#nullable enable

using System.Collections.Generic;
using System.Globalization;
using Microsoft.UI.Xaml.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Text
{
	[TestClass]
	public class Given_UnicodeText_Boundaries
	{
		private const int UBRK_LINE = 2;

		private CultureInfo? _originalUICulture;

		[TestInitialize]
		public void Init() => _originalUICulture = CultureInfo.CurrentUICulture;

		[TestCleanup]
		public void Cleanup() => CultureInfo.CurrentUICulture = _originalUICulture!;

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24756")]
		public void When_UICulture_Is_Japanese_Then_Line_Breaks_Use_Japanese_Rules()
		{
			// ICU's Japanese line-break tailoring allows a break before a small kana (ャ), while root rules don't.
			const string text = "キャンセル";

			CultureInfo.CurrentUICulture = new CultureInfo("en-US");
			var english = GetLineBreaks(text);

			CultureInfo.CurrentUICulture = new CultureInfo("ja-JP");
			var japanese = GetLineBreaks(text);

			CollectionAssert.DoesNotContain(english, 1, "Root line-break rules must not break before a small kana.");
			CollectionAssert.Contains(japanese, 1, "The ja-JP UI culture must reach ICU so its Japanese line-break rules apply.");
		}

		private static List<int> GetLineBreaks(string text)
		{
			var breaks = new List<int>();
			UnicodeText.AppendBoundaries(UBRK_LINE, text, 0, breaks);
			return breaks;
		}
	}
}
