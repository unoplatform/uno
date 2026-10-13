#if __SKIA__
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.DevTools.Input;
using Uno.Foundation.Extensibility;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Input.Preview.Injection;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Input_Preview_Injection;

[TestClass]
[RunsOnUIThread]
public class Given_InputInjector_Keyboard
{
	private static InputInjector GetInjector()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			// Inconclusive rather than [PlatformCondition]: XamlIsland hosting is a runtime
			// condition, so it cannot be expressed as an attribute.
			Assert.Inconclusive("Input injection is not supported in XamlIslands.");
		}

		return InputInjector.TryCreate() ?? throw new InvalidOperationException("InputInjector.TryCreate() returned null.");
	}

	private static InjectedInputKeyboardInfo Key(VirtualKey key, InjectedInputKeyOptions options = InjectedInputKeyOptions.None)
		=> new() { VirtualKey = (ushort)key, KeyOptions = options };

	private static InjectedInputKeyboardInfo KeyUp(VirtualKey key)
		=> Key(key, InjectedInputKeyOptions.KeyUp);

	private static InjectedInputKeyboardInfo Unicode(char c, InjectedInputKeyOptions options = InjectedInputKeyOptions.None)
		=> new() { VirtualKey = 0, ScanCode = c, KeyOptions = options | InjectedInputKeyOptions.Unicode };

	private static void Tap(InputInjector injector, VirtualKey key)
		=> injector.InjectKeyboardInput(new[] { Key(key), KeyUp(key) });

	/// <summary>
	/// The key carrying standard editing commands: Command on Apple keyboards, Control elsewhere.
	/// </summary>
	private static VirtualKey CommandKey
		=> Uno.UI.Helpers.DeviceTargetHelper.UsesAppleKeyboardLayout ? VirtualKey.LeftWindows : VirtualKey.Control;

	/// <summary>
	/// Renders surrogates as U+XXXX. Assertion messages travel through the test-results XML, which
	/// cannot carry an unpaired surrogate, so never compare raw text that may contain one.
	/// </summary>
	private static string Escape(string text)
		=> string.Concat(text.Select(c => char.IsSurrogate(c) ? $"U+{(int)c:X4}" : c.ToString()));

	[TestMethod]
	public async Task When_InjectKey_Types_Into_Focused_TextBox()
	{
		var injector = GetInjector();
		var textBox = new TextBox();
		await UITestHelper.Load(textBox);
		textBox.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		// Deliberately not naming the target element - the key must find the focused element itself.
		Tap(injector, VirtualKey.A);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual("a", textBox.Text);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
	public async Task When_InjectKey_During_Secondary_Window_Show_Activation()
	{
		var original = TestServices.WindowHelper.CurrentTestWindow;
		var injector = GetInjector();
		var secondary = new Window { Content = new Grid() };
		var originalKeys = 0;
		var secondaryKeys = 0;
		var injected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var visibleDuringActivation = true;
		void OnOriginalKey(object sender, KeyRoutedEventArgs args) => originalKeys++;
		void OnSecondaryKey(object sender, KeyRoutedEventArgs args) => secondaryKeys++;
		void OnActivated(object sender, WindowActivatedEventArgs args)
		{
			if (args.WindowActivationState != WindowActivationState.Deactivated && !injected.Task.IsCompleted)
			{
				visibleDuringActivation = secondary.NativeWrapper!.IsVisible;
				Tap(injector, VirtualKey.A);
				injected.SetResult();
			}
		}
		original.RootElement!.KeyDown += OnOriginalKey;
		secondary.RootElement!.KeyDown += OnSecondaryKey;
		secondary.Activated += OnActivated;
		try
		{
			await TestServices.WindowHelper.WaitForIdle();
			// An unshown HWND is inactive. Exercise the real WM_ACTIVATE inside ShowWindow/ShowCore.
			((Uno.UI.Xaml.Controls.NativeWindowWrapperBase)secondary.NativeWrapper!).ActivationState = Windows.UI.Core.CoreWindowActivationState.Deactivated;
			secondary.Activate();
			await injected.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.IsFalse(visibleDuringActivation, "This test must inject before ShowCore returns and marks the wrapper visible.");
			Assert.AreEqual(1, secondaryKeys);
			Assert.AreEqual(0, originalKeys);
		}
		finally
		{
			secondary.Activated -= OnActivated;
			secondary.RootElement!.KeyDown -= OnSecondaryKey;
			original.RootElement!.KeyDown -= OnOriginalKey;
			secondary.Close();
			original.Activate();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWayland)]
	public async Task When_InjectKey_Without_Native_Activation_Uses_Managed_Focus_And_Live_Window()
	{
		var original = TestServices.WindowHelper.CurrentTestWindow;
		var injector = GetInjector();
		var firstText = new TextBox();
		var secondText = new TextBox();
		var first = new Window { Content = firstText };
		var second = new Window { Content = secondText };
		var secondTarget = (IInputInjectorTarget)second.RootElement!.XamlRoot!.VisualTree.ContentRoot.InputManager;
		try
		{
			Assert.IsFalse(secondTarget.IsReadyForKeyboardInput, "An initialized but unshown window is not an injection target.");
			first.Activate();
			await TestServices.WindowHelper.WaitForLoaded(firstText);
			Assert.IsTrue(firstText.Focus(FocusState.Programmatic));
			await TestServices.WindowHelper.WaitForIdle();
			second.Activate();
			await TestServices.WindowHelper.WaitForLoaded(secondText);
			Assert.IsTrue(secondText.Focus(FocusState.Programmatic));
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsTrue(secondTarget.IsReadyForKeyboardInput);
			Assert.IsTrue(ApiExtensibility.CreateInstance<IInputInjectorKeyboardTargetPolicy>(injector, out var policy));
			var firstTarget = (IInputInjectorTarget)first.RootElement!.XamlRoot!.VisualTree.ContentRoot.InputManager;
			var originalTarget = (IInputInjectorTarget)original.RootElement!.XamlRoot!.VisualTree.ContentRoot.InputManager;
			if (policy!.IsActive(firstTarget) || policy.IsActive(secondTarget) || policy.IsActive(originalTarget))
			{
				Assert.Inconclusive("This lifecycle test requires a compositor without native activation (e.g. seatless Weston).");
			}
			Tap(injector, VirtualKey.A);
			Assert.AreEqual("a", secondText.Text);
			Assert.AreEqual("", firstText.Text);
			second.Close();
			Assert.IsFalse(secondTarget.IsReadyForKeyboardInput, "A retained root of a closed window must be ineligible.");
			Assert.IsTrue(firstText.Focus(FocusState.Programmatic));
			await TestServices.WindowHelper.WaitForIdle();
			Tap(injector, VirtualKey.B);
			Assert.AreEqual("b", firstText.Text);
			Assert.AreEqual("a", secondText.Text);
		}
		finally
		{
			second.Close();
			first.Close();
			original.Activate();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWayland)]
	public async Task When_InjectKey_Immediately_After_Cross_Window_Focus()
	{
		var original = TestServices.WindowHelper.CurrentTestWindow;
		var injector = GetInjector();
		var firstText = new TextBox { IsTabStop = false };
		var secondText = new TextBox { IsTabStop = false };
		var first = new Window { Content = firstText };
		var second = new Window { Content = secondText };
		try
		{
			first.Activate();
			await TestServices.WindowHelper.WaitForLoaded(firstText);
			second.Activate();
			await TestServices.WindowHelper.WaitForLoaded(secondText);
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsTrue(ApiExtensibility.CreateInstance<IInputInjectorKeyboardTargetPolicy>(injector, out var policy));
			var firstTarget = (IInputInjectorTarget)first.RootElement!.XamlRoot!.VisualTree.ContentRoot.InputManager;
			var secondTarget = (IInputInjectorTarget)second.RootElement!.XamlRoot!.VisualTree.ContentRoot.InputManager;
			var originalTarget = (IInputInjectorTarget)original.RootElement!.XamlRoot!.VisualTree.ContentRoot.InputManager;
			if (policy!.IsActive(firstTarget) || policy.IsActive(secondTarget) || policy.IsActive(originalTarget))
			{
				Assert.Inconclusive("This test requires a compositor without native activation (e.g. seatless Weston).");
			}

			// Prevent loading from pre-focusing B, then establish A as the previous injection target.
			firstText.IsTabStop = true;
			Assert.IsTrue(firstText.Focus(FocusState.Programmatic));
			await TestServices.WindowHelper.WaitForIdle();
			Assert.AreSame(firstTarget, policy.GetFallbackTarget());
			Assert.AreNotSame(secondText, FocusManager.GetFocusedElement(secondText.XamlRoot!));

			secondText.IsTabStop = true;
			Assert.IsTrue(secondText.Focus(FocusState.Programmatic));
			// No dispatcher yield: injection must observe the focus committed by the preceding call.
			Tap(injector, VirtualKey.A);
			Assert.AreEqual("a", secondText.Text);
			Assert.AreEqual(string.Empty, firstText.Text);

			await TestServices.WindowHelper.WaitForIdle();
			Tap(injector, VirtualKey.B);
			Assert.AreEqual("ab", secondText.Text, "Deferred focus events must preserve the committed injection target.");
			Assert.AreEqual(string.Empty, firstText.Text);
		}
		finally
		{
			second.Close();
			first.Close();
			original.Activate();
		}
	}

	/// <summary>
	/// The discriminating test: CharacterReceived is raised only by InputManager, so this cannot
	/// pass through TestServices.KeyboardHelper, which raises routed events directly on an element.
	/// </summary>
	[TestMethod]
	public async Task When_InjectKey_Raises_CharacterReceived()
	{
		var injector = GetInjector();
		var textBox = new TextBox();
		await UITestHelper.Load(textBox);
		textBox.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		var sequence = new List<string>();
		textBox.AddHandler(
			UIElement.KeyDownEvent,
			new KeyEventHandler((_, _) => sequence.Add("KeyDown")),
			handledEventsToo: true);
		textBox.AddHandler(
			UIElement.CharacterReceivedEvent,
			new TypedEventHandler<UIElement, CharacterReceivedRoutedEventArgs>((_, e) => sequence.Add($"CharacterReceived:{e.Character}")),
			handledEventsToo: true);

		Tap(injector, VirtualKey.A);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual("a", textBox.Text);
		CollectionAssert.AreEqual(new[] { "KeyDown", "CharacterReceived:a" }, sequence);
	}

	[TestMethod]
	public async Task When_InjectShiftA_Produces_Uppercase()
	{
		var injector = GetInjector();
		var textBox = new TextBox();
		await UITestHelper.Load(textBox);
		textBox.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			injector.InjectKeyboardInput(new[]
			{
				Key(VirtualKey.Shift),
				Key(VirtualKey.A),
				KeyUp(VirtualKey.A),
				KeyUp(VirtualKey.Shift),
			});
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual("A", textBox.Text);
		}
		finally
		{
			injector.InjectKeyboardInput(new[] { KeyUp(VirtualKey.Shift) });
		}
	}

	[TestMethod]
	[DataRow(VirtualKey.LeftShift, VirtualKey.RightShift)]
	[DataRow(VirtualKey.RightShift, VirtualKey.LeftShift)]
	public async Task When_Releasing_One_Shift_While_Other_Held_Keeps_Uppercase(VirtualKey released, VirtualKey held)
	{
		var injector = GetInjector();
		var textBox = new TextBox();
		await UITestHelper.Load(textBox);
		textBox.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			injector.InjectKeyboardInput(new[]
			{
				Key(released),
				Key(held),
				KeyUp(released),
				Key(VirtualKey.A),
				KeyUp(VirtualKey.A),
			});
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual("A", textBox.Text);
		}
		finally
		{
			injector.InjectKeyboardInput(new[] { KeyUp(released), KeyUp(held) });
		}
	}

	[TestMethod]
	public async Task When_Releasing_One_Control_While_Other_Held_Suppresses_Character()
	{
		var injector = GetInjector();
		var textBox = new TextBox();
		await UITestHelper.Load(textBox);
		textBox.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		var characters = new List<char>();
		textBox.AddHandler(
			UIElement.CharacterReceivedEvent,
			new TypedEventHandler<UIElement, CharacterReceivedRoutedEventArgs>((_, e) => characters.Add(e.Character)),
			handledEventsToo: true);

		try
		{
			injector.InjectKeyboardInput(new[]
			{
				Key(VirtualKey.LeftControl),
				Key(VirtualKey.RightControl),
				KeyUp(VirtualKey.LeftControl),
				Key(VirtualKey.B),
				KeyUp(VirtualKey.B),
			});
			await TestServices.WindowHelper.WaitForIdle();

			CollectionAssert.AreEqual(Array.Empty<char>(), characters);
		}
		finally
		{
			injector.InjectKeyboardInput(new[] { KeyUp(VirtualKey.LeftControl), KeyUp(VirtualKey.RightControl) });
		}
	}

	[TestMethod]
	[DataRow(VirtualKey.LeftShift, VirtualKey.RightShift, VirtualKeyModifiers.Shift)]
	[DataRow(VirtualKey.LeftControl, VirtualKey.RightControl, VirtualKeyModifiers.Control)]
	[DataRow(VirtualKey.LeftMenu, VirtualKey.RightMenu, VirtualKeyModifiers.Menu)]
	[DataRow(VirtualKey.LeftWindows, VirtualKey.RightWindows, VirtualKeyModifiers.Windows)]
	public async Task When_Releasing_One_Side_While_Other_Held_Reports_Modifier(VirtualKey released, VirtualKey held, VirtualKeyModifiers expected)
	{
		var injector = GetInjector();
		var button = new Button { Content = "Target" };
		await UITestHelper.Load(button);
		button.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		VirtualKeyModifiers? releaseModifiers = null;
		button.AddHandler(
			UIElement.KeyUpEvent,
			new KeyEventHandler((_, e) =>
			{
				if (e.Key == released)
				{
					releaseModifiers ??= e.KeyboardModifiers;
				}
			}),
			handledEventsToo: true);

		try
		{
			injector.InjectKeyboardInput(new[] { Key(released), Key(held), KeyUp(released) });
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsNotNull(releaseModifiers);
			Assert.IsTrue(releaseModifiers!.Value.HasFlag(expected), $"Expected {expected} while {held} is held, got {releaseModifiers}.");
		}
		finally
		{
			injector.InjectKeyboardInput(new[] { KeyUp(released), KeyUp(held) });
		}
	}

	[TestMethod]
	public async Task When_InjectCommandA_Selects_All_Without_Character()
	{
		var injector = GetInjector();
		var textBox = new TextBox { Text = "hello world" };
		await UITestHelper.Load(textBox);
		textBox.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		var characters = new List<char>();
		textBox.AddHandler(
			UIElement.CharacterReceivedEvent,
			new TypedEventHandler<UIElement, CharacterReceivedRoutedEventArgs>((_, e) => characters.Add(e.Character)),
			handledEventsToo: true);

		try
		{
			injector.InjectKeyboardInput(new[]
			{
				Key(CommandKey),
				Key(VirtualKey.A),
				KeyUp(VirtualKey.A),
				KeyUp(CommandKey),
			});
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual("hello world", textBox.Text, "Select-all must not type a character.");
			Assert.AreEqual(11, textBox.SelectionLength);
			CollectionAssert.AreEqual(Array.Empty<char>(), characters);
		}
		finally
		{
			injector.InjectKeyboardInput(new[] { KeyUp(CommandKey) });
		}
	}

	[TestMethod]
	public async Task When_InjectModifierKey_Fires_Accelerator()
	{
		var injector = GetInjector();
		var invoked = false;
		var button = new Button { Content = "Target" };
		var accelerator = new KeyboardAccelerator { Key = VirtualKey.X, Modifiers = VirtualKeyModifiers.Control };
		accelerator.Invoked += (_, e) =>
		{
			invoked = true;
			e.Handled = true;
		};
		button.KeyboardAccelerators.Add(accelerator);

		await UITestHelper.Load(button);
		button.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			// The Control key-down must itself be injected: accelerators read ambient key state,
			// not the modifiers carried on the event.
			injector.InjectKeyboardInput(new[]
			{
				Key(VirtualKey.Control),
				Key(VirtualKey.X),
				KeyUp(VirtualKey.X),
				KeyUp(VirtualKey.Control),
			});
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsTrue(invoked, "Ctrl+X accelerator should have been invoked.");
		}
		finally
		{
			injector.InjectKeyboardInput(new[] { KeyUp(VirtualKey.Control) });
		}
	}

	[TestMethod]
	public async Task When_InjectCapsLock_Produces_Uppercase()
	{
		var injector = GetInjector();

		var textBox = new TextBox();
		await UITestHelper.Load(textBox);
		textBox.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			Tap(injector, VirtualKey.CapitalLock);
			Tap(injector, VirtualKey.A);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual("A", textBox.Text);

			// Latched state is process-wide, so a second injector must observe it too.
			Tap(InputInjector.TryCreate()!, VirtualKey.B);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual("AB", textBox.Text);
		}
		finally
		{
			// Unlatch so later tests see a normal keyboard.
			Tap(injector, VirtualKey.CapitalLock);
		}
	}

	[TestMethod]
	public async Task When_InjectUnicode_With_NonZero_VirtualKey_Throws_Before_Dispatching()
	{
		var injector = GetInjector();

		var textBox = new TextBox();
		await UITestHelper.Load(textBox);
		textBox.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		var batch = new[]
		{
			Key(VirtualKey.A),
			new InjectedInputKeyboardInfo { VirtualKey = (ushort)VirtualKey.B, ScanCode = 'b', KeyOptions = InjectedInputKeyOptions.Unicode },
		};

		Assert.ThrowsExactly<ArgumentException>(() => injector.InjectKeyboardInput(batch));
		await TestServices.WindowHelper.WaitForIdle();

		// The valid leading entry must not have been dispatched.
		Assert.AreEqual(string.Empty, textBox.Text);
	}

	[TestMethod]
	public async Task When_InjectAsync_Canceled_MidBatch_Does_Not_Latch_Modifiers()
	{
		var injector = GetInjector();
		var textBox = new TextBox();
		await UITestHelper.Load(textBox);
		textBox.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			using CancellationTokenSource cts = new();
			var injection = injector.InjectKeyboardInputAsync(new[] { Key(VirtualKey.Control), KeyUp(VirtualKey.Control) }, cts.Token);
			cts.Cancel();

			await Assert.ThrowsAsync<OperationCanceledException>(async () => await injection);

			Tap(injector, VirtualKey.A);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual("a", textBox.Text);
		}
		finally
		{
			injector.InjectKeyboardInput(new[] { KeyUp(VirtualKey.Control) });
		}
	}

	[TestMethod]
	public async Task When_InjectTab_Moves_Focus()
	{
		var injector = GetInjector();
		var first = new Button { Content = "First" };
		var second = new Button { Content = "Second" };
		var panel = new StackPanel();
		panel.Children.Add(first);
		panel.Children.Add(second);

		await UITestHelper.Load(panel);
		first.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		Tap(injector, VirtualKey.Tab);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(second, FocusManager.GetFocusedElement(panel.XamlRoot!));
	}

	[TestMethod]
	public async Task When_InjectTab_Does_Not_Type_Tab_Character()
	{
		var injector = GetInjector();
		var textBox = new TextBox { AcceptsReturn = true };
		await UITestHelper.Load(textBox);
		textBox.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		Tap(injector, VirtualKey.Tab);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(string.Empty, textBox.Text);
	}

	[TestMethod]
	public async Task When_InjectUnicode_Delivers_Character()
	{
		var injector = GetInjector();
		var textBox = new TextBox();
		await UITestHelper.Load(textBox);
		textBox.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		injector.InjectKeyboardInput(new[]
		{
			Unicode('é'),
			Unicode('é', InjectedInputKeyOptions.KeyUp),
		});
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual("é", textBox.Text);
	}

	[TestMethod]
	public async Task When_InjectUnicode_SurrogatePair()
	{
		var injector = GetInjector();
		var textBox = new TextBox();
		await UITestHelper.Load(textBox);
		textBox.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		const string Emoji = "😀";
		injector.InjectKeyboardInput(new[]
		{
			Unicode(Emoji[0]),
			Unicode(Emoji[1]),
			Unicode(Emoji[1], InjectedInputKeyOptions.KeyUp),
			Unicode(Emoji[0], InjectedInputKeyOptions.KeyUp),
		});
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(Escape(Emoji), Escape(textBox.Text));
	}

	[TestMethod]
	public void When_InjectUnicode_With_NonZero_VirtualKey_Throws()
	{
		var injector = GetInjector();
		var info = new InjectedInputKeyboardInfo
		{
			VirtualKey = (ushort)VirtualKey.A,
			ScanCode = 'a',
			KeyOptions = InjectedInputKeyOptions.Unicode,
		};

		Assert.ThrowsExactly<ArgumentException>(() => injector.InjectKeyboardInput(new[] { info }));
	}

	[TestMethod]
	public async Task When_InjectKeyUp_Without_KeyDown()
	{
		var injector = GetInjector();
		var textBox = new TextBox();
		await UITestHelper.Load(textBox);
		textBox.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		var events = new List<string>();
		textBox.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler((_, _) => events.Add("KeyDown")), handledEventsToo: true);
		textBox.AddHandler(UIElement.KeyUpEvent, new KeyEventHandler((_, _) => events.Add("KeyUp")), handledEventsToo: true);
		textBox.AddHandler(
			UIElement.CharacterReceivedEvent,
			new TypedEventHandler<UIElement, CharacterReceivedRoutedEventArgs>((_, _) => events.Add("CharacterReceived")),
			handledEventsToo: true);

		injector.InjectKeyboardInput(new[] { KeyUp(VirtualKey.A) });
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(string.Empty, textBox.Text);
		CollectionAssert.AreEqual(new[] { "KeyUp" }, events);
	}

	[TestMethod]
	public async Task When_InjectKeys_Preserve_Order()
	{
		var injector = GetInjector();
		var textBox = new TextBox();
		await UITestHelper.Load(textBox);
		textBox.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		injector.InjectKeyboardInput(new[]
		{
			Key(VirtualKey.U), KeyUp(VirtualKey.U),
			Key(VirtualKey.N), KeyUp(VirtualKey.N),
			Key(VirtualKey.O), KeyUp(VirtualKey.O),
		});
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual("uno", textBox.Text);
	}

	[TestMethod]
	public async Task When_InjectCtrl_Then_Click_Reports_Control_Modifier()
	{
		var injector = GetInjector();
		var target = new Border
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.DeepPink),
		};
		var bounds = await UITestHelper.Load(target);

		VirtualKeyModifiers? modifiers = null;
		target.PointerPressed += (_, e) => modifiers ??= e.KeyModifiers;

		var mouse = injector.GetMouse();
		try
		{
			injector.InjectKeyboardInput(new[] { Key(VirtualKey.Control) });
			mouse.Press(new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2));
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(VirtualKeyModifiers.Control, modifiers);
		}
		finally
		{
			mouse.Release();
			injector.InjectKeyboardInput(new[] { KeyUp(VirtualKey.Control) });
		}
	}

	[TestMethod]
	public async Task When_InjectKey_Populates_KeyStatus()
	{
		var injector = GetInjector();
		var textBox = new TextBox();
		await UITestHelper.Load(textBox);
		textBox.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		KeyRoutedEventArgs? down = null;
		KeyRoutedEventArgs? up = null;
		textBox.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler((_, e) => down ??= e), handledEventsToo: true);
		textBox.AddHandler(UIElement.KeyUpEvent, new KeyEventHandler((_, e) => up ??= e), handledEventsToo: true);

		injector.InjectKeyboardInput(new[]
		{
			Key(VirtualKey.Right, InjectedInputKeyOptions.ExtendedKey),
			Key(VirtualKey.Right, InjectedInputKeyOptions.ExtendedKey | InjectedInputKeyOptions.KeyUp),
		});
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsNotNull(down);
		Assert.IsNotNull(up);

		Assert.IsFalse(down!.KeyStatus.WasKeyDown);
		Assert.IsFalse(down.KeyStatus.IsKeyReleased);
		Assert.AreEqual(1u, down.KeyStatus.RepeatCount);
		Assert.IsTrue(down.KeyStatus.IsExtendedKey);

		Assert.IsTrue(up!.KeyStatus.WasKeyDown);
		Assert.IsTrue(up.KeyStatus.IsKeyReleased);
		Assert.IsTrue(up.KeyStatus.IsExtendedKey);
	}
}
#endif
