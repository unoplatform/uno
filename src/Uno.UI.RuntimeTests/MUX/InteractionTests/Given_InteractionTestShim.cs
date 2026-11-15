using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using AutomationProperties = Microsoft.UI.Xaml.Automation.AutomationProperties;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Tests.MUXControls.InteractionTests.Common;
using Microsoft.Windows.Apps.Test.Automation;
using Microsoft.Windows.Apps.Test.Foundation;
using Microsoft.Windows.Apps.Test.Foundation.Waiters;
using Private.Infrastructure;
using Windows.System;
using Windows.UI.Input.Preview.Injection;
using ShimCheckBox = Microsoft.Windows.Apps.Test.Foundation.Controls.CheckBox;
using ShimToggleButton = Microsoft.Windows.Apps.Test.Foundation.Controls.ToggleButton;
using XamlCheckBox = Microsoft.UI.Xaml.Controls.CheckBox;
using XamlToggleButton = Microsoft.UI.Xaml.Controls.Primitives.ToggleButton;

namespace Uno.UI.RuntimeTests.Tests.MUX.InteractionTests;

[TestClass]
[RunsOnUIThread]
public class Given_InteractionTestShim
{
	[TestCleanup]
	public void Cleanup()
	{
		ElementCache.Clear();
		TestServices.WindowHelper.WindowContent = null;
	}

	[TestMethod]
	public void When_SendKeys_Named_Key_With_Repeat()
	{
		var keys = TextInput.ToKeys("{ENTER 2}");

		AssertKeys(keys, (VirtualKey.Enter, false), (VirtualKey.Enter, true), (VirtualKey.Enter, false), (VirtualKey.Enter, true));
	}

	[TestMethod]
	public void When_SendKeys_Modifier_Prefix_And_Group()
	{
		var keys = TextInput.ToKeys("^(ac)");

		AssertKeys(keys,
			(VirtualKey.Control, false),
			(VirtualKey.A, false), (VirtualKey.A, true),
			(VirtualKey.C, false), (VirtualKey.C, true),
			(VirtualKey.Control, true));
	}

	[TestMethod]
	public void When_SendKeys_Explicit_Down_Up()
	{
		var keys = TextInput.ToKeys("{SHIFT DOWN}{TAB}{SHIFT UP}");

		AssertKeys(keys, (VirtualKey.Shift, false), (VirtualKey.Tab, false), (VirtualKey.Tab, true), (VirtualKey.Shift, true));
	}

	[TestMethod]
	public void When_SendKeys_Uppercase_Literal_Adds_Shift()
	{
		var keys = TextInput.ToKeys("A1");

		AssertKeys(keys,
			(VirtualKey.Shift, false), (VirtualKey.A, false), (VirtualKey.A, true), (VirtualKey.Shift, true),
			(VirtualKey.Number1, false), (VirtualKey.Number1, true));
	}

	[TestMethod]
	public void When_SendKeys_Escaped_Brace_Is_Unicode()
	{
		var keys = TextInput.ToKeys("{}}");

		Assert.AreEqual(2, keys.Count);
		Assert.IsTrue(keys.All(k => k.KeyOptions.HasFlag(InjectedInputKeyOptions.Unicode) && k.ScanCode == '}'));
	}

	[TestMethod]
	public async Task When_FindElement_By_Name_And_Id()
	{
		var button = new Button { Content = "Click" };
		AutomationProperties.SetName(button, "NamedButton");
		var other = new Button { Name = "IdButton", Content = "Other" };
		await LoadAsync(new StackPanel { Children = { button, other } });

		var byName = FindElement.ByName("NamedButton");
		var byId = FindElement.ById("IdButton");

		Assert.AreEqual(ControlType.Button, byName.ControlType);
		Assert.AreEqual("IdButton", byId.AutomationId);
		Assert.IsNull(TryFindElement.ByName("Missing"));
	}

	[TestMethod]
	public async Task When_Toggle_Through_Pattern_Signals_Waiter()
	{
		var checkBox = new XamlCheckBox { Content = "Check" };
		AutomationProperties.SetName(checkBox, "Check");
		await LoadAsync(checkBox);

		var shim = new ShimCheckBox(FindElement.ByName("Check"));
		using (var waiter = shim.GetToggledWaiter())
		{
			shim.Toggle();
			Assert.IsTrue(await waiter.TryWait(TimeSpan.FromSeconds(1)));
		}

		Assert.AreEqual(ToggleState.On, shim.ToggleState);
		Assert.IsTrue(checkBox.IsChecked == true);
	}

	[TestMethod]
	public async Task When_No_Change_Waiter_Times_Out()
	{
		var checkBox = new XamlCheckBox { Content = "Check" };
		AutomationProperties.SetName(checkBox, "Check");
		await LoadAsync(checkBox);

		var shim = new ShimCheckBox(FindElement.ByName("Check"));
		using var waiter = shim.GetToggledWaiter();

		Assert.IsFalse(await waiter.TryWait(TimeSpan.FromMilliseconds(100)));
		await Assert.ThrowsExactlyAsync<WaiterTimedOutException>(() => waiter.Wait(TimeSpan.FromMilliseconds(50)));
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Click_Injects_Pointer()
	{
		var toggle = new XamlToggleButton { Content = "Toggle", Width = 120, Height = 40 };
		AutomationProperties.SetName(toggle, "Toggle");
		await LoadAsync(toggle);

		await InputHelper.LeftClick(new ShimToggleButton(FindElement.ByName("Toggle")));

		Assert.IsTrue(toggle.IsChecked == true);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_PressKey_Goes_To_Focused_Element()
	{
		var checkBox = new XamlCheckBox { Content = "Check" };
		AutomationProperties.SetName(checkBox, "Check");
		await LoadAsync(checkBox);

		var shim = new ShimCheckBox(FindElement.ByName("Check"));
		shim.SetFocus();
		await Wait.ForIdle();
		Assert.IsTrue(shim.HasKeyboardFocus);

		await KeyboardHelper.PressKey(Key.Space);

		Assert.IsTrue(checkBox.IsChecked == true);
	}

	private static async Task LoadAsync(FrameworkElement content)
	{
		ElementCache.Clear();
		TestServices.WindowHelper.WindowContent = content;
		await TestServices.WindowHelper.WaitForLoaded(content);
		await TestServices.WindowHelper.WaitForIdle();
	}

	private static void AssertKeys(System.Collections.Generic.IReadOnlyList<InjectedInputKeyboardInfo> keys, params (VirtualKey Key, bool IsUp)[] expected)
	{
		var actual = keys.Select(k => ((VirtualKey)k.VirtualKey, k.KeyOptions.HasFlag(InjectedInputKeyOptions.KeyUp))).ToArray();
		CollectionAssert.AreEqual(expected.Select(e => (e.Key, e.IsUp)).ToArray(), actual, string.Join(", ", actual));
	}
}
