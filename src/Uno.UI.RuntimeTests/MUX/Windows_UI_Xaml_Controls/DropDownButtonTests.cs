using Common;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using System.Threading.Tasks;
using Uno.UI.RuntimeTests;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests
{
	[TestClass]
	public class DropDownButtonTests
	{
#if !WINAPPSDK // GetTemplateChild is protected in UWP while public in Uno.
		[TestMethod]
		[Description("Verifies that the TextBlock representing the Chevron glyph uses the correct font")]
		[Ignore("Fluent styles V2 use AnimatedIcon instead of FontIcon")]
		[RunsOnUIThread]
		public void VerifyFontFamilyForChevron()
		{
			DropDownButton dropDownButton = null;
			dropDownButton = new DropDownButton();
			TestServices.WindowHelper.WindowContent = dropDownButton;

			var chevronTextBlock = dropDownButton.GetTemplateChild("ChevronTextBlock") as TextBlock;
			var font = chevronTextBlock.FontFamily;
			Verify.AreEqual((FontFamily)Application.Current.Resources["SymbolThemeFontFamily"], font);
		}
#endif

		[TestMethod]
		[Description("Verifies that IsEnabled property changes the visual state properly")]
		[RunsOnUIThread]
		public async Task VerifyIsEnabledChangesVisualState()
		{
			DropDownButton dropDownButton = null;

			await RunOnUIThread.ExecuteAsync(() =>
			{
				dropDownButton = new DropDownButton();
				dropDownButton.Content = "Test Button";
				TestServices.WindowHelper.WindowContent = dropDownButton;
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				// Verify button starts enabled
				Verify.IsTrue(dropDownButton.IsEnabled, "Button should start enabled");

				// Disable the button
				dropDownButton.IsEnabled = false;
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				// Verify button is disabled
				Verify.IsFalse(dropDownButton.IsEnabled, "Button should be disabled");

				// Re-enable the button (this is the critical test case for the issue)
				dropDownButton.IsEnabled = true;
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				// Verify button is enabled again
				Verify.IsTrue(dropDownButton.IsEnabled, "Button should be enabled again");
			});
		}

		[TestMethod]
		[Description("Verifies that IsEnabled can be toggled multiple times")]
		[RunsOnUIThread]
		public async Task VerifyIsEnabledCanBeToggledMultipleTimes()
		{
			const int ToggleIterations = 3;
			DropDownButton dropDownButton = null;

			await RunOnUIThread.ExecuteAsync(() =>
			{
				dropDownButton = new DropDownButton();
				dropDownButton.Content = "Test Button";
				TestServices.WindowHelper.WindowContent = dropDownButton;
			});

			await TestServices.WindowHelper.WaitForIdle();

			// Toggle IsEnabled multiple times to ensure visual state updates correctly
			for (int i = 0; i < ToggleIterations; i++)
			{
				await RunOnUIThread.ExecuteAsync(() =>
				{
					dropDownButton.IsEnabled = false;
				});

				await TestServices.WindowHelper.WaitForIdle();

				await RunOnUIThread.ExecuteAsync(() =>
				{
					Verify.IsFalse(dropDownButton.IsEnabled, $"Button should be disabled on iteration {i}");

					dropDownButton.IsEnabled = true;
				});

				await TestServices.WindowHelper.WaitForIdle();

				await RunOnUIThread.ExecuteAsync(() =>
				{
					Verify.IsTrue(dropDownButton.IsEnabled, $"Button should be enabled on iteration {i}");
				});
			}
		}
	}
}
