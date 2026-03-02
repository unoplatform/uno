using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

partial class Given_UIElement
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
	}

#if HAS_UNO // flyout.GetPresenter() is Uno-specific helper to access the presenter while the flyout is open.
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
#endif
}
