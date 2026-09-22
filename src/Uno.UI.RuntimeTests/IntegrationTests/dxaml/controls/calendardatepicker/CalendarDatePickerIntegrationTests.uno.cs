#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.MUX.Helpers;
using Windows.UI;

namespace Microsoft.UI.Xaml.Tests.Enterprise.CalendarDatePickerTests;

partial class CalendarDatePickerIntegrationTests
{
	private static async Task RunWithUwpStylesAsync(Func<Task> action)
	{
		IDisposable? restoreStyles = null;
		try
		{
			await RunOnUIThread(() => restoreStyles = StyleHelper.UseUwpStyles());
			await action();
		}
		finally
		{
			await RunOnUIThread(() => restoreStyles?.Dispose());
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_KeyboardFlyout_StyleScope_Ends_Then_FluentResourcesAreRestored(bool failFirstAttempt)
	{
		var resources = Application.Current.Resources;
		var fluentResources = resources.MergedDictionaries.OfType<XamlControlsResources>().FirstOrDefault();
		Assert.IsNotNull(fluentResources, "The test must start with the app's Fluent resources.");
		var originalIndex = resources.MergedDictionaries.IndexOf(fluentResources);
		var originalCount = resources.MergedDictionaries.Count;

		using var theme = ThemeHelper.UseApplicationLightTheme();
		try
		{
			if (failFirstAttempt)
			{
				var injectedFailure = new TimeoutException("Injected first-attempt failure after removing Fluent resources.");
				var exception = await Assert.ThrowsExactlyAsync<TimeoutException>(() =>
					RunWithUwpStylesAsync(async () =>
					{
						await AssertUwpStylesActive();
						await Task.Yield();
						throw injectedFailure;
					}));

				Assert.AreSame(injectedFailure, exception);
				Assert.IsTrue(resources.MergedDictionaries.Contains(fluentResources),
					"The original Fluent dictionary must be restored before a retry starts.");
			}

			await RunWithUwpStylesAsync(AssertUwpStylesActive);

			Assert.AreSame(resources, Application.Current.Resources);
			Assert.IsTrue(resources.MergedDictionaries.Contains(fluentResources));
			Assert.AreEqual(originalCount, resources.MergedDictionaries.Count);

			var comboBox = new ComboBox { Text = "Test", Width = 200 };
			var run = new Run { Text = "Fluent" };
			var textBlock = new TextBlock { Inlines = { run } };
			var panel = new StackPanel
			{
				RequestedTheme = ElementTheme.Light,
				Children = { comboBox, textBlock },
			};
			await UITestHelper.Load(panel);

			comboBox.IsEditable = true;
			await TestServices.WindowHelper.WaitForIdle();

			var editableTextBox = TreeHelper.GetVisualChildByName(comboBox, "EditableText") as TextBox;
			Assert.IsNotNull(editableTextBox, "The next ComboBox must use its Fluent template.");
			Assert.AreEqual("Test", editableTextBox.Text);
			var foreground = run.Foreground as SolidColorBrush;
			Assert.IsNotNull(foreground);
			Assert.AreEqual(Color.FromArgb(0xE4, 0, 0, 0), foreground.Color);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
			if (!resources.MergedDictionaries.Contains(fluentResources))
			{
				// A failing regression assertion must not contaminate the remaining tests.
				resources.MergedDictionaries.Insert(originalIndex, fluentResources);
				StyleHelper.UseUwpStyles().Dispose();
			}
		}

		Task AssertUwpStylesActive() => RunOnUIThread(() =>
		{
#if HAS_UNO
			Assert.IsFalse(resources.MergedDictionaries.Contains(fluentResources));
#endif
		});
	}
}
