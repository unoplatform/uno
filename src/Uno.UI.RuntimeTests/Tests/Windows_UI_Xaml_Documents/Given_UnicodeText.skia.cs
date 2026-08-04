#if __SKIA__
#nullable enable

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
	[TestMethod]
	[DataRow("\uD83D", DisplayName = "Lone high surrogate")]
	[DataRow("\uDE00", DisplayName = "Lone low surrogate")]
	[DataRow("a\uD83Db", DisplayName = "High surrogate between characters")]
	[DataRow("😀", DisplayName = "Valid surrogate pair")]
	public async Task When_Text_Contains_Unpaired_Surrogate(string text)
	{
		// Text layout must tolerate unpaired surrogates: they occur transiently while a surrogate
		// pair is being typed one code unit at a time.
		var textBlock = new TextBlock { Text = text };
		await UITestHelper.Load(textBlock, x => x.IsLoaded);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(text, textBlock.Text);
	}

	[TestMethod]
	[DataRow("\uD83D", DisplayName = "Lone high surrogate")]
	[DataRow("\uDE00", DisplayName = "Lone low surrogate")]
	[DataRow("a\uD83Db", DisplayName = "High surrogate between characters")]
	[DataRow("😀", DisplayName = "Valid surrogate pair")]
	public async Task When_TextBox_Text_Contains_Unpaired_Surrogate(string text)
	{
		var textBox = new TextBox { Text = text };
		await UITestHelper.Load(textBox, x => x.IsLoaded);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(text, textBox.Text);
	}

	[TestMethod]
	public async Task When_TextBox_Text_Grows_Into_Surrogate_Pair()
	{
		// Mirrors typing a surrogate pair one code unit at a time, which is how a pair arrives
		// through keyboard injection and through a real IME commit.
		var textBox = new TextBox();
		await UITestHelper.Load(textBox, x => x.IsLoaded);

		textBox.Text = "\uD83D";
		await TestServices.WindowHelper.WaitForIdle();

		textBox.Text = "😀";
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual("😀", textBox.Text);
	}
}
#endif
