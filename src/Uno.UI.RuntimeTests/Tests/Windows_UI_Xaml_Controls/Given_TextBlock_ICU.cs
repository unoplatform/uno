using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_TextBlock_ICU
{
	[TestMethod]
	public async Task When_Latin_Text_Is_Measured()
	{
		var SUT = new TextBlock { Text = "Hello World" };

		WindowHelper.WindowContent = SUT;
		await WindowHelper.WaitForLoaded(SUT);
		await WindowHelper.WaitForIdle();

		Assert.IsTrue(SUT.ActualWidth > 0, "Latin text should have non-zero width.");
		Assert.IsTrue(SUT.ActualHeight > 0, "Latin text should have non-zero height.");
	}

	[TestMethod]
	public async Task When_Arabic_BiDi_Text_Is_Measured()
	{
		var SUT = new TextBlock { Text = "اللغة العربية" };

		WindowHelper.WindowContent = SUT;
		await WindowHelper.WaitForLoaded(SUT);
		await WindowHelper.WaitForIdle();

		Assert.IsTrue(SUT.ActualWidth > 0, "Arabic text should have non-zero width.");
		Assert.IsTrue(SUT.ActualHeight > 0, "Arabic text should have non-zero height.");
	}

	[TestMethod]
	public async Task When_Hebrew_BiDi_Text_Is_Measured()
	{
		var SUT = new TextBlock { Text = "שלום עולם" };

		WindowHelper.WindowContent = SUT;
		await WindowHelper.WaitForLoaded(SUT);
		await WindowHelper.WaitForIdle();

		Assert.IsTrue(SUT.ActualWidth > 0, "Hebrew text should have non-zero width.");
		Assert.IsTrue(SUT.ActualHeight > 0, "Hebrew text should have non-zero height.");
	}

	[TestMethod]
	public async Task When_Mixed_BiDi_Text_Is_Measured()
	{
		var SUT = new TextBlock { Text = "Hello مرحبا World" };

		WindowHelper.WindowContent = SUT;
		await WindowHelper.WaitForLoaded(SUT);
		await WindowHelper.WaitForIdle();

		Assert.IsTrue(SUT.ActualWidth > 0, "Mixed BiDi text should have non-zero width.");
		Assert.IsTrue(SUT.ActualHeight > 0, "Mixed BiDi text should have non-zero height.");
	}

	[TestMethod]
	public async Task When_CJK_Text_Is_Measured()
	{
		var SUT = new TextBlock { Text = "示例文本" };

		WindowHelper.WindowContent = SUT;
		await WindowHelper.WaitForLoaded(SUT);
		await WindowHelper.WaitForIdle();

		Assert.IsTrue(SUT.ActualWidth > 0, "CJK text should have non-zero width.");
		Assert.IsTrue(SUT.ActualHeight > 0, "CJK text should have non-zero height.");
	}

	[TestMethod]
	public async Task When_Thai_Text_Wraps_Using_ICU_Dictionary_Breaker()
	{
		// Thai has no inter-word spaces. Finding legal break opportunities requires
		// ICU's dictionary-based line break iterator. Without ICU, the run cannot be
		// broken internally and the text stays on a single line regardless of width.
		const string ThaiText = "นี่คือประโยคภาษาไทยที่ยาวพอสมควรและควรตัดบรรทัดเมื่อถูกจำกัดความกว้าง";

		var singleLine = new TextBlock { Text = ThaiText };
		WindowHelper.WindowContent = singleLine;
		await WindowHelper.WaitForLoaded(singleLine);
		await WindowHelper.WaitForIdle();

		var singleLineHeight = singleLine.ActualHeight;

		var wrapped = new TextBlock
		{
			Text = ThaiText,
			TextWrapping = TextWrapping.Wrap,
			Width = 100,
		};
		WindowHelper.WindowContent = wrapped;
		await WindowHelper.WaitForLoaded(wrapped);
		await WindowHelper.WaitForIdle();

		Assert.IsTrue(
			wrapped.ActualHeight > singleLineHeight,
			$"Thai line breaking requires ICU's dictionary break iterator. " +
			$"wrapped={wrapped.ActualHeight}, singleLine={singleLineHeight}");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Native)]
	public async Task When_NonBreakingSpace_Is_Not_A_Wrap_Opportunity()
	{
		// Per UAX #14, NBSP (U+00A0) is a glue character: line breakers must not
		// split a run there. ICU honours this; a naive space-splitter would treat
		// NBSP like a regular space and wrap the same way.
		// WrapWholeWords is required so Uno doesn't fall back to an emergency
		// char-break when no ICU break opportunity is found — that fallback
		// would mask whether NBSP was actually excluded from the break set.
		var withSpace = new TextBlock
		{
			Text = "Hello World",
			TextWrapping = TextWrapping.WrapWholeWords,
			Width = 60,
		};
		WindowHelper.WindowContent = withSpace;
		await WindowHelper.WaitForLoaded(withSpace);
		await WindowHelper.WaitForIdle();

		var spaceHeight = withSpace.ActualHeight;

		var withNbsp = new TextBlock
		{
			Text = "Hello World",
			TextWrapping = TextWrapping.WrapWholeWords,
			Width = 60,
		};
		WindowHelper.WindowContent = withNbsp;
		await WindowHelper.WaitForLoaded(withNbsp);
		await WindowHelper.WaitForIdle();

		Assert.IsTrue(
			withNbsp.ActualHeight < spaceHeight,
			$"NBSP must not be a wrap opportunity under ICU. " +
			$"nbsp={withNbsp.ActualHeight}, space={spaceHeight}");
	}

	[TestMethod]
	public async Task When_RTL_FlowDirection_Text_Is_Measured()
	{
		var SUT = new TextBlock
		{
			Text = "مرحبا بالعالم",
			FlowDirection = FlowDirection.RightToLeft
		};

		WindowHelper.WindowContent = SUT;
		await WindowHelper.WaitForLoaded(SUT);
		await WindowHelper.WaitForIdle();

		Assert.IsTrue(SUT.ActualWidth > 0, "RTL FlowDirection text should have non-zero width.");
		Assert.IsTrue(SUT.ActualHeight > 0, "RTL FlowDirection text should have non-zero height.");
	}

	[TestMethod]
	public async Task When_Text_Is_Remeasured_After_Other_Text_Then_Size_Is_Unchanged()
	{
		// Line-break opportunities come from an ICU break iterator that is cached per thread and
		// re-pointed at each new string, so a measure must not depend on what was measured before it.
		// Wrapping at a fixed width is what makes those boundaries observable: a stale iterator
		// changes where the text breaks, and so its height.
		const string Probe = "The quick brown fox jumps over the lazy dog";

		var before = await MeasureWrappedAsync(Probe);

		// Different script, different length, and a single character, to leave the most stale state behind.
		await MeasureWrappedAsync("مرحبا بالعالم وأهلا وسهلا بكم جميعا");
		await MeasureWrappedAsync("a");

		var after = await MeasureWrappedAsync(Probe);

		Assert.AreEqual(before.Width, after.Width, 0.01, $"Width changed after measuring other text: {before.Width} -> {after.Width}.");
		Assert.AreEqual(before.Height, after.Height, 0.01, $"Height changed after measuring other text, so the text wrapped differently: {before.Height} -> {after.Height}.");
	}

	[TestMethod]
	public async Task When_Wrapped_Text_Is_Measured_Then_It_Breaks_Onto_Several_Lines()
	{
		// Guards the boundaries themselves rather than their stability: were the iterator to return
		// no break opportunities, the text would occupy a single line and the test above would still pass.
		var single = await MeasureWrappedAsync("a");
		var wrapped = await MeasureWrappedAsync("The quick brown fox jumps over the lazy dog");

		Assert.IsTrue(
			wrapped.Height > single.Height * 2,
			$"Wrapped text should occupy several lines, but its height ({wrapped.Height}) is not much more than one line ({single.Height}).");
	}

	private static async Task<Size> MeasureWrappedAsync(string text)
	{
		var SUT = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, Width = 120 };

		WindowHelper.WindowContent = SUT;
		await WindowHelper.WaitForLoaded(SUT);
		await WindowHelper.WaitForIdle();

		return new Size(SUT.ActualWidth, SUT.ActualHeight);
	}
}
