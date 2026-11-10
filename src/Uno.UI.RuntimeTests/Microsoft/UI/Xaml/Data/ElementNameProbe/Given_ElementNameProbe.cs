using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data.ElementNameProbe;

// Reference-only parity probe for #19420, not meant to merge: each test asserts one expectation and its
// message reports what it observed, so the pass/fail pattern differs between WinUI and Uno on purpose.
[TestClass]
[RunsOnUIThread]
public class Given_ElementNameProbe
{
	private static async Task Settle(Func<bool> condition = null, int timeoutMs = 1500)
	{
		await TestServices.WindowHelper.WaitForIdle();
		var sw = Stopwatch.StartNew();
		while (condition is not null && !condition() && sw.ElapsedMilliseconds < timeoutMs)
		{
			await Task.Delay(50);
		}
		await TestServices.WindowHelper.WaitForIdle();
	}

	private static TextBlock FindProbe(DependencyObject root)
	{
		if (root is TextBlock { Tag: "probe" } tb)
		{
			return tb;
		}

		var count = VisualTreeHelper.GetChildrenCount(root);
		for (var i = 0; i < count; i++)
		{
			if (FindProbe(VisualTreeHelper.GetChild(root, i)) is { } found)
			{
				return found;
			}
		}

		return null;
	}

	private static string Describe(TextBlock tb) => tb is null ? "<probe not found>" : $"'{tb.Text}'";

	private static string Describe(Brush brush) => brush is SolidColorBrush scb ? scb.Color.ToString() : $"<{brush?.GetType().Name ?? "null"}>";

	// ---------- Baselines (one-shot model) ----------

	[TestMethod]
	public async Task B1_CodeAdded_In_Ctor()
	{
		var uc = new ENP_CodeAdded(addInLoaded: false);
		await UITestHelper.Load(uc, x => x.IsLoaded);
		await Settle(() => uc.Bound.Text != "");
		Assert.AreEqual("Hello, World!", uc.Bound.Text, $"OBS bound={Describe(uc.Bound)}");
	}

	[TestMethod]
	public async Task B2_CodeAdded_In_Loaded()
	{
		var uc = new ENP_CodeAdded(addInLoaded: true);
		await UITestHelper.Load(uc, x => x.IsLoaded);
		await Settle(() => uc.Bound.Text != "");
		Assert.AreEqual("Hello, World!", uc.Bound.Text, $"OBS bound={Describe(uc.Bound)} children={uc.ParentSP.Children.Count}");
	}

	[TestMethod]
	public async Task B3_CodeBinding_On_Live_Target_Source_Present()
	{
		var target = new TextBlock();
		var sp = new StackPanel { Children = { new TextBlock { Name = "EnpSrcB3", Text = "b3" }, target } };
		await UITestHelper.Load(sp, x => x.IsLoaded);

		target.SetBinding(TextBlock.TextProperty, new Binding { ElementName = "EnpSrcB3", Path = new PropertyPath("Text") });
		await Settle(() => target.Text != "");
		Assert.AreEqual("b3", target.Text, $"OBS bound={Describe(target)}");
	}

	[TestMethod]
	public async Task B3b_CodeBinding_On_NotLive_Target_Source_Present()
	{
		var target = new TextBlock();
		target.SetBinding(TextBlock.TextProperty, new Binding { ElementName = "EnpSrcB3b", Path = new PropertyPath("Text") });
		var sp = new StackPanel { Children = { new TextBlock { Name = "EnpSrcB3b", Text = "b3b" }, target } };
		await UITestHelper.Load(sp, x => x.IsLoaded);
		await Settle(() => target.Text != "");
		Assert.AreEqual("b3b", target.Text, $"OBS bound={Describe(target)}");
	}

	[TestMethod]
	public async Task B4_CodeBinding_On_Live_Target_Source_Added_Later()
	{
		// Same shape as the repro test on dev/mazi/repro/19420.
		var target = new TextBlock();
		target.SetBinding(TextBlock.TextProperty, new Binding { ElementName = "EnpSrcB4", Path = new PropertyPath("Text") });
		var sp = new StackPanel { Children = { target } };
		await UITestHelper.Load(sp, x => x.IsLoaded);

		sp.Children.Add(new TextBlock { Name = "EnpSrcB4", Text = "b4" });
		await Settle(() => target.Text != "");
		Assert.AreEqual("b4", target.Text, $"OBS bound={Describe(target)}");
	}

	// ---------- Point 2: x:Load ----------

