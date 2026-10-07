#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using SampleControl.Presentation;
using Uno.UI.RuntimeTests;
using Uno.UI.Samples.Helper;
using Windows.Foundation;

namespace SamplesApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_ShellLayout
{
	private Rect? _testHostBounds;

	[TestInitialize]
	public void RecordTestHost() => _testHostBounds = GetTestHostBounds();

	// These tests change the live shell around the runner, so each must hand the next one its test host back unmoved.
	[TestCleanup]
	public async Task AssertTestHostRestored()
	{
		if (_testHostBounds is not { } before)
		{
			return;
		}

		bool IsBack() => GetTestHostBounds() is { } now
			&& Math.Abs(now.X - before.X) <= 0.5
			&& Math.Abs(now.Y - before.Y) <= 0.5
			&& Math.Abs(now.Width - before.Width) <= 0.5;

		// WinUI still runs pane and chrome transitions after WaitForIdle.
		var waited = Stopwatch.StartNew();
		while (!IsBack() && waited.ElapsedMilliseconds < 3000)
		{
			await TestServices.WindowHelper.WaitForIdle();
			await Task.Delay(50);
		}

		Assert.IsTrue(IsBack(), $"The test host was at {before} and is now at {GetTestHostBounds()}.");
	}

	private static Rect? GetTestHostBounds()
	{
		if (TestServices.WindowHelper.EmbeddedTestRoot.control is not FrameworkElement { XamlRoot: not null } host)
		{
			return null;
		}

		return host.TransformToVisual(null).TransformBounds(new Rect(0, 0, host.ActualWidth, 0));
	}

	[TestMethod]
	public void When_Shell_Loaded_Control_Fills_Window()
	{
		var owner = SampleChooserViewModel.Instance.Owner;
		var bounds = owner.TransformToVisual(null).TransformBounds(new Rect(0, 0, owner.ActualWidth, owner.ActualHeight));

		Assert.AreEqual(new Point(0, 0), new Point(bounds.X, bounds.Y));
		Assert.AreEqual(owner.XamlRoot!.Size.Width, bounds.Width, 0.5);
		Assert.AreEqual(owner.XamlRoot.Size.Height, bounds.Height, 0.5);
	}

	[TestMethod]
	public void When_Automation_Run_Sample_Keeps_Toolbar_Offset()
	{
		var vm = SampleChooserViewModel.Instance;
		if (!vm.IsAutomationRun)
		{
			Assert.Inconclusive("Only meaningful when launched with --runtime-tests.");
		}

		var host = (FrameworkElement)vm.Owner.FindName("SampleHost");
		var origin = host.TransformToVisual(vm.Owner).TransformPoint(new Point(0, 0));
		var headerHeight = (double)vm.Owner.Resources["ShellHeaderHeight"];

		// Runtime tests and screenshots expect the sample 56 DIP down, with nothing to its left.
		Assert.AreEqual(0, origin.X, 0.5);
		Assert.AreEqual(vm.TitleBarVisualHeight + headerHeight, origin.Y, 0.5);
		Assert.AreEqual(56d, headerHeight);
	}

