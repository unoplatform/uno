#nullable enable

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
public class Given_AutomationPropertiesDefaults
{
	[TestMethod]
	[RunsOnUIThread]
	public void When_Properties_Are_Unset_Then_WinUI_Defaults_Are_Returned()
	{
		var button = new Button();

		Assert.AreEqual(string.Empty, AutomationProperties.GetName(button));
		Assert.AreEqual(-1, AutomationProperties.GetLevel(button));
		Assert.AreEqual(-1, AutomationProperties.GetPositionInSet(button));
		Assert.AreEqual(-1, AutomationProperties.GetSizeOfSet(button));
	}
}
