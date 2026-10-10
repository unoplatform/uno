#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
[RunsOnUIThread]
public class Given_PlaceholderDescribedBy
{
	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Text_Entered_Placeholder_No_Longer_Describes_Control(bool isPasswordBox)
	{
		Control control = isPasswordBox
			? new PasswordBox { PlaceholderText = "Enter a value", Width = 200 }
			: new TextBox { PlaceholderText = "Enter a value", Width = 200 };

		try
		{
			await UITestHelper.Load(control);
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control);
			Assert.IsNotNull(peer);

			peer.GetDescribedBy();
			Assert.HasCount(1, AutomationProperties.GetDescribedBy(control), "The visible placeholder should describe the empty control.");

			if (control is PasswordBox passwordBox)
			{
				passwordBox.Password = "secret";
			}
			else
			{
				((TextBox)control).Text = "value";
			}
			await WindowHelper.WaitForIdle();

			Assert.IsEmpty(AutomationProperties.GetDescribedBy(control), "A hidden placeholder must not keep describing the control.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
