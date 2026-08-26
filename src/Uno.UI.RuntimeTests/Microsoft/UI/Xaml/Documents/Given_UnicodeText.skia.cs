#if __SKIA__
#nullable enable

using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Documents;

[TestClass]
[RunsOnUIThread]
public class Given_UnicodeText
{
	private const char HighSurrogate = '\uD83D';
	private const char LowSurrogate = '\uDE00';
	private const string Emoji = "😀";

	/// <summary>
	/// The samples are named rather than passed literally: a test name and an assertion message both
	/// travel through the test-results XML, which cannot carry an unpaired surrogate.
	/// </summary>
	public enum SurrogateSample
	{
		LoneHigh,
		LoneLow,
		HighBetweenCharacters,
		ValidPair,
	}

	private static string TextFor(SurrogateSample sample) => sample switch
	{
		SurrogateSample.LoneHigh => HighSurrogate.ToString(),
		SurrogateSample.LoneLow => LowSurrogate.ToString(),
		SurrogateSample.HighBetweenCharacters => $"a{HighSurrogate}b",
		_ => Emoji,
	};

	private static string Escape(string text)
		=> string.Concat(text.Select(c => char.IsSurrogate(c) ? $"U+{(int)c:X4}" : c.ToString()));

	[TestMethod]
	[DataRow(SurrogateSample.LoneHigh)]
	[DataRow(SurrogateSample.LoneLow)]
	[DataRow(SurrogateSample.HighBetweenCharacters)]
	[DataRow(SurrogateSample.ValidPair)]
	public async Task When_Text_Contains_Unpaired_Surrogate(SurrogateSample sample)
	{
		// Text layout must tolerate unpaired surrogates: they occur transiently while a surrogate
		// pair is being typed one code unit at a time.
		var text = TextFor(sample);
		var textBlock = new TextBlock { Text = text };
		await UITestHelper.Load(textBlock, x => x.IsLoaded);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(Escape(text), Escape(textBlock.Text));
	}

	[TestMethod]
	[DataRow(SurrogateSample.LoneHigh)]
	[DataRow(SurrogateSample.LoneLow)]
	[DataRow(SurrogateSample.HighBetweenCharacters)]
	[DataRow(SurrogateSample.ValidPair)]
	public async Task When_TextBox_Text_Contains_Unpaired_Surrogate(SurrogateSample sample)
	{
		var text = TextFor(sample);
		var textBox = new TextBox { Text = text };
		await UITestHelper.Load(textBox, x => x.IsLoaded);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(Escape(text), Escape(textBox.Text));
	}

	[TestMethod]
	public async Task When_TextBox_Text_Grows_Into_Surrogate_Pair()
	{
		// Mirrors typing a surrogate pair one code unit at a time, which is how a pair arrives
		// through keyboard injection and through a real IME commit.
		var textBox = new TextBox();
		await UITestHelper.Load(textBox, x => x.IsLoaded);

		textBox.Text = HighSurrogate.ToString();
		await TestServices.WindowHelper.WaitForIdle();

		textBox.Text = Emoji;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(Escape(Emoji), Escape(textBox.Text));
	}
}
#endif
