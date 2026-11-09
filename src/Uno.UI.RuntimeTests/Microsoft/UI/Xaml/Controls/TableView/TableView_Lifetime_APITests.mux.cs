// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\APITests\TableView_Lifetime_APITests.cs, tag winui3/main, commit 79209e041

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using Microsoft.UI.Xaml.Controls.Tabular;
using MUXControlsTestApp.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Common;
using Private.Infrastructure;


using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewRowTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewLifetimeTestHelpers;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;

// Each test keeps the would-be rooter (source, items, peers) alive; dropping both would prove nothing.
[TestClass]
public class TableViewLifetimeTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies a TableView is collected after it is unloaded, while the items source it was bound to stays alive.")]
	public async Task VerifyTableViewIsCollectedWhileItsItemsSourceOutlivesIt()
	{
		var items = new ObservableCollection<Person>(MakeManyItems(40));
		var tracked = new Dictionary<string, WeakReference>();

		RunOnUIThread.Execute(() => CreateAndLoadTable(items, tracked));

		await SettleAfterLoad();

		RunOnUIThread.Execute(() => Content = null);

		await AssertCollected(tracked);

		Verify.AreEqual(40, items.Count, "The items source must still be alive after the table was collected.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a TableView bound through a shaped source with live sort-key tracking is still collected after unload.")]
	public async Task VerifyTableViewIsCollectedAfterShapedSourceWithLiveTracking()
	{
		var items = new ObservableCollection<NotifyingPerson>(
			Enumerable.Range(0, 40).Select(i => new NotifyingPerson { Name = $"Person {i}", Role = $"Role {i % 5}" }));

		var tracked = new Dictionary<string, WeakReference>();

		RunOnUIThread.Execute(() => CreateAndLoadShapedTable(items, tracked));

		await SettleAfterLoad();

		RunOnUIThread.Execute(() => Content = null);

		await AssertCollected(tracked);

		// Raised on the UI thread so a surviving handler shows up as a leak, not a cross-thread error.
		RunOnUIThread.Execute(() => items[0].Role = "Changed");

		await TestServices.WindowHelper.WaitForIdle();
		Verify.AreEqual(40, items.Count, "The items must still be alive after the table was collected.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies realizing automation peers for a TableView does not keep the control alive after unload.")]
	public async Task VerifyAutomationPeersDoNotKeepTheTableAlive()
	{
		var items = new ObservableCollection<Person>(MakeManyItems(40));
		var tracked = new Dictionary<string, WeakReference>();

		RunOnUIThread.Execute(() => CreateLoadAndWalkPeers(items, tracked));

		await SettleAfterLoad();

		RunOnUIThread.Execute(() => Content = null);

		await AssertCollected(tracked);

		Verify.AreEqual(40, items.Count, "The items source must still be alive after the table was collected.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies releasing a TableView whose rows were recycled into the template pools does not fail-fast, for both an unloaded table and one in a closed secondary window.")]
	public async Task VerifyReleasingTableViewWithRecycledRowsDoesNotCrash()
	{
		foreach (var inSecondaryWindow in new[] { false, true })
		{
			var tracked = new Dictionary<string, WeakReference>();
			TableView tableView = null;
			Window window = null;

			try
			{
				RunOnUIThread.Execute(() =>
				{
					EnsureTabularControlsResources();
					tableView = CreateTableView(MakeManyItems(40), headers: new[] { "Name", "Role" }, width: 400, height: 300);
					tracked["TableView"] = new WeakReference(tableView);
					if (inSecondaryWindow)
					{
						window = new Window() { Content = tableView };
						window.Activate();
					}
					else
					{
						LoadContent(tableView);
					}
				});
				await SettleAfterLoad();

				// Swapping the source recycles the realized rows into the template's pool.
				RunOnUIThread.Execute(() =>
				{
					Verify.IsGreaterThan(GetRealizedRows(tableView).Count, 0, "Rows must be realized before the source swap.");
					tableView.ItemsSource = MakeManyItems(40);
					tableView.UpdateLayout();
				});
				await SettleAfterLoad();

				RunOnUIThread.Execute(() =>
				{
					Verify.IsTrue(RowTemplateIsPooled(tableView), "The row template must carry a RecyclePool so Detach has a pool edge to break.");

					tableView = null;
					if (inSecondaryWindow)
					{
						window.Close();
						window = null;
					}
					else
					{
						Content = null;
					}
				});

				await AssertCollected(tracked);
			}
			finally
			{
				RunOnUIThread.Execute(() =>
				{
					window?.Close();
					TestServices.WindowHelper.CurrentTestWindow.Activate();
				});
			}

			Log.Comment($"Released a TableView with pooled row templates (secondary window: {inSecondaryWindow}) without crashing.");
		}
	}

	// NoInlining so the selector and template do not linger in a stack slot past the call.
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool RowTemplateIsPooled(TableView tableView)
	{
		var repeater = (Microsoft.UI.Xaml.Controls.ItemsRepeater)tableView.FindVisualChildByName("PART_RowsRepeater");
		var selector = (Microsoft.UI.Xaml.Controls.DataTemplateSelector)repeater.ItemTemplate;
		var rowTemplate = selector.SelectTemplate(((IList<Person>)tableView.ItemsSource)[0]);
		return rowTemplate != null && Microsoft.UI.Xaml.Controls.RecyclePool.GetPoolInstance(rowTemplate) != null;
	}
	[MethodImpl(MethodImplOptions.NoInlining)]
	private void CreateAndLoadTable(object itemsSource, Dictionary<string, WeakReference> tracked)
	{
		var tableView = CreateTableView(itemsSource, headers: new[] { "Name", "Role" }, width: 400, height: 300);
		LoadContent(tableView);

		tracked["TableView"] = new WeakReference(tableView);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void CreateAndLoadShapedTable(object itemsSource, Dictionary<string, WeakReference> tracked)
	{
		var source = TableViewSource.From(itemsSource);
		source.Sort("Name", SortDirection.Ascending);

		var tableView = CreateTableView(source, headers: new[] { "Name", "Role" }, width: 400, height: 300);
		LoadContent(tableView);

		tracked["TableView"] = new WeakReference(tableView);
		tracked["TableViewSource"] = new WeakReference(source);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void CreateLoadAndWalkPeers(object itemsSource, Dictionary<string, WeakReference> tracked)
	{
		var tableView = CreateTableView(itemsSource, headers: new[] { "Name", "Role" }, width: 400, height: 300);
		LoadContent(tableView);

		foreach (var row in GetRealizedRows(tableView))
		{
			var rowPeer = GetRowPeer(row);
			rowPeer?.GetName();
			GetCellPeer(rowPeer, 0)?.GetName();
		}

		tracked["TableView"] = new WeakReference(tableView);
	}
}

// Isolated, not ApiTestBase: after XAML shuts down on a second thread the shared Setup/Cleanup would hang.
[TestClass]
public class TableViewShutdownTests
{
	// Holds the control past ShutdownAllPeers, like an app view model would.
	private static TableView s_keepAlive;
	private static DataTemplate s_rowTemplate;

	[TestMethod]
	[TestProperty("IsolationLevel", "Method")]
	[Ignore("Uno has no second XAML island/DispatcherQueueController shutdown on a secondary thread, and no C#/WinRT IWinRTObject to release the native object.")]
	[TestProperty("Description", "Verifies releasing a TableView whose row templates are pooled, after XAML shut down on its thread, does not fail-fast.")]
	public void VerifyTableViewReleasedAfterXamlShutdownOnItsThreadDoesNotCrash()
	{
		Exception failure = null;
		Exception clearPoolFailure = null;
		bool templatesPooled = false;

		var thread = new global::System.Threading.Thread(() =>
		{
			try
			{
				var controller = Microsoft.UI.Dispatching.DispatcherQueueController.CreateOnCurrentThread();
				var xaml = Microsoft.UI.Xaml.Hosting.WindowsXamlManager.InitializeForCurrentThread();

				templatesPooled = ShowTableViewAndRunUntilExit(controller.DispatcherQueue);

				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();

				// Window stays open, so shutdown runs while s_keepAlive still holds the TableView.
				controller.ShutdownQueue();
				xaml.Dispose();

				// The state from the crash stack: clearing the template's pool, as main's Detach did, now throws.
				var rowTemplate = s_rowTemplate;
				s_rowTemplate = null;
				try
				{
					Microsoft.UI.Xaml.Controls.RecyclePool.SetPoolInstance(rowTemplate, null);
				}
				catch (Exception ex)
				{
					clearPoolFailure = ex;
				}

				// Drop the test's references after shutdown. In-process this does not reach Detach's throwing
				// call (it passes without the fix too), so it only checks that releasing the control does not crash.
				var tableView = s_keepAlive;
				s_keepAlive = null;
#if !HAS_UNO
				((WinRT.IWinRTObject)tableView).NativeObject.Dispose();
				((WinRT.IWinRTObject)rowTemplate).NativeObject.Dispose();
#endif
			}
			catch (Exception ex)
			{
				failure = ex;
			}
		});
		thread.SetApartmentState(global::System.Threading.ApartmentState.STA);
		thread.Start();

		Verify.IsTrue(thread.Join(TimeSpan.FromSeconds(60)), "The secondary UI thread should finish shutting down XAML.");
		Verify.IsNull(failure, $"Secondary UI thread failed: {failure}");
		Verify.IsTrue(templatesPooled, "The row template must carry a RecyclePool so Detach has a pool edge to break.");
		Verify.IsNotNull(clearPoolFailure, "Precondition: after XAML shutdown, clearing the row template's pool must throw; that is the state Detach has to tolerate.");
		Log.Comment($"Clearing the pool after shutdown threw {clearPoolFailure?.GetType().Name} 0x{clearPoolFailure?.HResult:X8}: {clearPoolFailure?.Message}");
		Verify.AreEqual(unchecked((int)0x8000FFFF), clearPoolFailure?.HResult ?? 0, "Precondition: clearing the pool must fail with E_UNEXPECTED, the shutdown failure.");

		Log.Comment("Releasing the TableView after XAML shutdown completed without crashing.");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool ShowTableViewAndRunUntilExit(Microsoft.UI.Dispatching.DispatcherQueue dispatcher)
	{
		bool templatesPooled = false;
		var items = MakeManyItems(30);
		var tableView = CreateTableView(items, headers: new[] { "Name", "Role" });
		var window = new Window() { Content = tableView };
		s_keepAlive = tableView;

		tableView.Loaded += (sender, args) =>
		{
			tableView.UpdateLayout();

			// Swap the source so realized rows are recycled into the template's pool.
			var replacement = MakeManyItems(30);
			tableView.ItemsSource = replacement;
			tableView.UpdateLayout();

			dispatcher.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
			{
				try
				{
					var repeater = (Microsoft.UI.Xaml.Controls.ItemsRepeater)tableView.FindVisualChildByName("PART_RowsRepeater");
					var selector = (Microsoft.UI.Xaml.Controls.DataTemplateSelector)repeater.ItemTemplate;
					var rowTemplate = selector.SelectTemplate(replacement[0]);
					templatesPooled = rowTemplate != null && Microsoft.UI.Xaml.Controls.RecyclePool.GetPoolInstance(rowTemplate) != null;
					s_rowTemplate = rowTemplate;
					Log.Comment($"Realized rows: {GetRealizedRows(tableView).Count}; row template pooled: {templatesPooled}.");
				}
				finally
				{
					dispatcher.EnqueueEventLoopExit();
				}
			});
		};

		window.Activate();
		dispatcher.RunEventLoop();
		return templatesPooled;
	}
}

internal sealed class NotifyingPerson : INotifyPropertyChanged
{
	private string _name;
	private string _role;

	public event PropertyChangedEventHandler PropertyChanged;

	public string Name
	{
		get => _name;
		set { _name = value; Raise(nameof(Name)); }
	}

	public string Role
	{
		get => _role;
		set { _role = value; Raise(nameof(Role)); }
	}

	private void Raise(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
}

internal static class TableViewLifetimeTestHelpers
{
	internal static async Task SettleAfterLoad()
	{
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() => { });

		await TestServices.WindowHelper.WaitForIdle();
	}

	// Retried because a rooted object never becomes collectable, so retrying cannot hide a leak.
	private const int CollectionAttempts = 5;

	internal static async Task AssertCollected(Dictionary<string, WeakReference> tracked)
	{
		Verify.IsGreaterThan(tracked.Count, 0, "The fixture must have tracked at least one object.");

		var alive = new List<string>();

		for (var attempt = 1; attempt <= CollectionAttempts; attempt++)
		{
			await TestServices.WindowHelper.WaitForIdle();

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			await TestServices.WindowHelper.WaitForIdle();

			alive = tracked.Where(pair => pair.Value.IsAlive).Select(pair => pair.Key).ToList();

			if (alive.Count == 0)
			{
				Log.Comment($"All tracked objects collected after {attempt} attempt(s).");
				return;
			}

			Log.Comment($"Attempt {attempt}: still alive - {string.Join(", ", alive)}.");
		}

		Verify.Fail(
			$"These objects were still alive after {CollectionAttempts} collection attempts: {string.Join(", ", alive)}. " +
			"Something that outlived the control is still holding it - most likely a subscription or peer cache that is not released on unload.");
	}
}
