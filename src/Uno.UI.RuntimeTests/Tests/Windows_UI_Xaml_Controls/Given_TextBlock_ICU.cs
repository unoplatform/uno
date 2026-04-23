using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
	public async Task When_Wrapping_Text_Produces_Multiple_Lines()
	{
		var SUT = new TextBlock
		{
			Text = "This is a long sentence that should wrap to multiple lines when constrained.",
			TextWrapping = TextWrapping.Wrap,
			Width = 100
		};

		WindowHelper.WindowContent = SUT;
		await WindowHelper.WaitForLoaded(SUT);
		await WindowHelper.WaitForIdle();

		// With line breaking via ICU, constrained wrapping text should be taller than single-line text
		var singleLine = new TextBlock
		{
			Text = SUT.Text
		};

		WindowHelper.WindowContent = singleLine;
		await WindowHelper.WaitForLoaded(singleLine);
		await WindowHelper.WaitForIdle();

		Assert.IsTrue(SUT.ActualHeight > singleLine.ActualHeight,
			"Wrapped text should be taller than unwrapped text, confirming ICU line breaking works.");
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
}
