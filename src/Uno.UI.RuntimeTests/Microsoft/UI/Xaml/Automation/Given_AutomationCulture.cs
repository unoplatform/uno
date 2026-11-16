#nullable enable

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
[RunsOnUIThread]
public class Given_AutomationCulture
{
	[TestMethod]
	[DataRow("en-US", 1033)]
	[DataRow("fr-FR", 1036)]
	[DataRow("fr", 12)]
	public void When_Culture_Is_Unset_Then_Uses_Language(string language, int expected)
	{
		var owner = new Button { Language = language };
		var peer = new ButtonAutomationPeer(owner);

		Assert.AreEqual(expected, peer.GetCulture());
	}

	[TestMethod]
	[DataRow(0)]
	[DataRow(1031)]
	public void When_Culture_Is_Set_Then_Overrides_Language(int culture)
	{
		var owner = new Button { Language = "fr-FR" };
		AutomationProperties.SetCulture(owner, culture);
		var peer = new ButtonAutomationPeer(owner);

		Assert.AreEqual(culture, peer.GetCulture());
	}

	[TestMethod]
	public void When_Culture_Is_Cleared_Then_Uses_Current_Language()
	{
		var owner = new Button { Language = "fr-FR" };
		AutomationProperties.SetCulture(owner, 0);
		var peer = new ButtonAutomationPeer(owner);

		owner.ClearValue(AutomationProperties.CultureProperty);
		Assert.AreEqual(1036, peer.GetCulture());

		owner.Language = "de-DE";
		Assert.AreEqual(1031, peer.GetCulture());
	}
}