	[TestMethod]
	public async Task When_Destination_Changes_Rail_Selection_Follows()
	{
		var vm = SampleChooserViewModel.Instance;
		var rail = (NavigationView)vm.Owner.FindName("ShellRail");
		var wasAutomation = vm.IsAutomationRun;
		var wasBrowserView = vm.BrowserView;
		var wasSplitVisible = vm.IsSplitVisible;

		try
		{
			// The rail reports no selection while automation hides its pane, so leave automation briefly.
			vm.IsShellPersistenceSuspended = true;
			vm.IsAutomationRun = false;
			await TestServices.WindowHelper.WaitForIdle();

			if (!rail.IsPaneVisible)
			{
				Assert.Inconclusive("Narrow windows have no rail; the browser pane lists its destinations.");
			}

			Assert.AreEqual(ShellDestination.RuntimeTests, vm.ShellDestination);
			Assert.AreSame(vm.Owner.FindName("ShellRailRuntimeTests"), rail.SelectedItem);

			// Settings opens in the browser pane, so the runner stays loaded behind it.
			vm.ShowSettingsCommand.Execute(null);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(ShellDestination.Settings, vm.ShellDestination);
			Assert.AreSame(rail.SettingsItem, rail.SelectedItem);
		}
		finally
		{
			vm.BrowserView = wasBrowserView;
			vm.IsSplitVisible = wasSplitVisible;
			vm.IsAutomationRun = wasAutomation;
			vm.IsShellPersistenceSuspended = false;
			await TestServices.WindowHelper.WaitForIdle();
		}

		Assert.AreEqual(ShellDestination.RuntimeTests, vm.ShellDestination);
		Assert.AreEqual(!wasAutomation, rail.IsPaneVisible);
	}

