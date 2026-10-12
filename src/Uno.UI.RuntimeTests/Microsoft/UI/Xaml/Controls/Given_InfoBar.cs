using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.Extensions;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_InfoBar
{
	[TestCleanup]
	public void Cleanup()
	{
		TestServices.WindowHelper.WindowContent = null;
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25015")]
	public async Task When_Default_Style_Then_FluentTheme_Template_Applies()
	{
		var SUT = new InfoBar { IsOpen = true, Title = "Title", Message = "Message" };

		TestServices.WindowHelper.WindowContent = SUT;
		await TestServices.WindowHelper.WaitForLoaded(SUT);

		Assert.IsNotNull(SUT.FindFirstDescendant<Border>("ContentRoot"));
		// The WinUI 2 "v1" style used a 0 border thickness.
		Assert.AreEqual(new Thickness(1), SUT.BorderThickness);
	}
}
