#nullable enable

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.ViewManagement;

namespace Uno.UI.Tests.Windows_UI;

[TestClass]
public class Given_UISettings_GetDoubleClickTime
{
	[TestMethod]
	public void When_Static_GetDoubleClickTime_Then_Matches_Instance_Property()
	{
		UISettings settings = new();

		Assert.AreEqual(settings.DoubleClickTime, UISettings.GetDoubleClickTime());
	}

	[TestMethod]
	public void When_Static_GetDoubleClickTime_Then_Returns_Host_Value()
	{
		// The user may have changed the double-click speed, so Windows is checked against user32 itself.
		var expected = OperatingSystem.IsWindows() ? HostGetDoubleClickTime() : 500u;

		Assert.AreEqual(expected, UISettings.GetDoubleClickTime());
	}

	[DllImport("user32.dll", EntryPoint = "GetDoubleClickTime")]
	private static extern uint HostGetDoubleClickTime();
}