	[TestMethod]
	public async Task X1_xLoadFalse_Binding_Declared_Before()
	{
		var uc = new ENP_xLoad();
		await UITestHelper.Load(uc, x => x.IsLoaded);
		await Settle(() => uc.BoundBefore.Text != "");
		Assert.IsNotNull(uc.Deferred1, $"OBS realized={uc.Deferred1 is not null} bound={Describe(uc.BoundBefore)} control-realized={uc.Deferred3 is not null}");
		Assert.AreEqual("d1", uc.BoundBefore.Text, $"OBS realized=True bound={Describe(uc.BoundBefore)}");
	}

	[TestMethod]
	public async Task X2_xLoadFalse_Binding_Declared_After()
	{
		var uc = new ENP_xLoad();
		await UITestHelper.Load(uc, x => x.IsLoaded);
		await Settle(() => uc.BoundAfter.Text != "");
		Assert.IsNotNull(uc.Deferred2, $"OBS realized={uc.Deferred2 is not null} bound={Describe(uc.BoundAfter)} control-realized={uc.Deferred3 is not null}");
		Assert.AreEqual("d2", uc.BoundAfter.Text, $"OBS realized=True bound={Describe(uc.BoundAfter)}");
	}

	[TestMethod]
	public async Task X3_xLoadFalse_Unreferenced_Control()
	{
		var uc = new ENP_xLoad();
		await UITestHelper.Load(uc, x => x.IsLoaded);
		await Settle();
		Assert.IsNull(uc.Deferred3, "OBS unreferenced x:Load=False element was realized");
	}

	[TestMethod]
	public async Task X4_xLoad_Unload_Reload_Binding_Follows_New_Instance()
	{
		var uc = new ENP_xLoadToggle();
		await UITestHelper.Load(uc, x => x.IsLoaded);
		await Settle(() => uc.BoundToggled.Text != "");
		var initial = uc.BoundToggled.Text;
		var first = uc.Toggled;

		uc.Model.IsToggledLoaded = false;
		await Settle(() => uc.Toggled is null);
		var afterUnload = uc.BoundToggled.Text;
		var fieldAfterUnload = uc.Toggled is null ? "null" : "set";

		uc.Model.IsToggledLoaded = true;
		await Settle(() => uc.Toggled is not null);
		var second = uc.Toggled;
		if (second is not null)
		{
			second.Text = "t2";
		}
		await Settle(() => uc.BoundToggled.Text == "t2");

		Assert.AreEqual("t2", uc.BoundToggled.Text,
			$"OBS initial='{initial}' afterUnload='{afterUnload}' fieldAfterUnload={fieldAfterUnload} " +
			$"reloaded={second is not null} sameInstance={ReferenceEquals(first, second)} final={Describe(uc.BoundToggled)} firstText='{first?.Text}'");
	}

	// ---------- Point 3: scope ----------

	private static async Task<ENP_Scope> LoadScope()
	{
		var uc = new ENP_Scope();
		await UITestHelper.Load(uc, x => x.IsLoaded);
		await Settle();
		return uc;
	}

	[TestMethod]
	public async Task S01_Brush_In_UserControl_Resources()
	{
		var uc = await LoadScope();
		await Settle(() => (uc.S1.Background as SolidColorBrush)?.Color == Colors.Red);
		Assert.AreEqual(Colors.Red, (uc.S1.Background as SolidColorBrush)?.Color, $"OBS brush={Describe(uc.S1.Background)}");
	}

	[TestMethod]
	public async Task S02_Brush_In_Panel_Resources()
	{
		var uc = await LoadScope();
		await Settle(() => (uc.S2.Background as SolidColorBrush)?.Color == Colors.Red);
		Assert.AreEqual(Colors.Red, (uc.S2.Background as SolidColorBrush)?.Color, $"OBS brush={Describe(uc.S2.Background)}");
	}

	[TestMethod]
	public async Task S03_Inline_Brush()
	{
		var uc = await LoadScope();
		await Settle(() => (uc.S3.Background as SolidColorBrush)?.Color == Colors.Red);
		Assert.AreEqual(Colors.Red, (uc.S3.Background as SolidColorBrush)?.Color, $"OBS brush={Describe(uc.S3.Background)}");
	}

	[TestMethod]
	public async Task S04_Run_In_TextBlock()
	{
		var uc = await LoadScope();
		await Settle(() => uc.S4.Text == "src");
		Assert.AreEqual("src", uc.S4.Text, $"OBS text={Describe(uc.S4)}");
	}

