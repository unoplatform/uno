using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.DevTools.Input;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_TextBox_PreventKeyboardDisplay
{
	[TestMethod]
	public void When_Default_Then_Keyboard_Is_Not_Prevented()
	{
		Assert.IsFalse(new TextBox().PreventKeyboardDisplayOnProgrammaticFocus);
		Assert.IsFalse(new PasswordBox().PreventKeyboardDisplayOnProgrammaticFocus);
	}

	[TestMethod]
	[DataRow(true, FocusState.Programmatic, true)]
	[DataRow(true, FocusState.Pointer, false)]
	[DataRow(true, FocusState.Keyboard, false)]
	[DataRow(false, FocusState.Programmatic, false)]
	public async Task When_Focused_Then_Keyboard_Suppressed_Only_For_Prevented_Programmatic_Focus(bool prevent, FocusState focusState, bool expectSuppressed)
	{
		var extension = new RecordingExtension();
		using var scope = ImeSessionCoordinator.SetExtensionFactoryForTesting(_ => extension);
		var textBox = new TextBox { Width = 200, PreventKeyboardDisplayOnProgrammaticFocus = prevent };
		var other = new Button { Content = "other" };
		try
		{
			await UITestHelper.Load(new StackPanel { Children = { other, textBox } });
			Assert.IsTrue(other.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(textBox.Focus(focusState));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(focusState, extension.LastActivation.FocusState);
			Assert.AreEqual(expectSuppressed, extension.LastActivation.IsSoftwareKeyboardSuppressed);
			Assert.AreEqual(expectSuppressed, textBox.Core.IsSoftwareKeyboardSuppressed);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)] // The injected touch doesn't reach the TextBox on the Android emulator
	public async Task When_Programmatically_Focused_Then_Tap_Shows_Keyboard()
	{
		var extension = new RecordingExtension();
		using var scope = ImeSessionCoordinator.SetExtensionFactoryForTesting(_ => extension);
		var textBox = new TextBox { Width = 200, PreventKeyboardDisplayOnProgrammaticFocus = true };
		try
		{
			await UITestHelper.Load(textBox);
			Assert.IsTrue(textBox.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(textBox.Core.IsSoftwareKeyboardSuppressed, "Pre-condition: the programmatic focus keeps the keyboard hidden");

			using var finger = InputInjector.TryCreate()?.GetFinger() ?? throw new InvalidOperationException("Failed to create finger");
			finger.Tap(textBox.TransformToVisual(null).TransformPoint(new Point(textBox.ActualWidth / 2, textBox.ActualHeight / 2)));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(FocusState.Pointer, extension.LastActivation.FocusState);
			Assert.IsFalse(extension.LastActivation.IsSoftwareKeyboardSuppressed);
			Assert.IsFalse(textBox.Core.IsSoftwareKeyboardSuppressed);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	// The tap only counts for the keyboard request it makes: a later programmatic focus of the same box keeps the keyboard
	// hidden again.
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)] // The injected touch doesn't reach the TextBox on the Android emulator
	public async Task When_Tapped_Then_Later_Programmatic_Focus_Suppresses_Keyboard_Again()
	{
		var extension = new RecordingExtension();
		using var scope = ImeSessionCoordinator.SetExtensionFactoryForTesting(_ => extension);
		var textBox = new TextBox { Width = 200, PreventKeyboardDisplayOnProgrammaticFocus = true };
		try
		{
			await UITestHelper.Load(textBox);
			Assert.IsTrue(textBox.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();

			using var finger = InputInjector.TryCreate()?.GetFinger() ?? throw new InvalidOperationException("Failed to create finger");
			finger.Tap(textBox.TransformToVisual(null).TransformPoint(new Point(textBox.ActualWidth / 2, textBox.ActualHeight / 2)));
			await WindowHelper.WaitForIdle();
			Assert.IsFalse(textBox.Core.IsSoftwareKeyboardSuppressed, "Pre-condition: the tap shows the keyboard");

			// With the keyboard as the last input device, the programmatic focus changes the box's coerced focus state, which
			// restarts its text input without the box losing the focus.
			await WindowHelper.SetLastInputMethod(Uno.UI.Xaml.Input.InputDeviceType.Keyboard, textBox.XamlRoot);
			Assert.IsTrue(textBox.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(FocusState.Keyboard, textBox.FocusState, "Pre-condition: the focus state changed, restarting the text input");

			Assert.IsTrue(extension.LastActivation.IsSoftwareKeyboardSuppressed);
			Assert.IsTrue(textBox.Core.IsSoftwareKeyboardSuppressed);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_PasswordBox_Programmatically_Focused_Then_Keyboard_Suppressed()
	{
		var passwordBox = new PasswordBox { Width = 200, PreventKeyboardDisplayOnProgrammaticFocus = true };
		try
		{
			await UITestHelper.Load(passwordBox);
			Assert.IsTrue(passwordBox.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(passwordBox.Core.IsSoftwareKeyboardSuppressed);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private sealed class RecordingExtension : IHostScopedImeTextBoxExtension
	{
		public ImeSessionActivation LastActivation { get; private set; }

		public bool IsComposing => false;

		public event EventHandler CompositionStarted { add { } remove { } }
		public event EventHandler<ImeCompositionEventArgs> CompositionUpdated { add { } remove { } }
		public event EventHandler<ImeCompositionEventArgs> CompositionCompleted { add { } remove { } }
		public event EventHandler<ImePartialCompositionEventArgs> CompositionPartiallyCommitted { add { } remove { } }
		public event EventHandler<ImeCompositionEventArgs> CompositionCanceled { add { } remove { } }
		public event EventHandler CompositionEnded { add { } remove { } }
		public event EventHandler<ImeCandidateWindowBoundsChangedEventArgs> CandidateWindowBoundsChanged { add { } remove { } }

		public void StartImeSession(IImeSessionHost host, ImeSessionActivation activation) => LastActivation = activation;

		public void EndImeSession()
		{
		}

		public void UpdateImeSession(IImeSessionHost host, ImeSessionUpdate update)
		{
		}

		public Task<IReadOnlyList<string>> GetLinguisticAlternativesAsync(string compositionText, CancellationToken cancellationToken)
			=> Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
	}
}
