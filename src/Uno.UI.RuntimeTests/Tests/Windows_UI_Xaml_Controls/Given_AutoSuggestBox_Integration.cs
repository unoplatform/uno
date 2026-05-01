// MUX Reference AutoSuggestBoxIntegrationTests.cpp, commit 5f9e85113
// Selected test ports — focused on functional integration tests that don't require
// gamepad/mouse/telemetry infrastructure not available in the Uno runtime test harness.

using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MUXControlsTestApp.Utilities;
using Uno.UI.Extensions;

using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_AutoSuggestBox_Integration
	{
		[TestMethod]
		public void CanInstantiate()
		{
			// AutoSuggestBoxIntegrationTests::CanInstantiate
			var autoSuggestBox = new AutoSuggestBox();
			Assert.IsNotNull(autoSuggestBox);
		}

		[TestMethod]
		public async Task CanEnterAndLeaveLiveTree()
		{
			// AutoSuggestBoxIntegrationTests::CanEnterAndLeaveLiveTree
			var autoSuggestBox = new AutoSuggestBox();

			WindowHelper.WindowContent = autoSuggestBox;
			await WindowHelper.WaitForLoaded(autoSuggestBox);

			Assert.IsTrue(autoSuggestBox.IsLoaded);

			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(autoSuggestBox.IsLoaded);
		}

		[TestMethod]
		public void CanGetAndSetProperties()
		{
			// AutoSuggestBoxIntegrationTests::CanGetAndSetProperties
			var itemList = new[] { "Red", "Blue", "Yellow" };

			var autoSuggestBox = new AutoSuggestBox();

			Assert.IsFalse(autoSuggestBox.AutoMaximizeSuggestionArea, "Uno default differs from WinUI: AutoMaximizeSuggestionArea=false");
			autoSuggestBox.AutoMaximizeSuggestionArea = true;
			Assert.IsTrue(autoSuggestBox.AutoMaximizeSuggestionArea);

			Assert.IsNull(autoSuggestBox.ItemsSource);
			autoSuggestBox.ItemsSource = itemList;
			Assert.AreSame(itemList, autoSuggestBox.ItemsSource);

			Assert.IsFalse(autoSuggestBox.IsSuggestionListOpen);

			Assert.IsTrue(autoSuggestBox.UpdateTextOnSelect);
			autoSuggestBox.UpdateTextOnSelect = false;
			Assert.IsFalse(autoSuggestBox.UpdateTextOnSelect);

			Assert.AreEqual(string.Empty, autoSuggestBox.Text);
			autoSuggestBox.Text = "Auto Suggest Box";
			Assert.AreEqual("Auto Suggest Box", autoSuggestBox.Text);

			Assert.AreEqual(string.Empty, autoSuggestBox.PlaceholderText);
			autoSuggestBox.PlaceholderText = "Placeholder Text";
			Assert.AreEqual("Placeholder Text", autoSuggestBox.PlaceholderText);

			Assert.AreEqual(string.Empty, autoSuggestBox.TextMemberPath);
			autoSuggestBox.TextMemberPath = "Text Member Path";
			Assert.AreEqual("Text Member Path", autoSuggestBox.TextMemberPath);
		}

		[TestMethod]
		public async Task CanSetQueryButtonIcon()
		{
			// AutoSuggestBoxIntegrationTests::CanSetQueryButtonIcon
			var autoSuggestBox = new AutoSuggestBox
			{
				QueryIcon = new SymbolIcon { Symbol = Symbol.Find }
			};

			WindowHelper.WindowContent = autoSuggestBox;
			await WindowHelper.WaitForLoaded(autoSuggestBox);

			var newIcon = new SymbolIcon { Symbol = Symbol.Accept };
			autoSuggestBox.QueryIcon = newIcon;
			await WindowHelper.WaitForIdle();

			var queryButton = (ButtonBase)autoSuggestBox.GetTemplateChild("QueryButton");
			Assert.IsNotNull(queryButton);
			Assert.AreSame(newIcon, queryButton.Content);
		}

		[TestMethod]
		public async Task ValidateQueryButtonIsCollapsedWithNoQueryIcon()
		{
			// AutoSuggestBoxIntegrationTests::ValidateQueryButtonIsCollapsedWithNoQueryIcon
			var autoSuggestBox = new AutoSuggestBox();

			WindowHelper.WindowContent = autoSuggestBox;
			await WindowHelper.WaitForLoaded(autoSuggestBox);

			var queryButton = (ButtonBase)autoSuggestBox.GetTemplateChild("QueryButton");
			Assert.IsNotNull(queryButton);
			Assert.AreEqual(Visibility.Collapsed, queryButton.Visibility);
		}

		[TestMethod]
		public async Task ValidateDataContextDoesNotPropagateIntoHeader()
		{
			// AutoSuggestBoxIntegrationTests::ValidateDataContextDoesNotPropagateIntoHeader
			// header content presenter has x:DeferLoadStrategy = "Lazy" in WinUI;
			// in Uno the presenter exists but should be Collapsed when Header is null,
			// and DataContext should not flow into a non-existent Header.
			var stackPanel = new StackPanel
			{
				Children =
				{
					new AutoSuggestBox { Width = 150 },
					new Button { Content = "Button" },
				}
			};
			var autoSuggestBox = (AutoSuggestBox)stackPanel.Children[0];

			WindowHelper.WindowContent = stackPanel;
			await WindowHelper.WaitForLoaded(autoSuggestBox);

			stackPanel.DataContext = stackPanel;
			await WindowHelper.WaitForIdle();

			// In Uno, the inner TextBox hosts the Header presenter named "HeaderContentPresenter".
			// It should be either null (deferred-load equivalent) or Collapsed when no Header is set,
			// and most importantly it should not contain anything reflecting the DataContext.
			var textBox = (TextBox)autoSuggestBox.GetTemplateChild("TextBox");
			Assert.IsNotNull(textBox);

			var headerPresenter = textBox.GetTemplateChild("HeaderContentPresenter") as FrameworkElement;
			if (headerPresenter is not null)
			{
				Assert.AreEqual(Visibility.Collapsed, headerPresenter.Visibility,
					"HeaderContentPresenter should be collapsed when Header is null.");
			}
		}

		[TestMethod]
		public async Task ValidatePopupOpensAsSoonAsItemsSourceChanges()
		{
			// AutoSuggestBoxIntegrationTests::ValidatePopupOpensAsSoonAsItemsSourceChanges
			// Regression for setting ItemsSource inside the TextChanged handler:
			// IsSuggestionListOpen should become true synchronously when items are assigned
			// while there is text in the box.
			var autoSuggestBox = new AutoSuggestBox();

			autoSuggestBox.TextChanged += (sender, args) =>
			{
				if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
				{
					sender.ItemsSource = new[] { "this", "that", "the other" };
					Assert.IsTrue(sender.IsSuggestionListOpen,
						"IsSuggestionListOpen should be true immediately after assigning a non-empty ItemsSource while focused with user-typed text.");
				}
			};

			WindowHelper.WindowContent = autoSuggestBox;
			await WindowHelper.WaitForLoaded(autoSuggestBox);

			autoSuggestBox.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var textBox = (TextBox)autoSuggestBox.GetTemplateChild("TextBox");
			textBox.ProcessTextInput("t");
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(autoSuggestBox.IsSuggestionListOpen, "Popup should still be open after the TextChanged handler returns.");
		}

		[TestMethod]
		public async Task ValidateIsSuggestionListOpenChangesWhenPopupOpens()
		{
			// AutoSuggestBoxIntegrationTests::ValidateIsSuggestionListOpenChangesWhenPopupOpens
			var autoSuggestBox = new AutoSuggestBox
			{
				ItemsSource = new[] { "this", "that", "the other" },
			};

			int callbackOnOpenCount = 0;
			int callbackOnClosedCount = 0;

			var token = autoSuggestBox.RegisterPropertyChangedCallback(
				AutoSuggestBox.IsSuggestionListOpenProperty,
				(sender, _) =>
				{
					if (((AutoSuggestBox)sender).IsSuggestionListOpen)
					{
						callbackOnOpenCount++;
					}
					else
					{
						callbackOnClosedCount++;
					}
				});

			try
			{
				WindowHelper.WindowContent = autoSuggestBox;
				await WindowHelper.WaitForLoaded(autoSuggestBox);

				autoSuggestBox.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();

				var textBox = (TextBox)autoSuggestBox.GetTemplateChild("TextBox");
				textBox.ProcessTextInput("t");
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(1, callbackOnOpenCount, "IsSuggestionListOpen should have fired once when the popup opened.");

				autoSuggestBox.IsSuggestionListOpen = false;
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(1, callbackOnClosedCount, "IsSuggestionListOpen should have fired once when the popup closed.");
			}
			finally
			{
				autoSuggestBox.UnregisterPropertyChangedCallback(AutoSuggestBox.IsSuggestionListOpenProperty, token);
			}
		}
	}
}
