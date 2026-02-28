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
		// assigned as ContextFlyout on multiple controls. When a control is removed
		// from the tree, its DataContext (ViewModel) should be collectable.

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

		// Reassign the flyout to a different control to simulate the shared pattern
		var textBox2 = new TextBox { Text = "World" };
		textBox2.ContextFlyout = sharedFlyout;

		textBox = null;
		viewModel = null;

		var collected = await TestHelper.TryWaitUntilCollected(weakViewModel);

		Assert.IsTrue(collected, "ViewModel should be collected after the control using the shared ContextFlyout is removed from the tree.");
	}

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
}
