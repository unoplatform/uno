#if __SKIA__
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI.Input.Preview.Injection;
using Uno.UI.Toolkit.DevTools.Input;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Input;

/// <summary>
/// Regression coverage for https://github.com/unoplatform/uno/issues/23440:
/// when a ContextFlyout is set on both a child and an ancestor, triggering the context
/// menu on the child must open only the innermost flyout. The ContextRequested event must
/// stop at the first element that shows a flyout (which sets Handled=true) and never bubble
/// on to also open an ancestor's flyout. These tests exercise the real input path (gesture
/// recognizer -> ContextRequested) across several tree shapes, which the direct-RaiseEvent
/// tests in <see cref="Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_UIElement_ContextFlyoutParentChain"/> do not cover.
/// </summary>
public partial class Given_ContextRequested_Injection
{
	private static (MenuFlyout flyout, Func<int> count) MakeCountingFlyout(string text)
	{
		var opened = 0;
		var f = new MenuFlyout();
		f.Items.Add(new MenuFlyoutItem { Text = text });
		f.Opened += (s, e) => opened++;
		return (f, () => opened);
	}

	private static Point CenterOf(Rect r) => new(r.X + r.Width / 2, r.Y + r.Height / 2);

	private static async Task RightClickCenter(FrameworkElement target)
	{
		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init InputInjector");
		using var mouse = injector.GetMouse();
		mouse.PressRight(CenterOf(target.GetAbsoluteBounds()));
		mouse.ReleaseRight();
		await TestServices.WindowHelper.WaitForIdle();
		await Task.Delay(150);
	}

	// Both child and parent are non-Control elements (Border in Border).
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23440")]
	public async Task When_RightClick_Child_Border_In_Border_Only_Child_Opens()
	{
		var (parentFlyout, parentCount) = MakeCountingFlyout("Parent");
		var (childFlyout, childCount) = MakeCountingFlyout("Child");

		var child = new Border { Width = 80, Height = 40, Background = new SolidColorBrush(Microsoft.UI.Colors.Yellow), ContextFlyout = childFlyout };
		var parent = new Border { Child = child, ContextFlyout = parentFlyout, Width = 200, Height = 100, Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray) };

		await UITestHelper.Load(parent);
		try
		{
			await RightClickCenter(child);
			Assert.AreEqual(1, childCount(), "Child's ContextFlyout should open exactly once");
			Assert.AreEqual(0, parentCount(), "Parent's ContextFlyout must NOT open when the child has one");
		}
		finally
		{
			childFlyout.Hide();
			parentFlyout.Hide();
		}
	}

	// Parent (flyout) -> intermediate panel without a flyout -> child (flyout).
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23440")]
	public async Task When_RightClick_Child_Through_Intermediate_Panel_Only_Child_Opens()
	{
		var (parentFlyout, parentCount) = MakeCountingFlyout("Parent");
		var (childFlyout, childCount) = MakeCountingFlyout("Child");

		var child = new Border { Width = 80, Height = 40, Background = new SolidColorBrush(Microsoft.UI.Colors.Yellow), ContextFlyout = childFlyout };
		var intermediate = new StackPanel();
		intermediate.Children.Add(child);
		var parent = new Border { Child = intermediate, ContextFlyout = parentFlyout, Width = 200, Height = 100, Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray) };

		await UITestHelper.Load(parent);
		try
		{
			await RightClickCenter(child);
			Assert.AreEqual(1, childCount(), "Child's ContextFlyout should open exactly once");
			Assert.AreEqual(0, parentCount(), "Ancestor's ContextFlyout must NOT open across an intermediate panel");
		}
		finally
		{
			childFlyout.Hide();
			parentFlyout.Hide();
		}
	}

	// "File explorer" shape: an item with its own ContextFlyout inside a ListView that also has one.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23440")]
	public async Task When_RightClick_ListViewItem_With_Flyout_ListView_Flyout_Not_Shown()
	{
		var (listFlyout, listCount) = MakeCountingFlyout("List");
		var (itemFlyout, itemCount) = MakeCountingFlyout("Item");

		var item = new ListViewItem { Content = "Item 0", ContextFlyout = itemFlyout, Width = 180, Height = 40 };
		var lv = new ListView { ContextFlyout = listFlyout, Width = 200, Height = 200 };
		lv.Items.Add(item);

		await UITestHelper.Load(lv);
		await TestServices.WindowHelper.WaitForIdle();
		try
		{
			await RightClickCenter(item);
			Assert.AreEqual(1, itemCount(), "Item's ContextFlyout should open exactly once");
			Assert.AreEqual(0, listCount(), "ListView's ContextFlyout must NOT open when the item has one");
		}
		finally
		{
			itemFlyout.Hide();
			listFlyout.Hide();
		}
	}

	// Child inside a ScrollViewer (captures the pointer for potential panning) with an ancestor flyout.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23440")]
	public async Task When_RightClick_Child_In_ScrollViewer_Only_Child_Opens()
	{
		var (parentFlyout, parentCount) = MakeCountingFlyout("Parent");
		var (childFlyout, childCount) = MakeCountingFlyout("Child");

		var child = new Border { Width = 120, Height = 40, Background = new SolidColorBrush(Microsoft.UI.Colors.Yellow), ContextFlyout = childFlyout };
		var sv = new ScrollViewer { Content = child, Width = 150, Height = 60 };
		var parent = new Grid { ContextFlyout = parentFlyout, Width = 200, Height = 120, Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray) };
		parent.Children.Add(sv);

		await UITestHelper.Load(parent);
		await TestServices.WindowHelper.WaitForIdle();
		try
		{
			await RightClickCenter(child);
			Assert.AreEqual(1, childCount(), "Child's ContextFlyout should open exactly once");
			Assert.AreEqual(0, parentCount(), "Ancestor's ContextFlyout must NOT open when the child is inside a capturing ScrollViewer");
		}
		finally
		{
			childFlyout.Hide();
			parentFlyout.Hide();
		}
	}

	// Touch long-press (Holding) variant, mirroring the linked issue #22229.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23440")]
	public async Task When_TouchHold_Child_In_Parent_Only_Child_Opens()
	{
		var (parentFlyout, parentCount) = MakeCountingFlyout("Parent");
		var (childFlyout, childCount) = MakeCountingFlyout("Child");

		var child = new Border { Width = 80, Height = 40, Background = new SolidColorBrush(Microsoft.UI.Colors.Yellow), ContextFlyout = childFlyout };
		var parent = new Border { Child = child, ContextFlyout = parentFlyout, Width = 200, Height = 100, Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray) };

		await UITestHelper.Load(parent);
		try
		{
			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init InputInjector");
			using var finger = injector.GetFinger();
			finger.Press(CenterOf(child.GetAbsoluteBounds()));
			// Hold long enough to cross the Holding-gesture threshold.
			await Task.Delay(1200);
			finger.Release();
			await TestServices.WindowHelper.WaitForIdle();
			await Task.Delay(150);

			Assert.AreEqual(1, childCount(), "Child's ContextFlyout should open exactly once on touch-hold");
			Assert.AreEqual(0, parentCount(), "Parent's ContextFlyout must NOT open on touch-hold when the child has one");
		}
		finally
		{
			childFlyout.Hide();
			parentFlyout.Hide();
		}
	}
}
#endif