	[TestMethod]
	public async Task S05_Button_Flyout_Content()
	{
		var uc = await LoadScope();
		var flyout = uc.S5Button.Flyout;
		try
		{
			flyout.ShowAt(uc.S5Button);
			await Settle(() => uc.S5.Text == "src");
			Assert.AreEqual("src", uc.S5.Text, $"OBS text={Describe(uc.S5)} loaded={uc.S5.IsLoaded}");
		}
		finally
		{
			flyout.Hide();
			await Settle();
		}
	}

	[TestMethod]
	public async Task S06_ToolTip_Content()
	{
		var uc = await LoadScope();
		try
		{
			uc.S6Tip.IsOpen = true;
			await Settle(() => uc.S6.Text == "src");
			Assert.AreEqual("src", uc.S6.Text, $"OBS text={Describe(uc.S6)} loaded={uc.S6.IsLoaded}");
		}
		finally
		{
			uc.S6Tip.IsOpen = false;
			await Settle();
		}
	}

	[TestMethod]
	public async Task S07_Popup_Child()
	{
		var uc = await LoadScope();
		try
		{
			uc.S7Popup.IsOpen = true;
			await Settle(() => uc.S7.Text == "src");
			Assert.AreEqual("src", uc.S7.Text, $"OBS text={Describe(uc.S7)} loaded={uc.S7.IsLoaded}");
		}
		finally
		{
			uc.S7Popup.IsOpen = false;
			await Settle();
		}
	}

	[TestMethod]
	public async Task S08_ContentControl_ContentTemplate()
	{
		var uc = await LoadScope();
		await Settle(() => FindProbe(uc.S8)?.Text == "src");
		Assert.AreEqual("src", FindProbe(uc.S8)?.Text, $"OBS text={Describe(FindProbe(uc.S8))}");
	}

	[TestMethod]
	public async Task S09_Nested_DataTemplate()
	{
		var uc = await LoadScope();
		await Settle(() => FindProbe(uc.S9)?.Text == "src");
		Assert.AreEqual("src", FindProbe(uc.S9)?.Text, $"OBS text={Describe(FindProbe(uc.S9))}");
	}

	[TestMethod]
	public async Task S10_ItemsControl_ItemTemplate()
	{
		var uc = await LoadScope();
		await Settle(() => FindProbe(uc.S10)?.Text == "src");
		Assert.AreEqual("src", FindProbe(uc.S10)?.Text, $"OBS text={Describe(FindProbe(uc.S10))}");
	}

	[TestMethod]
	public async Task S11_ListView_ItemTemplate()
	{
		var uc = await LoadScope();
		await Settle(() => FindProbe(uc.S11)?.Text == "src");
		Assert.AreEqual("src", FindProbe(uc.S11)?.Text, $"OBS text={Describe(FindProbe(uc.S11))}");
	}

	[TestMethod]
	public async Task S12_ItemsRepeater_ItemTemplate()
	{
		var uc = await LoadScope();
		await Settle(() => FindProbe(uc.S12)?.Text == "src");
		Assert.AreEqual("src", FindProbe(uc.S12)?.Text, $"OBS text={Describe(FindProbe(uc.S12))}");
	}

	// ---------- Point 4: duplicate names set in code ----------

	[TestMethod]
	public async Task D1_Duplicate_Name_Added_To_Live_Panel()
	{
		var sp = new StackPanel { Children = { new TextBlock { Name = "EnpDup", Text = "1" } } };
		await UITestHelper.Load(sp, x => x.IsLoaded);

		Exception error = null;
		try
		{
			sp.Children.Add(new TextBlock { Name = "EnpDup", Text = "2" });
			await Settle();
		}
		catch (Exception e)
		{
			error = e;
		}

		Assert.IsNull(error, $"OBS {error?.GetType().Name}: {error?.Message} (HResult 0x{error?.HResult:X8})");
	}

	[TestMethod]
	public async Task D2_Duplicate_Name_Set_On_Live_Element()
	{
		var second = new TextBlock { Text = "2" };
		var sp = new StackPanel { Children = { new TextBlock { Name = "EnpDup2", Text = "1" }, second } };
		await UITestHelper.Load(sp, x => x.IsLoaded);

		Exception error = null;
		try
		{
			second.Name = "EnpDup2";
			await Settle();
		}
		catch (Exception e)
		{
			error = e;
		}

		Assert.IsNull(error, $"OBS {error?.GetType().Name}: {error?.Message} (HResult 0x{error?.HResult:X8})");
	}
}
