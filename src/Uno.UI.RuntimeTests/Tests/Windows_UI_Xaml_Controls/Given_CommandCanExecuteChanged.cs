#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

// ButtonBase and MenuFlyoutItem subscribe to Command.CanExecuteChanged weakly, as WinUI does (WeakEventPtr):
// a command that outlives a control (a view model's) must not keep it alive, e.g. an item of a flyout never opened.
[TestClass]
public class Given_CommandCanExecuteChanged
{
	private const int Count = 5;

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(nameof(Button))]
	[DataRow(nameof(AppBarButton))]
	[DataRow(nameof(MenuFlyoutItem))]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
#if RUNTIME_NATIVE_AOT
	[Ignore("NativeAOT GC behavior may differ for leak detection tests")]
#endif
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25969")]
	public async Task When_Never_Loaded_Then_Not_Rooted_By_Command(string controlType)
	{
		var command = new TestCommand(); // outlives the controls
		var refs = CreateNeverLoaded(controlType, command);

		await CollectAsync(refs);

		Assert.AreEqual(0, refs.Count(r => r.IsAlive), $"A never-loaded {controlType} is still rooted by its Command.");
		GC.KeepAlive(command);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
#if RUNTIME_NATIVE_AOT
	[Ignore("NativeAOT GC behavior may differ for leak detection tests")]
#endif
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25969")]
	public async Task When_Never_Loaded_With_XamlUICommand_Then_Not_Rooted_By_Command()
	{
		var command = new XamlUICommand { Label = "Print" };
		var refs = CreateNeverLoaded(nameof(Button), command);

		await CollectAsync(refs);

		Assert.AreEqual(0, refs.Count(r => r.IsAlive), "A never-loaded Button is still rooted by its XamlUICommand.");
		GC.KeepAlive(command);
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(false)]
	[DataRow(true)]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
#if RUNTIME_NATIVE_AOT
	[Ignore("NativeAOT GC behavior may differ for leak detection tests")]
#endif
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25969")]
	public async Task When_Screen_With_Unopened_MenuFlyout_Dropped_Then_Not_Rooted_By_Command(bool shown)
	{
		var command = new TestCommand();
		var screenRef = CreateScreenWithMenu(command, shown);
		try
		{
			if (shown)
			{
				for (var i = 0; i < 20 && !IsLoaded(screenRef); i++)
				{
					await WindowHelper.WaitForIdle();
				}
				Assert.IsTrue(IsLoaded(screenRef), "The screen was never loaded.");

				WindowHelper.WindowContent = null;
				await WindowHelper.WaitForIdle();
			}

			var refs = new List<WeakReference> { screenRef };
			await CollectAsync(refs);

			Assert.IsFalse(screenRef.IsAlive, "The screen is still rooted by the command of its menu items.");
			GC.KeepAlive(command);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
#if RUNTIME_NATIVE_AOT
	[Ignore("NativeAOT GC behavior may differ for leak detection tests")]
#endif
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25969")]
	public async Task When_Owner_Collected_Then_Subscription_Removed_From_Command()
	{
		var command = new TestCommand();
		var refs = CreateNeverLoaded(nameof(Button), command);
		Assert.AreEqual(Count, command.SubscriberCount);

		await CollectAsync(refs, () => command.SubscriberCount == 0);

		Assert.AreEqual(0, command.SubscriberCount, "Subscriptions of collected buttons are still on the command.");
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
#if RUNTIME_NATIVE_AOT
	[Ignore("NativeAOT GC behavior may differ for leak detection tests")]
#endif
	public async Task When_Owner_Collected_Then_Raising_CanExecuteChanged_Is_Safe()
	{
		var command = new TestCommand();
		var refs = CreateNeverLoaded(nameof(MenuFlyoutItem), command);

		// Don't yield to the dispatcher here: it would remove the subscriptions of the collected items before the raise.
		for (var i = 0; i < 10 && refs.Any(r => r.IsAlive); i++)
		{
			GC.Collect(2);
			GC.WaitForPendingFinalizers();
		}
		if (refs.Any(r => r.IsAlive) || command.SubscriberCount != Count)
		{
			Assert.Inconclusive("Could not collect the items while their subscriptions are still on the command.");
		}

		// TestCommand raises over its live handler list: unsubscribing from within the raise would throw.
		command.CanExecuteValue = false;

		await CollectAsync(refs, () => command.SubscriberCount == 0);
		Assert.AreEqual(0, command.SubscriberCount);
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(nameof(Button))]
	[DataRow(nameof(MenuFlyoutItem))]
	public async Task When_Owner_Alive_Through_GC_Then_CanExecuteChanged_Still_Updates_IsEnabled(string controlType)
	{
		var command = new TestCommand();
		var control = (Control)Create(controlType, command);

		await CollectAsync(new List<WeakReference>(), until: () => true);

		command.CanExecuteValue = false;
		Assert.IsFalse(control.IsEnabled, $"The {controlType} lost its subscription while still alive.");
		GC.KeepAlive(control);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Button_Loaded_Then_CanExecuteChanged_Updates_IsEnabled()
	{
		var command = new TestCommand();
		var button = new Button { Content = "Print", Command = command };
		await UITestHelper.Load(button);
		Assert.IsTrue(button.IsEnabled);

		command.CanExecuteValue = false;
		await WindowHelper.WaitForIdle();
		Assert.IsFalse(button.IsEnabled);

		command.CanExecuteValue = true;
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(button.IsEnabled);
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(nameof(Button))]
	[DataRow(nameof(MenuFlyoutItem))]
	public void When_Never_Loaded_Then_CanExecuteChanged_Updates_IsEnabled(string controlType)
	{
		var command = new TestCommand();
		var control = (Control)Create(controlType, command);
		Assert.IsTrue(control.IsEnabled);

		command.CanExecuteValue = false;
		Assert.IsFalse(control.IsEnabled);

		command.CanExecuteValue = true;
		Assert.IsTrue(control.IsEnabled);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Button_Leaves_Tree_Then_Unsubscribed_From_Command()
	{
		var command = new TestCommand();
		var button = new Button { Content = "Print", Command = command };
		var host = new Grid { Children = { button } };
		await UITestHelper.Load(host);
		Assert.AreEqual(1, command.SubscriberCount);

		host.Children.Clear();
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(0, command.SubscriberCount);
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(nameof(Button))]
	[DataRow(nameof(MenuFlyoutItem))]
	public void When_Command_Replaced_Then_Old_Command_Unsubscribed(string controlType)
	{
		var oldCommand = new TestCommand();
		var newCommand = new TestCommand();
		var control = Create(controlType, oldCommand);
		Assert.AreEqual(1, oldCommand.SubscriberCount);

		SetCommand(control, newCommand);

		Assert.AreEqual(0, oldCommand.SubscriberCount);
		Assert.AreEqual(1, newCommand.SubscriberCount);
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(nameof(Button))]
	[DataRow(nameof(MenuFlyoutItem))]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_Command_Remove_Throws_Then_Command_Still_Replaced(string controlType)
	{
		var throwingCommand = new TestCommand { ThrowOnRemove = true };
		var newCommand = new TestCommand();
		var control = Create(controlType, throwingCommand);

		SetCommand(control, newCommand);

		Assert.AreEqual(1, newCommand.SubscriberCount);
		newCommand.CanExecuteValue = false;
		Assert.IsFalse(((Control)control).IsEnabled);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static List<WeakReference> CreateNeverLoaded(string controlType, ICommand command)
	{
		var refs = new List<WeakReference>();
		for (var i = 0; i < Count; i++)
		{
			refs.Add(new WeakReference(Create(controlType, command)));
		}
		return refs;
	}

	// A screen whose "..." button has a MenuFlyout of command items, the flyout being never opened.
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference CreateScreenWithMenu(ICommand command, bool show)
	{
		var screen = new Grid();
		var menu = new MenuFlyout();
		for (var i = 0; i < 3; i++)
		{
			menu.Items.Add(new MenuFlyoutItem { Text = $"Item {i}", Command = command, DataContext = screen });
		}
		screen.Children.Add(new Button { Content = "...", Flyout = menu });

		if (show)
		{
			WindowHelper.WindowContent = screen;
		}

		return new WeakReference(screen);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool IsLoaded(WeakReference reference) => reference.Target is FrameworkElement { IsLoaded: true };

	private static FrameworkElement Create(string controlType, ICommand command) => controlType switch
	{
		nameof(Button) => new Button { Command = command, DataContext = new object() },
		nameof(AppBarButton) => new AppBarButton { Command = command, DataContext = new object() },
		nameof(MenuFlyoutItem) => new MenuFlyoutItem { Command = command, DataContext = new object() },
		_ => throw new ArgumentOutOfRangeException(nameof(controlType)),
	};

	private static void SetCommand(FrameworkElement control, ICommand command)
	{
		switch (control)
		{
			case Button button:
				button.Command = command;
				break;
			case MenuFlyoutItem item:
				item.Command = command;
				break;
		}
	}

	// Collects at least 3 times, pumping the dispatcher in between so the removals posted by finalizers run.
	private static async Task CollectAsync(List<WeakReference> refs, Func<bool>? until = null)
	{
		until ??= () => !refs.Any(r => r.IsAlive);
		var sw = Stopwatch.StartNew();
		var timeout = TimeSpan.FromSeconds(10);
		var minIterations = 3;

		while (minIterations-- > 0 || (!until() && sw.Elapsed < timeout))
		{
			GC.Collect(2);
			GC.WaitForPendingFinalizers();
			GC.Collect(2);

			// Some platforms need a yield before weak references are cleared, as in Given_BindingMemoryLeak.
#if __SKIA__
			if (OperatingSystem.IsBrowser() || OperatingSystem.IsLinux() || OperatingSystem.IsIOS() || OperatingSystem.IsAndroid())
#endif
			{
				await Task.Yield();
				GC.Collect(2);
				GC.WaitForPendingFinalizers();
				GC.Collect(2);
			}

			await WindowHelper.WaitForIdle();
		}
	}

	// Raises over its live handler list, like a command with a custom add/remove: removing a handler mid-raise throws.
	private sealed class TestCommand : ICommand
	{
		private readonly List<EventHandler> _handlers = new();
		private bool _canExecute = true;

		public bool ThrowOnRemove { get; init; }

		public event EventHandler? CanExecuteChanged
		{
			add => _handlers.Add(value!);
			remove
			{
				if (ThrowOnRemove)
				{
					throw new InvalidOperationException("remove failed");
				}

				_handlers.Remove(value!);
			}
		}

		public int SubscriberCount => _handlers.Count;

		public bool CanExecuteValue
		{
			get => _canExecute;
			set
			{
				_canExecute = value;
				foreach (var handler in _handlers)
				{
					handler(this, EventArgs.Empty);
				}
			}
		}

		public bool CanExecute(object? parameter) => _canExecute;

		public void Execute(object? parameter) { }
	}
}