	[TestMethod]
	public async Task When_Browser_Toggled_Pane_And_Toggle_Follow_View_Model()
	{
		var vm = SampleChooserViewModel.Instance;
		var splitView = (SplitView)vm.Owner.FindName("SplitView");
		var toggle = (ToggleButton)vm.Owner.FindName("ShellBrowserToggle");
		var wasSplitVisible = vm.IsSplitVisible;

		try
		{
			vm.IsSplitVisible = true;
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsTrue(splitView.IsPaneOpen);
			Assert.IsTrue(toggle.IsChecked);

			vm.IsSplitVisible = false;
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsFalse(splitView.IsPaneOpen);
			Assert.IsFalse(toggle.IsChecked);
		}
		finally
		{
			vm.IsSplitVisible = wasSplitVisible;
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

#if HAS_UNO
	[TestMethod]
	public async Task When_Touch_Simulated_Pane_Rows_Use_Touch_Height()
	{
		var vm = SampleChooserViewModel.Instance;
		var pane = (FrameworkElement)vm.Owner.FindName("ShellBrowserPane");
		var lists = new[] { "ShellCategoriesList", "ShellSamplesList" }.Select(name => (ListView)vm.Owner.FindName(name)).ToArray();
		var wasTouch = vm.SimulateTouch;
		var wasPaneOpen = vm.IsSplitVisible;
		var expectedDefault = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() ? 40d : 32d;

		IEnumerable<ListViewItem> RealizedRows() => lists.SelectMany(list => list.ItemsPanelRoot?.Children.OfType<ListViewItem>() ?? []);

		void AssertRowHeight(double expected)
		{
			Assert.AreEqual(expected, (double)pane.Resources["ListViewItemMinHeight"]);

			var rows = RealizedRows().ToArray();
			Assert.IsTrue(rows.Length > 0, "No pane rows were realized.");
			foreach (var item in rows)
			{
				Assert.AreEqual(expected, item.MinHeight);
			}
		}

		try
		{
			// Under --runtime-tests the pane starts closed and its lists have no containers.
			vm.IsSplitVisible = true;
			await TestServices.WindowHelper.WaitFor(() => RealizedRows().Any(), timeoutMS: 5000);

			vm.SimulateTouch = true;
			await TestServices.WindowHelper.WaitForIdle();
			AssertRowHeight(40d);

			vm.SimulateTouch = false;
			await TestServices.WindowHelper.WaitForIdle();
			AssertRowHeight(expectedDefault);
		}
		finally
		{
			vm.SimulateTouch = wasTouch;
			vm.IsSplitVisible = wasPaneOpen;
			await TestServices.WindowHelper.WaitForIdle();
		}
	}
#endif

	[TestMethod]
	public async Task When_Theme_Changes_Current_Pane_Destination_Follows()
	{
		var vm = SampleChooserViewModel.Instance;
		var owner = vm.Owner;
		var buttons = new[] { "ShellPaneHomeButton", "ShellPaneRuntimeTestsButton", "ShellPaneBenchmarksButton", "ShellPanePlaygroundButton", "ShellPaneHelpButton", "ShellPaneSettingsButton" }
			.Select(name => (Button)owner.FindName(name))
			.ToArray();
		var current = buttons.FirstOrDefault(b => Equals(b.Tag, vm.ShellDestination.ToString()));
		if (current is null)
		{
			Assert.Inconclusive($"No pane button for {vm.ShellDestination}.");
		}

		var wasAutomation = vm.IsAutomationRun;
		var wasLight = vm.IsAppThemeLight;
		var wasDark = vm.IsAppThemeDark;
		try
		{
			// Keeps the theme flip out of the persisted settings.
			vm.IsAutomationRun = true;

			foreach (var dark in new[] { true, false })
			{
				if (dark)
				{
					vm.IsAppThemeDark = true;
				}
				else
				{
					vm.IsAppThemeLight = true;
				}

				await TestServices.WindowHelper.WaitForIdle();

				var expectedTheme = dark ? ElementTheme.Dark : ElementTheme.Light;
				Assert.AreEqual(expectedTheme, current.ActualTheme);
				Assert.AreSame(ShellThemeBrushes.Get("SubtleFillColorSecondaryBrush", expectedTheme), current.Background);
				Assert.AreEqual("Current", AutomationProperties.GetItemStatus(current));
				foreach (var other in buttons.Where(b => b != current))
				{
					Assert.IsTrue(string.IsNullOrEmpty(AutomationProperties.GetItemStatus(other)));
				}
			}
		}
		finally
		{
			if (wasLight)
			{
				vm.IsAppThemeLight = true;
			}
			else if (wasDark)
			{
				vm.IsAppThemeDark = true;
			}
			else
			{
				vm.IsAppThemeSystem = true;
			}

			vm.IsAutomationRun = wasAutomation;
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task When_IsFavoritedSample_Set_Favorite_Is_Toggled()
	{
		var vm = SampleChooserViewModel.Instance;
		var sample = vm.CurrentSelectedSample;
		Assert.IsNotNull(sample);

		var initial = vm.IsFavoritedSample;
		try
		{
			vm.IsFavoritedSample = !initial;
			await TestServices.WindowHelper.WaitFor(() => (vm.FavoriteSamples?.Contains(sample) ?? false) == !initial, timeoutMS: 5000);

			Assert.AreEqual(!initial, vm.IsFavoritedSample);
		}
		finally
		{
			vm.IsFavoritedSample = initial;
			await TestServices.WindowHelper.WaitFor(() => (vm.FavoriteSamples?.Contains(sample) ?? false) == initial, timeoutMS: 5000);
		}

		Assert.AreEqual(initial, vm.IsFavoritedSample);
	}

	[TestMethod]
	public async Task When_IsFavoritedSample_Toggled_Twice_Quickly_Last_Request_Wins()
	{
		var vm = SampleChooserViewModel.Instance;
		var sample = vm.CurrentSelectedSample;
		Assert.IsNotNull(sample);

		var initial = vm.IsFavoritedSample;
		bool IsPersisted() => vm.FavoriteSamples?.Contains(sample) ?? false;

		try
		{
			// The second set lands while the first write is still in flight.
			vm.IsFavoritedSample = !initial;
			vm.IsFavoritedSample = initial;

			await TestServices.WindowHelper.WaitFor(() => !vm.IsPersistingFavorite, timeoutMS: 5000);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(initial, IsPersisted());
			Assert.AreEqual(initial, vm.IsFavoritedSample);
		}
		finally
		{
			if (IsPersisted() != initial)
			{
				vm.IsFavoritedSample = initial;
				await TestServices.WindowHelper.WaitFor(() => IsPersisted() == initial, timeoutMS: 5000);
			}
		}
	}
}
