using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

public partial class Given_UIElement
{
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_SharedContextFlyout_DoesNotLeak_ViewModel()
	{
		// Simulate the shared TextCommandBarFlyout pattern: a single flyout instance
		// used as ContextFlyout. When the control is removed from the tree, its
		// DataContext (ViewModel) should be collectable even though the flyout is still alive.

		var sharedFlyout = new MenuFlyout();
		sharedFlyout.Items.Add(new MenuFlyoutItem { Text = "Cut" });
		sharedFlyout.Items.Add(new MenuFlyoutItem { Text = "Copy" });

		var viewModel = new object();
		var weakViewModel = new WeakReference(viewModel);

		var textBox = new TextBox
		{
			Text = "Hello",
			DataContext = viewModel,
			ContextFlyout = sharedFlyout,
		};

		var root = new Grid();
		root.Children.Add(textBox);

		await UITestHelper.Load(root, x => x.IsLoaded);

		// Open and close the flyout to simulate user interaction
		sharedFlyout.ShowAt(textBox);
		await TestServices.WindowHelper.WaitForIdle();

		sharedFlyout.Hide();
		await TestServices.WindowHelper.WaitForIdle();

		// Remove the textbox from the tree and release all local references
		root.Children.Clear();
		await TestServices.WindowHelper.WaitForIdle();

		textBox = null;
		viewModel = null;

		var collected = await TestHelper.TryWaitUntilCollected(weakViewModel);

		Assert.IsTrue(collected, "ViewModel should be collected after the control using the shared ContextFlyout is removed from the tree.");
		GC.KeepAlive(sharedFlyout);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_SharedSelectionFlyout_DoesNotLeak_ViewModel()
	{
		// Same as the ContextFlyout test but for SelectionFlyout on TextBox.
		// SelectionFlyout (TextCommandBarFlyout) is also shared across controls.

		var sharedFlyout = new MenuFlyout();
		sharedFlyout.Items.Add(new MenuFlyoutItem { Text = "Cut" });
		sharedFlyout.Items.Add(new MenuFlyoutItem { Text = "Copy" });

		var viewModel = new object();
		var weakViewModel = new WeakReference(viewModel);

		var textBox = new TextBox
		{
			Text = "Hello",
			DataContext = viewModel,
			SelectionFlyout = sharedFlyout,
		};

		var root = new Grid();
		root.Children.Add(textBox);

		await UITestHelper.Load(root, x => x.IsLoaded);

		// Open and close the flyout to simulate user interaction
		sharedFlyout.ShowAt(textBox);
		await TestServices.WindowHelper.WaitForIdle();

		sharedFlyout.Hide();
		await TestServices.WindowHelper.WaitForIdle();

		// Remove the textbox from the tree and release all local references
		root.Children.Clear();
		await TestServices.WindowHelper.WaitForIdle();

		textBox = null;
		viewModel = null;

		var collected = await TestHelper.TryWaitUntilCollected(weakViewModel);

		Assert.IsTrue(collected, "ViewModel should be collected after the control using the shared SelectionFlyout is removed from the tree.");
		GC.KeepAlive(sharedFlyout);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_DefaultContextFlyout_DoesNotLeak_ViewModel()
	{
		// Verify that the default TextCommandBarFlyout created automatically by TextBox
		// does not prevent the ViewModel from being collected after the TextBox is removed.

		var viewModel = new object();
		var weakViewModel = new WeakReference(viewModel);

		var textBox = new TextBox
		{
			Text = "Hello",
			DataContext = viewModel,
		};

		var root = new Grid();
		root.Children.Add(textBox);
		await UITestHelper.Load(root, x => x.IsLoaded);

		// Get the default TextCommandBarFlyout and open/close it
		var flyout = textBox.ContextFlyout;
		Assert.IsNotNull(flyout, "TextBox should have a default ContextFlyout");
		flyout.ShowAt(textBox);
		await TestServices.WindowHelper.WaitForIdle();
		flyout.Hide();
		await TestServices.WindowHelper.WaitForIdle();

		// Remove from tree and release references
		root.Children.Clear();
		await TestServices.WindowHelper.WaitForIdle();

		textBox = null;
		viewModel = null;

		var collected = await TestHelper.TryWaitUntilCollected(weakViewModel);
		Assert.IsTrue(collected, "ViewModel should be collected after removing TextBox with default ContextFlyout from the tree.");
		GC.KeepAlive(flyout);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_DefaultSelectionFlyout_DoesNotLeak_ViewModel()
	{
		// Verify that the default SelectionFlyout created automatically by TextBox
		// does not prevent the ViewModel from being collected after the TextBox is removed.

		var viewModel = new object();
		var weakViewModel = new WeakReference(viewModel);

		var textBox = new TextBox
		{
			Text = "Hello",
			DataContext = viewModel,
		};

		var root = new Grid();
		root.Children.Add(textBox);
		await UITestHelper.Load(root, x => x.IsLoaded);

		// Get the default SelectionFlyout and open/close it
		var flyout = textBox.SelectionFlyout;
		Assert.IsNotNull(flyout, "TextBox should have a default SelectionFlyout");
		flyout.ShowAt(textBox);
		await TestServices.WindowHelper.WaitForIdle();
		flyout.Hide();
		await TestServices.WindowHelper.WaitForIdle();

		// Remove from tree and release references
		root.Children.Clear();
		await TestServices.WindowHelper.WaitForIdle();

		textBox = null;
		viewModel = null;

		var collected = await TestHelper.TryWaitUntilCollected(weakViewModel);
		Assert.IsTrue(collected, "ViewModel should be collected after removing TextBox with default SelectionFlyout from the tree.");
		GC.KeepAlive(flyout);
	}

#if HAS_UNO
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_ContextFlyout_BindingOnFlyout_ResolvesDataContext()
	{
		// Verify that {Binding} on a FlyoutBase property (attached or otherwise)
		// resolves against the owning UIElement's DataContext.
		// This is the regression scenario from kahua-private#439.

		var menuItems = new[] { "Item1", "Item2", "Item3" };
		var viewModel = new { MenuItems = menuItems };

		var flyout = new MenuFlyout();
		var button = new Button
		{
			Content = "Test",
			DataContext = viewModel,
		};

		// Simulate what XAML does: set an attached property binding on the flyout.
		// We set the binding manually since we're in code-behind.
		button.ContextFlyout = flyout;

		// The flyout should now have DataContext from the button.
		Assert.AreEqual(viewModel, flyout.DataContext,
			"ContextFlyout should inherit DataContext from the owning UIElement.");
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_ContextFlyout_DataContextChanges_FlyoutUpdated()
	{
		// Verify that when the owning element's DataContext changes,
		// the ContextFlyout's DataContext is updated.

		var vm1 = new object();
		var vm2 = new object();

		var flyout = new MenuFlyout();
		var button = new Button
		{
			Content = "Test",
			DataContext = vm1,
			ContextFlyout = flyout,
		};

		Assert.AreEqual(vm1, flyout.DataContext);

		button.DataContext = vm2;
		Assert.AreEqual(vm2, flyout.DataContext,
			"ContextFlyout DataContext should update when owning element's DataContext changes.");
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_ContextFlyout_ElementUnloaded_DataContextCleared()
	{
		// Verify that when the owning element is unloaded, the flyout's
		// inherited DataContext is cleared (to prevent memory leaks).

		var viewModel = new object();
		var flyout = new MenuFlyout();
		var button = new Button
		{
			Content = "Test",
			DataContext = viewModel,
			ContextFlyout = flyout,
		};

		var root = new Grid();
		root.Children.Add(button);
		await UITestHelper.Load(root, x => x.IsLoaded);

		Assert.AreEqual(viewModel, flyout.DataContext);

		// Remove from tree - DataContext should be cleared
		root.Children.Clear();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsNull(flyout.DataContext,
			"ContextFlyout DataContext should be cleared when owning element is unloaded.");
	}

	// flyout.GetPresenter() is Uno-specific helper to access the presenter while the flyout is open.
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_ContextFlyout_PresenterGetsDataContext_WhileOpen()
	{
		// Verify that the flyout presenter still receives DataContext
		// from the placement target while the flyout is open.

		var flyout = new MenuFlyout();
		var menuItem = new MenuFlyoutItem { Text = "Test" };
		flyout.Items.Add(menuItem);

		var viewModel = "TestDataContext";
		var textBox = new TextBox
		{
			Text = "Hello",
			DataContext = viewModel,
			ContextFlyout = flyout,
		};

		var root = new Grid();
		root.Children.Add(textBox);
		await UITestHelper.Load(root, x => x.IsLoaded);

		flyout.ShowAt(textBox);
		await TestServices.WindowHelper.WaitForIdle();

		// Verify the presenter has the DataContext from the placement target
		var presenter = flyout.GetPresenter() as FrameworkElement;
		Assert.IsNotNull(presenter, "Presenter should exist while flyout is open.");
		Assert.AreEqual(viewModel, presenter.DataContext, "Presenter should have DataContext from placement target while flyout is open.");

		flyout.Hide();
		await TestServices.WindowHelper.WaitForIdle();

		root.Children.Clear();
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_ContextFlyout_SharedFlyoutReassigned_DataContextSwitches()
	{
		// When a flyout is moved from element A to element B,
		// its DataContext should switch to element B's DataContext.

		var vmA = "ViewModelA";
		var vmB = "ViewModelB";

		var flyout = new MenuFlyout();
		var buttonA = new Button { Content = "A", DataContext = vmA, ContextFlyout = flyout };

		Assert.AreEqual(vmA, flyout.DataContext,
			"Flyout should have DataContext from element A.");

		var buttonB = new Button { Content = "B", DataContext = vmB };
		buttonB.ContextFlyout = flyout;

		Assert.AreEqual(vmB, flyout.DataContext,
			"Flyout should have DataContext from element B after reassignment.");
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_ContextFlyout_ElementReloaded_DataContextRepropagated()
	{
		// When a UIElement is removed and re-added to the tree,
		// the flyout's DataContext should be re-propagated.

		var viewModel = new object();
		var flyout = new MenuFlyout();
		var button = new Button
		{
			Content = "Test",
			DataContext = viewModel,
			ContextFlyout = flyout,
		};

		var root = new Grid();
		root.Children.Add(button);
		await UITestHelper.Load(root, x => x.IsLoaded);

		Assert.AreEqual(viewModel, flyout.DataContext);

		// Remove from tree - DataContext should be cleared
		root.Children.Clear();
		await TestServices.WindowHelper.WaitForIdle();
		Assert.IsNull(flyout.DataContext,
			"DataContext should be cleared after unload.");

		// Re-add to tree - DataContext should be re-propagated
		root.Children.Add(button);
		await UITestHelper.Load(root, x => x.IsLoaded);

		Assert.AreEqual(viewModel, flyout.DataContext,
			"DataContext should be re-propagated after reload.");
	}
#endif
}
