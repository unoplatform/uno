using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.X11;

namespace Uno.UI.Tests.Windows_UI_Xaml_Automation;

/// <summary>
/// Tests for <see cref="AtspiRoleMap"/>. libatspi derives the role a screen reader announces from the
/// numeric id alone, so the ids must match at-spi2-core's AtspiRole enum.
/// </summary>
[UITestClass]
public class Given_AtspiRoleMap
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24380")]
	public void When_ControlTypeIsMapped_Then_RoleMatchesAtspi()
	{
		Assert.AreEqual((43u, "push button"), AtspiRoleMap.GetRole(AutomationControlType.Button));
		Assert.AreEqual((79u, "entry"), AtspiRoleMap.GetRole(AutomationControlType.Edit));
		Assert.AreEqual((7u, "check box"), AtspiRoleMap.GetRole(AutomationControlType.CheckBox));
		Assert.AreEqual((44u, "radio button"), AtspiRoleMap.GetRole(AutomationControlType.RadioButton));
		Assert.AreEqual((51u, "slider"), AtspiRoleMap.GetRole(AutomationControlType.Slider));
		Assert.AreEqual((11u, "combo box"), AtspiRoleMap.GetRole(AutomationControlType.ComboBox));
		Assert.AreEqual((88u, "link"), AtspiRoleMap.GetRole(AutomationControlType.Hyperlink));
		Assert.AreEqual((42u, "progress bar"), AtspiRoleMap.GetRole(AutomationControlType.ProgressBar));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24380")]
	public void When_ControlTypeIsUnmapped_Then_RoleIsPanel()
		=> Assert.AreEqual((39u, "panel"), AtspiRoleMap.GetRole(AutomationControlType.Custom));

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24380")]
	public void When_StructuralRoleIsUsed_Then_IdMatchesAtspi()
	{
		Assert.AreEqual(75u, AtspiRoleMap.ApplicationRoleId);
		Assert.AreEqual(23u, AtspiRoleMap.FrameRoleId);
		Assert.AreEqual(16u, AtspiRoleMap.DialogRoleId);
		Assert.AreEqual(83u, AtspiRoleMap.HeadingRoleId);
		Assert.AreEqual(110u, AtspiRoleMap.LandmarkRoleId);
		Assert.AreEqual(40u, AtspiRoleMap.PasswordTextRoleId);
		Assert.AreEqual(62u, AtspiRoleMap.ToggleButtonRoleId);
	}
}
