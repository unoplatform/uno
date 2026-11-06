#if !WINAPPSDK // Uses Uno internals.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.MUX.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_Flyout_Lifecycle
{
	private static async Task<(Button target, Flyout flyout, List<string> log)> Setup(string name = "A", Button target = null)
	{
		target ??= new Button { Content = name };
		if (target.Parent is null)
		{
			await UITestHelper.Load(target);
		}

		var flyout = new Flyout { Content = new Border { Width = 50, Height = 50 } };
		List<string> log = new();
		Track(flyout, name, log);
		return (target, flyout, log);
	}

	private static void Track(FlyoutBase flyout, string name, List<string> log)
	{
		flyout.Opening += (_, _) => log.Add($"opening{name}");
		flyout.Opened += (_, _) => log.Add($"opened{name}");
		flyout.Closing += (_, _) => log.Add($"closing{name}");
		flyout.Closed += (_, _) => log.Add($"closed{name}");
	}

	[TestMethod]
	public async Task When_Hide_Then_Closed_Is_Raised_After_Hide_Returns()
	{
		var (target, flyout, log) = await Setup();
		try
		{
			flyout.ShowAt(target);
			await TestServices.WindowHelper.WaitForIdle();

			log.Clear();
			flyout.Hide();

			CollectionAssert.AreEqual(new[] { "closingA" }, log);
			Assert.IsFalse(flyout.IsOpen);

			await TestServices.WindowHelper.WaitForIdle();

			CollectionAssert.AreEqual(new[] { "closingA", "closedA" }, log);
		}
		finally
		{
			VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
		}
	}

	[TestMethod]
	public async Task When_Opened_Then_Presenter_Is_Loaded_And_Focused()
	{
		var (target, flyout, _) = await Setup();
		try
		{
			bool? wasLoaded = null;
			flyout.Opened += (_, _) => wasLoaded = flyout.GetPresenter()?.IsLoaded;

			flyout.ShowAt(target);

			Assert.IsTrue(flyout.IsOpen);
			Assert.IsNull(wasLoaded);

			await TestServices.WindowHelper.WaitFor(() => wasLoaded is not null);

			Assert.IsTrue(wasLoaded);
		}
		finally
		{
			VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
		}
	}

	[TestMethod]
	public async Task When_Closed_Then_Target_Is_Cleared()
	{
		var (target, flyout, _) = await Setup();
		try
		{
			flyout.ShowAt(target);
			await TestServices.WindowHelper.WaitForIdle();
			Assert.AreSame(target, flyout.Target);

			flyout.Hide();
			Assert.AreSame(target, flyout.Target);

			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsNull(flyout.Target);
		}
		finally
		{
			VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
		}
	}

	[TestMethod]
	public async Task When_Show_While_Another_Open_Then_First_Closes_Before_Second_Opens()
	{
		var panel = new StackPanel();
		var targetA = new Button { Content = "A" };
		var targetB = new Button { Content = "B" };
		panel.Children.Add(targetA);
		panel.Children.Add(targetB);
		await UITestHelper.Load(panel);

		var flyoutA = new Flyout { Content = new Border { Width = 50, Height = 50 } };
		var flyoutB = new Flyout { Content = new Border { Width = 50, Height = 50 } };
		List<string> log = new();
		Track(flyoutA, "A", log);
		Track(flyoutB, "B", log);

		try
		{
			flyoutA.ShowAt(targetA);
			await TestServices.WindowHelper.WaitForIdle();
			log.Clear();

			flyoutB.ShowAt(targetB);
			Assert.IsFalse(flyoutB.IsOpen, "The second flyout is staged until the first one has closed.");
			await TestServices.WindowHelper.WaitForIdle();

			CollectionAssert.AreEqual(new[] { "closingA", "closedA", "openingB", "openedB" }, log);
			Assert.IsFalse(flyoutA.IsOpen);
			Assert.IsTrue(flyoutB.IsOpen);
		}
		finally
		{
			VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
		}
	}

	// FlyoutBase_partial.cpp, CheckAndHandleOpenFlyout: the metadata still has the flyout as open until its presenter
	// unloads, so a ShowAt at the same target and position is ignored and the flyout ends up closed.
	[TestMethod]
	public async Task When_Hide_And_Show_At_Same_Target_In_The_Same_Tick_Then_Stays_Closed()
	{
		var (target, flyout, log) = await Setup();
		try
		{
			flyout.ShowAt(target);
			await TestServices.WindowHelper.WaitForIdle();
			log.Clear();

			flyout.Hide();
			flyout.ShowAt(target);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsFalse(flyout.IsOpen);
			CollectionAssert.AreEqual(new[] { "closingA", "closedA" }, log);
		}
		finally
		{
			VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
		}
	}

	[TestMethod]
	public async Task When_Shown_At_Other_Target_While_Open_Then_Reopens()
	{
		var panel = new StackPanel();
		var targetA = new Button { Content = "A" };
		var targetB = new Button { Content = "B" };
		panel.Children.Add(targetA);
		panel.Children.Add(targetB);
		await UITestHelper.Load(panel);

		var flyout = new Flyout { Content = new Border { Width = 50, Height = 50 } };
		List<string> log = new();
		Track(flyout, "A", log);

		try
		{
			flyout.ShowAt(targetA);
			await TestServices.WindowHelper.WaitForIdle();
			log.Clear();

			flyout.ShowAt(targetB);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsTrue(flyout.IsOpen);
			Assert.AreSame(targetB, flyout.Target);
			CollectionAssert.AreEqual(new[] { "closingA", "closedA", "openingA", "openedA" }, log);
		}
		finally
		{
			VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
		}
	}

	[TestMethod]
	public async Task When_Show_And_Hide_Before_Loaded_Then_Closes_And_Can_Reopen()
	{
		var (target, flyout, log) = await Setup();
		try
		{
			flyout.ShowAt(target);
			flyout.Hide();
			await TestServices.WindowHelper.WaitForIdle();

			CollectionAssert.AreEqual(new[] { "openingA", "closingA", "closedA" }, log);

			flyout.ShowAt(target);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsTrue(flyout.IsOpen);
		}
		finally
		{
			VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
		}
	}

	[TestMethod]
	public async Task When_Parent_Hides_Then_Child_Chain_Closes_Innermost_First()
	{
		var innerTarget = new Button { Content = "inner", Width = 50, Height = 50 };
		var (target, flyout, log) = await Setup("P");
		flyout.Content = innerTarget;
		var child = new Flyout { Content = new Border { Width = 50, Height = 50 } };
		Track(child, "C", log);

		try
		{
			flyout.ShowAt(target);
			await TestServices.WindowHelper.WaitForLoaded(innerTarget);

			child.ShowAt(innerTarget);
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsTrue(flyout.IsOpen, "A child flyout must not close its parent.");
			Assert.IsTrue(child.IsOpen);

			log.Clear();
			flyout.Hide();
			await TestServices.WindowHelper.WaitForIdle();

			CollectionAssert.AreEqual(new[] { "closingP", "closingC", "closedC", "closedP" }, log);
		}
		finally
		{
			VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24422")]
	public async Task When_Target_RightToLeft_Then_Presenter_And_Popup_Follow()
	{
		var target = new Button { Content = "target", HorizontalAlignment = HorizontalAlignment.Center };
		var root = new Grid { FlowDirection = FlowDirection.RightToLeft };
		root.Children.Add(target);
		await UITestHelper.Load(root);

		foreach (FlyoutBase flyout in new FlyoutBase[]
		{
			new Flyout { Content = new Border { Width = 50, Height = 50 } },
			new MenuFlyout { Items = { new MenuFlyoutItem { Text = "item" } } },
		})
		{
			try
			{
				flyout.ShowAt(target);
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual(FlowDirection.RightToLeft, flyout.GetPresenter().FlowDirection);
				Assert.AreEqual(flyout is MenuFlyout ? FlowDirection.LeftToRight : FlowDirection.RightToLeft, flyout._popup.FlowDirection); // See Given_MenuFlyout_Windowing.
			}
			finally
			{
				flyout.Hide();
				await TestServices.WindowHelper.WaitForIdle();
			}
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23588")]
	public async Task When_DatePicker_In_Flyout_Then_Can_Be_Used_Again_After_Reopen()
	{
		var datePicker = new DatePicker { UseNativeStyle = false };
		var flyout = new Flyout { Content = datePicker };
		var target = new Button { Content = "target", Flyout = flyout };
		await UITestHelper.Load(target);

		try
		{
			for (var i = 0; i < 2; i++)
			{
				flyout.ShowAt(target);
				await TestServices.WindowHelper.WaitForLoaded(datePicker);

				await DateTimePickerHelper.OpenDateTimePicker(datePicker);
				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsTrue(flyout.IsOpen, $"Pass {i}: the date picker's flyout is a child of the flyout.");
				Assert.HasCount(2, FlyoutBase.OpenFlyouts, $"Pass {i}");
				Assert.IsInstanceOfType(FlyoutBase.OpenFlyouts[1], typeof(DatePickerFlyout));

				FlyoutBase.OpenFlyouts[1].Hide();
				await TestServices.WindowHelper.WaitForIdle();
				Assert.IsTrue(flyout.IsOpen, $"Pass {i}: closing the date picker's flyout leaves the flyout open.");

				flyout.Hide();
				await TestServices.WindowHelper.WaitForIdle();
				Assert.IsEmpty(FlyoutBase.OpenFlyouts);
			}
		}
		finally
		{
			VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19404")]
	public async Task When_CommandBar_MenuFlyout_Bottom_Then_Below_The_Button()
	{
		var menu = new MenuFlyout { Placement = FlyoutPlacementMode.Bottom };
		menu.Items.Add(new MenuFlyoutItem { Text = "Connect" });
		var button = new AppBarButton { Label = "File", Flyout = menu };
		var commandBar = new CommandBar { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, DefaultLabelPosition = CommandBarDefaultLabelPosition.Right };
		commandBar.PrimaryCommands.Add(button);
		await UITestHelper.Load(commandBar);

		try
		{
			menu.ShowAt(button);
			await TestServices.WindowHelper.WaitForIdle();

			var buttonBounds = button.GetAbsoluteBounds();
			var presenterBounds = menu.GetPresenter().GetAbsoluteBounds();

			Assert.IsGreaterThanOrEqualTo(buttonBounds.Bottom - 1, presenterBounds.Top, "The menu opens below the button.");
		}
		finally
		{
			menu.Hide();
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task When_Transient_Then_Presenter_Key_Events_Route_To_Target()
	{
		var (target, flyout, _) = await Setup();
		try
		{
			flyout.ShowMode = FlyoutShowMode.Transient;
			flyout.ShowAt(target);
			await TestServices.WindowHelper.WaitForIdle();

			var presenter = (FlyoutPresenter)flyout.GetPresenter();

			Assert.AreSame(target, presenter.GetTargetIfOpenedAsTransient());

			flyout.ShowMode = FlyoutShowMode.Standard;

			Assert.IsNull(presenter.GetTargetIfOpenedAsTransient());
		}
		finally
		{
			VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
		}
	}
}
#endif
