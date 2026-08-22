#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Native_Automation_Text_Pattern_Is_Requested_Managed_Projection_Is_Null()
	{
		var editor = new RichEditBox();
		try
		{
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(editor);

			Assert.IsNotNull(peer);
			Assert.IsNull(peer.GetPattern(PatternInterface.Text));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
