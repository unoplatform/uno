#if HAS_INPUT_INJECTOR || WINAPPSDK
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit.DevTools.Input;
using Uno.UI.Extensions;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Input.Preview.Injection;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Input_Preview_Injection;

[TestClass]
[RunsOnUIThread]
public class Given_InputInjector_Keyboard
{
	private static InputInjector? TryGetInjector()
		=> TestServices.WindowHelper.IsXamlIsland ? null : InputInjector.TryCreate();

	private static InjectedInputKeyboardInfo Key(VirtualKey key, InjectedInputKeyOptions options = InjectedInputKeyOptions.None)
		=> new() { VirtualKey = (ushort)key, KeyOptions = options };

	private static InjectedInputKeyboardInfo KeyUp(VirtualKey key)
		=> Key(key, InjectedInputKeyOptions.KeyUp);

	private static InjectedInputKeyboardInfo Unicode(char c, InjectedInputKeyOptions options = InjectedInputKeyOptions.None)
		=> new() { VirtualKey = 0, ScanCode = c, KeyOptions = options | InjectedInputKeyOptions.Unicode };

	private static void Tap(InputInjector injector, VirtualKey key)
		=> injector.InjectKeyboardInput(new[] { Key(key), KeyUp(key) });

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_InjectKey_Types_Into_Focused_TextBox()
	{
		if (TryGetInjector() is not { } injector)
		{
			return;
		}

		var textBox = new TextBox();
		await UITestHelper.Load(textBox);
		textBox.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		// Deliberately not naming the target element - the key must find the focused element itself.
		Tap(injector, VirtualKey.A);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual("a", textBox.Text);
	}

	/// <summary>
	/// The discriminating test: CharacterReceived is raised only by InputManager, so this cannot
	/// pass through TestServices.KeyboardHelper, which raises routed events directly on an element.
	/// </summary>
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_InjectKey_Raises_CharacterReceived()
	{
		if (TryGetInjector() is not { } injector)
		{
			return;
		}

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
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_InjectShiftA_Produces_Uppercase()
	{
		if (TryGetInjector() is not { } injector)
		{
			return;
		}

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
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_InjectCtrlA_Selects_All_Without_Character()
	{
		if (TryGetInjector() is not { } injector)
		{
			return;
		}

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
				Key(VirtualKey.Control),
				Key(VirtualKey.A),
				KeyUp(VirtualKey.A),
				KeyUp(VirtualKey.Control),
			});
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual("hello world", textBox.Text, "Ctrl+A must not type a character.");
			Assert.AreEqual(11, textBox.SelectionLength);
			CollectionAssert.AreEqual(Array.Empty<char>(), characters);
		}
		finally
		{
			injector.InjectKeyboardInput(new[] { KeyUp(VirtualKey.Control) });
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_InjectModifierKey_Fires_Accelerator()
	{
		if (TryGetInjector() is not { } injector)
		{
			return;
		}

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
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_InjectTab_Moves_Focus()
	{
		if (TryGetInjector() is not { } injector)
		{
			return;
		}

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
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_InjectTab_Does_Not_Type_Tab_Character()
	{
		if (TryGetInjector() is not { } injector)
		{
			return;
		}

		var textBox = new TextBox { AcceptsReturn = true };
		await UITestHelper.Load(textBox);
		textBox.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		Tap(injector, VirtualKey.Tab);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(string.Empty, textBox.Text);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_InjectUnicode_Delivers_Character()
	{
		if (TryGetInjector() is not { } injector)
		{
			return;
		}

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
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_InjectUnicode_SurrogatePair()
	{
		if (TryGetInjector() is not { } injector)
		{
			return;
		}

		var textBox = new TextBox();
		await UITestHelper.Load(textBox);
		textBox.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		const string Emoji = "😀";
		injector.InjectKeyboardInput(new[]
		{
			Unicode(Emoji[0]),
			Unicode(Emoji[1]),
		});
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(Emoji, textBox.Text);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_InjectUnicode_With_NonZero_VirtualKey_Throws()
	{
		if (TryGetInjector() is not { } injector)
		{
			return;
		}

		var info = new InjectedInputKeyboardInfo
		{
			VirtualKey = (ushort)VirtualKey.A,
			ScanCode = 'a',
			KeyOptions = InjectedInputKeyOptions.Unicode,
		};

		Assert.ThrowsExactly<ArgumentException>(() => injector.InjectKeyboardInput(new[] { info }));
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_InjectKeyUp_Without_KeyDown()
	{
		if (TryGetInjector() is not { } injector)
		{
			return;
		}

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
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_InjectKeys_Preserve_Order()
	{
		if (TryGetInjector() is not { } injector)
		{
			return;
		}

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
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_InjectCtrl_Then_Click_Reports_Control_Modifier()
	{
		if (TryGetInjector() is not { } injector)
		{
			return;
		}

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
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_InjectKey_Populates_KeyStatus()
	{
		if (TryGetInjector() is not { } injector)
		{
			return;
		}

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
