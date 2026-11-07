#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;
using static Uno.UI.RuntimeTests.Helpers.LeakTestHelper;

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

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.NativeAndroid)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25969")]
	public async Task When_BindableButton_Never_Attached_Then_Not_Rooted_By_Command()
	{
#if __ANDROID__
		var command = new TestCommand();
		var refs = CreateBindableButtons(command);
		Assert.AreEqual(Count, command.SubscriberCount);

		await CollectAsync(refs);

		Assert.AreEqual(0, refs.Count(r => r.IsAlive), "A never-attached BindableButton is still rooted by its ClickCommand.");
		GC.KeepAlive(command);
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.NativeAndroid)]
	public async Task When_BindableButton_Alive_Through_GC_Then_CanExecuteChanged_Still_Updates_Enabled()
	{
#if __ANDROID__
		var command = new TestCommand();
		var button = new Uno.UI.Controls.BindableButton(Uno.UI.ContextHelper.Current) { ClickCommand = command };
		Assert.IsTrue(button.Enabled);

		await CollectAsync(new List<WeakReference>(), until: () => true);

		command.CanExecuteValue = false;
		Assert.IsFalse(button.Enabled, "The BindableButton lost its subscription while still alive.");
		GC.KeepAlive(button);
#else
		await Task.CompletedTask;
#endif
	}

#if __ANDROID__
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static List<WeakReference> CreateBindableButtons(ICommand command)
	{
		var refs = new List<WeakReference>();
		for (var i = 0; i < Count; i++)
		{
			refs.Add(new WeakReference(new Uno.UI.Controls.BindableButton(Uno.UI.ContextHelper.Current) { ClickCommand = command }));
		}
		return refs;
	}
#endif

	// The common case: a page and its view model become unreachable together. The command must not wait for the removal
	// of a subscription of a button that died with it, queued to the dispatcher, before it can be collected.
	[TestMethod]
	// 6.x only: the owner's DependencyObjectStore has a finalizer (master removed it), so the store keeps what it references
	// alive until finalizers run. Single-threaded WASM runs them only from the event loop, and the mobile runtimes fail too.
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaMobile)]
	[RunsOnUIThread]
#if RUNTIME_NATIVE_AOT
	[Ignore("NativeAOT GC behavior may differ for leak detection tests")]
#endif
	public async Task When_Owner_And_Command_Dropped_Together_Then_Command_Collected_Without_Dispatcher()
	{
		await WindowHelper.WaitForIdle();
		var (ownerRef, commandRef) = CreateWithOwnCommand();

		// Don't yield to the dispatcher here: the command must not depend on queued work to be collected.
		CollectWithoutDispatcher([commandRef]);

		if (ownerRef.IsAlive)
		{
			Assert.Inconclusive("The button was not collected without yielding on this platform.");
		}

		Assert.IsFalse(commandRef.IsAlive, "The command is kept alive by the cleanup of a button that died with it.");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static (WeakReference owner, WeakReference command) CreateWithOwnCommand()
	{
		var command = new TestCommand();
		var button = new Button { Command = command, DataContext = new object() };

		// Tracks resurrection: a short weak reference is cleared even while finalization still keeps the command alive.
		return (new WeakReference(button), new WeakReference(command, trackResurrection: true));
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
