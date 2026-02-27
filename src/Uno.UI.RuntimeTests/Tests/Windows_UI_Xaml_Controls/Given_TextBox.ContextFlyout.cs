using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_TextBox
	{
		[TestMethod]
		public async Task When_TextBox_ContextFlyout_IsTextCommandBarFlyout()
		{
			var SUT = new TextBox();

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			Assert.IsNotNull(SUT.ContextFlyout, "TextBox should have a default ContextFlyout");
			Assert.IsInstanceOfType(SUT.ContextFlyout, typeof(TextCommandBarFlyout), "ContextFlyout should be TextCommandBarFlyout");
		}


		[TestMethod]
		public async Task When_TextBox_SelectionFlyout_IsTextCommandBarFlyout()
		{
			var SUT = new TextBox();

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			Assert.IsNotNull(SUT.SelectionFlyout, "TextBox should have a default SelectionFlyout");
			Assert.IsInstanceOfType(SUT.SelectionFlyout, typeof(TextCommandBarFlyout), "SelectionFlyout should be TextCommandBarFlyout");
		}

		[TestMethod]
		public async Task When_TextBox_HasSelection_Commands_Available()
		{
			try
			{
#if __SKIA__
				using var _ = new TextBoxFeatureConfigDisposable();
#endif
				CopyPlaceholderTextToClipboard();

				var SUT = new TextBox { Text = "Test content", Width = 200 };

				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();

				SUT.Select(0, 4); // Select "Test"

				await WindowHelper.WaitForIdle();

				Assert.IsInstanceOfType<TextCommandBarFlyout>(SUT.ContextFlyout, "ContextFlyout should be TextCommandBarFlyout");
				var flyout = (TextCommandBarFlyout)SUT.ContextFlyout;

				flyout.ShowAt(SUT);
				await WindowHelper.WaitForIdle();

				var (hasSelectAll, hasCut, hasCopy, hasPaste) = GetAvailableCommands(flyout);

				Assert.IsTrue(hasCut, "Cut button should be available when text is selected");
				Assert.IsTrue(hasCopy, "Copy button should be available when text is selected");
				Assert.IsTrue(hasSelectAll, "Select All button should be available when text is selected");
				Assert.IsTrue(hasPaste, "Paste should be available");

				flyout.Hide();
			}
			finally
			{
				ClearClipboard();
			}
		}

		[TestMethod]
		public async Task When_TextBox_NoSelection_Commands_Available()
		{
			try
			{
#if __SKIA__
				using var _ = new TextBoxFeatureConfigDisposable();
#endif
				CopyPlaceholderTextToClipboard();

				var SUT = new TextBox { Text = "Test content", Width = 200 };

				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();

				// No selection
				SUT.Select(0, 0);
				await WindowHelper.WaitForIdle();

				Assert.IsInstanceOfType<TextCommandBarFlyout>(SUT.ContextFlyout, "ContextFlyout should be TextCommandBarFlyout");
				var flyout = (TextCommandBarFlyout)SUT.ContextFlyout;

				flyout.ShowAt(SUT);
				await WindowHelper.WaitForIdle();

				var (hasSelectAll, hasCut, hasCopy, hasPaste) = GetAvailableCommands(flyout);

				Assert.IsFalse(hasCut, "Cut button should be available when text is selected");
				Assert.IsFalse(hasCopy, "Copy button should be available when text is selected");
				Assert.IsTrue(hasSelectAll, "Select All button should be available when not all text is selected");
				Assert.IsTrue(hasPaste, "Paste should be available");

				flyout.Hide();
			}
			finally
			{
				ClearClipboard();
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)] // WASM clipboard APIs are async; GetContent() always registers providers, so Contains() is always true after Clear()
		public async Task When_TextBox_Clipboard_Empty()
		{
#if __SKIA__
			using var _ = new TextBoxFeatureConfigDisposable();
#endif
			ClearClipboard();

			var SUT = new TextBox { Text = "Test content", Width = 200 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			// No selection
			SUT.Select(0, 0);
			await WindowHelper.WaitForIdle();

			Assert.IsInstanceOfType<TextCommandBarFlyout>(SUT.ContextFlyout, "ContextFlyout should be TextCommandBarFlyout");
			var flyout = (TextCommandBarFlyout)SUT.ContextFlyout;

			flyout.ShowAt(SUT);
			await WindowHelper.WaitForIdle();

			var (hasSelectAll, hasCut, hasCopy, hasPaste) = GetAvailableCommands(flyout);

			Assert.IsFalse(hasPaste, "Paste should not be available if clipboard is empty");

			flyout.Hide();
		}

		[TestMethod]
		public async Task When_TextBox_IsReadOnly_CutPaste_NotAvailable()
		{
			try
			{
#if __SKIA__
				using var _ = new TextBoxFeatureConfigDisposable();
#endif
				CopyPlaceholderTextToClipboard();

				var SUT = new TextBox { Text = "Test Me", IsReadOnly = true, Width = 200 };

				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();

				SUT.Select(0, 4);
				await WindowHelper.WaitForIdle();

				var flyout = SUT.ContextFlyout as TextCommandBarFlyout;
				Assert.IsInstanceOfType<TextCommandBarFlyout>(SUT.ContextFlyout, "ContextFlyout should be TextCommandBarFlyout");

				flyout.ShowAt(SUT);
				await WindowHelper.WaitForIdle();

				var (hasSelectAll, hasCut, hasCopy, hasPaste) = GetAvailableCommands(flyout);

				Assert.IsFalse(hasCut, "Cut should NOT be available for ReadOnly TextBox");
				Assert.IsFalse(hasPaste, "Paste should NOT be available for ReadOnly TextBox");
				Assert.IsTrue(hasCopy, "Copy should be available for ReadOnly TextBox with selection");
				Assert.IsTrue(hasSelectAll, "Select All should be available for ReadOnly TextBox");

				flyout.Hide();
			}
			finally
			{
				ClearClipboard();
			}
		}

		[TestMethod]
		public async Task When_TextBox_Full_Text_Selected_Commands_Available()
		{
			try
			{
#if __SKIA__
				using var _ = new TextBoxFeatureConfigDisposable();
#endif
				CopyPlaceholderTextToClipboard();

				var SUT = new TextBox { Text = "Test content", Width = 200 };

				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();

				SUT.SelectAll();
				await WindowHelper.WaitForIdle();

				Assert.IsInstanceOfType<TextCommandBarFlyout>(SUT.ContextFlyout, "ContextFlyout should be TextCommandBarFlyout");
				var flyout = (TextCommandBarFlyout)SUT.ContextFlyout;

				flyout.ShowAt(SUT);
				await WindowHelper.WaitForIdle();

				var (hasSelectAll, hasCut, hasCopy, hasPaste) = GetAvailableCommands(flyout);

				Assert.IsTrue(hasCut, "Cut button should be available when text is selected");
				Assert.IsTrue(hasCopy, "Copy button should be available when text is selected");
				Assert.IsTrue(hasSelectAll, "Select All button should be available when text is selected");
				Assert.IsTrue(hasPaste, "Should be available when clipboard has content");

				flyout.Hide();
			}
			finally
			{
				ClearClipboard();
			}
		}

		[TestMethod]
		public async Task When_CanPasteClipboardContent_WithText()
		{
#if __SKIA__
			using var _ = new TextBoxFeatureConfigDisposable();
#endif

			var SUT = new TextBox();

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			// Set clipboard to contain text
			var dataPackage = new DataPackage();
			dataPackage.SetText("Hello");
			Clipboard.SetContent(dataPackage);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.CanPasteClipboardContent, "CanPasteClipboardContent should be true when clipboard has text");
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)] // WASM clipboard APIs are async; GetContent() always registers providers, so Contains() is always true after Clear()
		public async Task When_CanPasteClipboardContent_EmptyClipboard()
		{
#if __SKIA__
			using var _ = new TextBoxFeatureConfigDisposable();
#endif

			var SUT = new TextBox();

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			// Clear clipboard
			Clipboard.Clear();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.CanPasteClipboardContent, "CanPasteClipboardContent should be false when clipboard is empty");
		}

		[TestMethod]
		public async Task When_CanPasteClipboardContent_ReadOnly()
		{
#if __SKIA__
			using var _ = new TextBoxFeatureConfigDisposable();
#endif

			var SUT = new TextBox { IsReadOnly = true };

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(SUT);

			// Set clipboard to contain text
			var dataPackage = new DataPackage();
			dataPackage.SetText("Hello");
			Clipboard.SetContent(dataPackage);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.CanPasteClipboardContent, "CanPasteClipboardContent should be false when TextBox is read-only");
		}

		private void CopyPlaceholderTextToClipboard()
		{
			var dataPackage = new DataPackage();
			dataPackage.SetText("CLIPBOARD CONTENT");
			Clipboard.SetContent(dataPackage);
		}

		private void ClearClipboard()
		{
			Clipboard.Clear();
		}

		private (bool hasSelectAll, bool hasCut, bool hasCopy, bool hasPaste) GetAvailableCommands(TextCommandBarFlyout flyout)
		{
			var allCommands = flyout.PrimaryCommands.Concat(flyout.SecondaryCommands).ToList();
			var hasCut = allCommands.OfType<AppBarButton>().Any(b => b.KeyboardAccelerators.Any(ka => ka.Key == VirtualKey.X && ka.Modifiers.HasFlag(_platformCtrlKey)));
			var hasCopy = allCommands.OfType<AppBarButton>().Any(b => b.KeyboardAccelerators.Any(ka => ka.Key == VirtualKey.C && ka.Modifiers.HasFlag(_platformCtrlKey)));
			var hasPaste = allCommands.OfType<AppBarButton>().Any(b => b.KeyboardAccelerators.Any(ka => ka.Key == VirtualKey.V && ka.Modifiers.HasFlag(_platformCtrlKey)));
			var hasSelectAll = allCommands.OfType<AppBarButton>().Any(b => b.KeyboardAccelerators.Any(ka => ka.Key == VirtualKey.A && ka.Modifiers.HasFlag(_platformCtrlKey)));

			return (hasSelectAll, hasCut, hasCopy, hasPaste);
		}
	}
}
