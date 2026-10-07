#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Input;
using Windows.UI.ViewManagement;

namespace Uno.UI.Tests.Windows_UI;

[TestClass]
public class Given_UISettings
{
	[TestMethod]
	public void When_DoubleClickTime_Then_Matches_GestureRecognizer_MultiTap_Delay()
	{
		UISettings settings = new();

		Assert.AreEqual(500u, settings.DoubleClickTime);
		Assert.AreEqual(GestureRecognizer.MultiTapMaxDelayMicroseconds / 1000, (ulong)settings.DoubleClickTime);
	}
}
